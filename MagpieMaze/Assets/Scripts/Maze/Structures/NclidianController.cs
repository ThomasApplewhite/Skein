/*Copyright (c) 2021 Magpie Paulsen
Written by Thomas Applewhite

This program is free software; you can non-commercially distribute
this software without modifcation and with attribution under the Creative Commons
BY-NC-ND 4.0 License.

This program is distributed WITHOUT WARRANTY or FITNESS FOR A PARTICULAR PURPOSE.

You shoould have recieved a copy of the Creative Commons BY-NC-ND 4.0 License along
with this program. If not, see <https://creativecommons.org/licenses/by-nc-nd/4.0/>*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NclidianController : MonoBehaviour
{
    public enum ReplacementState
    {
        DIRECT,
        RANDOM,
        OPEN
    };

    public GameObject PortalA;
    public GameObject PortalB;

    /*
        Okay this is gonna get a bit fucky but hear me out
        This array works by having each ReplacementState's method in the same
        index as the actual enum value. This way, the array can be indexed by enum!
        No comparisons, I just plug in the correct enum and let the methods go!

        I know it's a bit silly, but the underlying idea of directly tying
        an enum value to a delegate isn't crazy. C# just doesn;t
        let me do it directly

        I think.
    */
    private System.Action<GameObject, MazeNeighbors>[] placementMethods;

    void Awake()
    {
        placementMethods = new System.Action<GameObject, MazeNeighbors>[] 
        {
            (portal, region) => DirectPortalReplacement(portal, region),
            (portal, region) => RandomPortalReplacement(portal, region),
            (portal, region) => OpenPortalReplacement(portal, region),
        };
    }

    //Replaces alphaCell and betaCell with portalA and portalB, respectively
    public void PlacePortals(MazeNeighbors alphaRegion, MazeNeighbors betaRegion, 
        ReplacementState alphaState=ReplacementState.RANDOM, ReplacementState betaState=ReplacementState.RANDOM)
    {
        /*if(alphaState == ReplacementState.DIRECT) DirectPortalReplacement(PortalA, alphaRegion);
        else RandomPortalReplacement(PortalA, alphaRegion);

        if(betaState == ReplacementState.DIRECT) DirectPortalReplacement(PortalB, betaRegion);
        else RandomPortalReplacement(PortalB, betaRegion);*/

        placementMethods[(int)alphaState].Invoke(PortalA, alphaRegion);
        placementMethods[(int)betaState].Invoke(PortalB, betaRegion);
            
        //Update the list of portals the player knows about (make sure not to do this too often!)
        GameObject.FindWithTag("Player").BroadcastMessage("UpdatePortalArray");

        Destroy(this.gameObject);
    }

    //Places the portal and copies the replacee's connections
    void DirectPortalReplacement(GameObject portal, MazeNeighbors region)
    {
        //deparent the portal and do the replacement
        var portalCell = TransformReplace(portal, region);

        //Then assume all of the replaced cell's connections
        portalCell.CopyConnections(region.Owner);

        //Then align the portal with at least one of them
        portalCell.AlignPortalWithAnyConnection();
    }

    //Places the portal and makes random connections
    void RandomPortalReplacement(GameObject portal, MazeNeighbors region)
    {
        //First, disconnect all of the original cell's neighbors
        region.Owner.DisconnectAll();

        //deparent the portal and do the replacement
        var portalCell = TransformReplace(portal, region);

        //Then the replaced cell's north neighbor (and potentially others)
        //to the cell
        portalCell.Connect(region.North);

        //Now connect a random wall that isn't the northern wall
        //This UnityEngine statement generates a random int: 0, 1, or 2
        switch (UnityEngine.Random.Range(0, 3))
        {
            case 0:
                portalCell.Connect(region.West);
                break;

            case 1:
                portalCell.Connect(region.East);
                break;

            default: //fires on anything other than 0 or 1
                portalCell.Connect(region.South);
                break;
        }
    }

    //Places the portal and connects everything but the direction the portal is facing
    void OpenPortalReplacement(GameObject portal, MazeNeighbors region)
    {
        //deparent the portal and do the replacement
        var portalCell = TransformReplace(portal, region);

        //Then connect to every side
        portalCell.Connect(region.North);
        portalCell.Connect(region.South);
        portalCell.Connect(region.East);
        portalCell.Connect(region.West);
    }

    //Does universal setup with the transform of the portals
    PortalReplacer TransformReplace(GameObject portal, MazeNeighbors region)
    {
        var portalCell = portal.GetComponent<PortalReplacer>();
        portal.transform.parent = region.Owner.gameObject.transform.parent;
        portalCell.Initialize(region.Owner);

        return portalCell;
    }
}
