/*Copyright (c) 2021 Magpie Paulsen
Written by Thomas Applewhite

This program is free software; you can non-commercially distribute
this software without modification and with attribution under the Creative Commons
BY-NC-ND 4.0 License.

This program is distributed WITHOUT WARRANTY or FITNESS FOR A PARTICULAR PURPOSE.

You should have received a copy of the Creative Commons BY-NC-ND 4.0 License along
with this program. If not, see <https://creativecommons.org/licenses/by-nc-nd/4.0/>*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

[RequireComponent(typeof(Collider))]
public class DialogueInitiator : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [Tooltip("The YarnProgram to pass to the DialogueRunner on startup")]
    public YarnProgram characterDialogue;

    [Tooltip("The starting node for this character")]
    public string startingNode = "Start";

    [Tooltip("The transform to rotate towards the player (and that the player will rotate towards)")]
    public Transform speakerTransform;

    [Header("Other Settings")]
    //[Tooltip("The canvas to display dialogue text on for this character")]
    //Normally this would be pulled directly via GetComponentInChildren, but that method can only find
    //components on active game objects, and yarnspinner UI starts the scene inactive
    //public Canvas canvas;

    [Tooltip("How many seconds it should take for the camera to rotate towards the dialogue UI")]
    public float initiateTime = 0.5f;

    //the scene's DialogueRunner
    private DialogueRunner dialogueRunner;

    //the dialogueUI for this Initiator instance
    private DialogueUI dialogueUI;

    //the player, for ease of use
    private GameObject player;

    //the player's camera, for ease of use
    private GameObject playerCamera;

    //The direction the speaker is facing before the dialogue starts
    private Vector3 originalForward;

    // Start is called before the first frame update
    void Start()
    {
        //locate the dialogueUI
        if(GameObject.FindWithTag("DialogueRunner")?.TryGetComponent<DialogueUI>(out dialogueUI) == true)
        {
            //If it's there, subscribe DeactiveDialogue to OnDialogueEnd
            dialogueUI.onDialogueEnd.AddListener( () => DeactivateDialogue() );
        }
        else
        {
            //If it isn't there, raise an error
            Debug.LogError($"{this.gameObject.name}.DialogueInitiator.Start: DialogueUI not found!");
        }

        //locate the dialogueRunner
        if(GameObject.FindWithTag("DialogueRunner")?.TryGetComponent<DialogueRunner>(out dialogueRunner) == true)
        {
            //Move to dialogue start to avoid loading conflicts
            //add this character's dialogue to the runner's programs
            //dialogueRunner.Add(characterDialogue);
        }
        else
        {
            //If it isn't there, raise an error
            Debug.LogError($"{this.gameObject.name}.DialogueInitiator.Start: DialogueRunner not found!");
        }

        //locate the player
        if( (player = GameObject.FindWithTag("Player")) != null)
        {
            //If it's there, do something. Not sure what yet
        }
        else
        {
            //If it isn't there, raise an error
            Debug.LogError($"{this.gameObject.name}.DialogueInitiator.Start: Player not found!");
        }

        //locate the camera
        if( (playerCamera = GameObject.FindWithTag("MainCamera")) != null)
        {
            //If it's there, assign the actual camera component to the UI's canvas events
            //canvas.worldCamera = playerCamera.GetComponent<Camera>();
        }
        else
        {
            Debug.LogError($"{this.gameObject.name}.DialogueInitiator.Start: Camera not found!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        //If this gameObject is the player...
        if(other.gameObject.tag == "Player")
        {
            //Time to start the dialogue!
            ActivateDialogue();
        }
    }

    //Initiates or handles all of the setup needed for dialogue to be played
    void ActivateDialogue()
    {
        //Step 1: Stop all NPC and player movement
        foreach(FreezableNPC npc in FindObjectsOfType<FreezableNPC>()) npc.Freeze();

        //Step 2: Lerp the player's camera towards the current speaker
        //and
        //Step 3: Lerp the UI towards the camera
        StartCoroutine(LerpToCenterDialogue(speakerTransform, playerCamera.transform));

        //Step 4: Load the approprite dialogue
        dialogueRunner.Add(characterDialogue);

        //Step 5: Start the dialogue for real
        dialogueRunner.StartDialogue(startingNode);
    }

    //Rotates the UI and player camera towards each other
    IEnumerator LerpToCenterDialogue(Transform ui, Transform playerCamera)
    {
        //Calculate the direction from the camera to the UI, which is
        //the two-argument arctangent of the point (UI.y - cam.y, UI.x - cam.y)
        /*Vector3 camToUI = System.Math.Atan2
        (
            ui.position.y - playerCamera.position.y,
            ui.position.x - playerCamera.position.x
        );*/
        //Or is it the difference of the destination minus the source?
        Vector3 camToUI = ui.position - playerCamera.position;
        //Invert that vector to get the direction from the UI to the camera
        Vector3 uiToCam = -camToUI;

        //Save the initial forward vectors of both transforms
        Vector3 camForward = playerCamera.forward;
        Vector3 uiForward = ui.forward;

        //finally, save the speaker's original forward
        originalForward = this.gameObject.transform.forward;

        //While the maximum initialization time hasn't passed...
        var timePassed = 0f;
        while(timePassed < initiateTime)
        {
            //Lerp the forward vectors of both transforms towards their ideal facing directions
            playerCamera.forward = Vector3.Lerp(camForward, camToUI, timePassed / initiateTime);
            ui.forward = Vector3.Lerp(uiForward, uiToCam, timePassed / initiateTime);

            //Let this tiny movement of both items happen
            yield return null;

            //Increment timer
            timePassed += Time.deltaTime;
        }

        //force the rotations the file step of the way, just in case
        playerCamera.forward = camToUI;
        ui.forward = uiToCam;
    }

    //Initiates or handles all of the teardown needed for returning to normal gameplay
    void DeactivateDialogue()
    {
        //Step 1: Unstop all NPC and player movement
        //Well... just the player for now
        foreach (FreezableNPC npc in FindObjectsOfType<FreezableNPC>()) npc.Unfreeze();

        //Step 2: Restore the rotation of the speaker
        this.gameObject.transform.forward = originalForward;

        //Step 3: Unload the dialogue
        dialogueRunner.Clear();
    }
}
