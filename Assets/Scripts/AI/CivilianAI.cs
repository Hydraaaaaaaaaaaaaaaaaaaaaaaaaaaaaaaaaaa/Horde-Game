﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class CivilianAI : MonoBehaviour
{
    public enum QuestList { DECIDE_ON_STARTUP, FIND_QITEM, GIVE_GUN, RESCUE, ESCORT }

    public Animator anim;

    // Enum for the type of quest
    public QuestList Quest;

    public float talkDistance;

    // Dialogue time
    public float popupTime;
    float currentPopupTime;



    [Header("Collection Dialogue")]
    // All dialogue that can be used by the npc
    public string[] startDialogueCollect;
    public string[] endDialogueCollect;
    public string[] todoListInfoCollect;
    public GameObject[] UniqueItems;

    [Header("Give Dialogue")]
    public string[] startDialogueGive;
    public string[] endDialogueGive;
    public string[] todoListInfoGive;
    public GameObject[] GunType;

    [Header("Rescue Dialogue")]
    public string[] startDialogueRescue;
    public string[] endDialogueRescue;
    public string[] todoListInfoRescue;

    [Header("Escort Dialogue")]
    public string[] startDialogueEscort;
    public string[] endDialogueEscort;
    public string[] todoListInfoEscort;


    [Header("Debug Information")]
    // Public list of all players in range of the npc
    public List<GameObject> playersInRange;

    // bools
    public bool MissionAvailable;
    public bool ObjectiveCompleted;
    public bool MissionCompleted;
    public bool Talking;

    // The row of dialogue used for the npc
    public int questDialogueNo;
    public int uniqueItemNo;

    // Use this for initialization
    void Start ()
    {
        // If the mission type is undefined
        if (Quest == QuestList.DECIDE_ON_STARTUP)
        {
            Quest = (QuestList)Random.Range(1, 4);
        }

        switch (Quest)
        {
            case QuestList.ESCORT:
                uniqueItemNo = Random.Range(0, UniqueItems.Length - 1);
                break;
            case QuestList.FIND_QITEM:
                uniqueItemNo = Random.Range(0, GunType.Length - 1);
                break;
        }

        anim = transform.GetChild(0).GetComponent<Animator>();

        // Generate the Quest Text
        switch (Quest)
        {
            // If escort mission
            case QuestList.ESCORT:
                // Grab random line
                questDialogueNo = Random.Range(0, startDialogueEscort.Length);
                currentPopupTime = popupTime;

                // Initialize variables
                Vector3 pos = new Vector3(10000, 10000, 10000);
                float ClosestDist = 10000;

                // Go through each civilian spawner
                for (int i = 0; i < GameObjectManager.instance.civilianSpawners.Count; i++)
                {
                    // Find the closest one to the civilian
                    if (Vector3.Distance(this.transform.position, GameObjectManager.instance.civilianSpawners[i].transform.position) < ClosestDist)
                    {
                        // Set its position to the civilian spawner
                        ClosestDist = Vector3.Distance(this.transform.position, GameObjectManager.instance.civilianSpawners[i].transform.position);
                        pos = GameObjectManager.instance.civilianSpawners[i].transform.position;
                        GetComponent<CivilianNavigation>().targetPos = pos;
                    }
                }
                break;
            case QuestList.FIND_QITEM:
                questDialogueNo = Random.Range(0, startDialogueCollect.Length);
                currentPopupTime = popupTime;
                if (MissionCompleted == true && MissionAvailable == false)
                {
                    GetComponent<CivilianNavigation>().enabled = true;
                }
                break;
            case QuestList.GIVE_GUN:
                questDialogueNo = Random.Range(0, startDialogueGive.Length);
                currentPopupTime = popupTime;
                if (MissionCompleted == true && MissionAvailable == false)
                {
                    GetComponent<CivilianNavigation>().enabled = true;
                }
                break;
            case QuestList.RESCUE:
                questDialogueNo = Random.Range(0, startDialogueRescue.Length);
                currentPopupTime = popupTime;
                if (MissionCompleted == true && MissionAvailable == false)
                {
                    GetComponent<CivilianNavigation>().enabled = true;
                }
                break;
        }
	}
	
    void EscortType()
    {
        anim.SetBool("MissionAvailable", MissionAvailable);
        anim.SetBool("MissionCompleted", ObjectiveCompleted);
        anim.SetBool("Talking", Talking);

        // If the AI has reached the target position
        if (Vector3.Distance(transform.position, GetComponent<CivilianNavigation>().targetPos) < 1.1f)
        {
            Debug.Log("You Escorted Me O 3 O");

            GameObjectManager.instance.civiliansEscaped++;

            // Delete this gameobject from the scene
            Destroy(gameObject);
        }

        for (int i = 0; i < playersInRange.Count; i++)
        {
            if (playersInRange[i] == null)
            {
                playersInRange.RemoveAt(i);
                break;
            }
            if (Vector3.Distance(playersInRange[i].transform.position, transform.position) > talkDistance)
            {
                playersInRange.RemoveAt(i);
                break;
            }
            //// Rotate to look at the first player in the List
            //if (i == 0 && ObjectiveCompleted == false)
            //{
            //    Vector3 dir = playersInRange[i].transform.position - transform.position;
            //    Quaternion rot = transform.rotation;
            //    rot.SetLookRotation(new Vector3(dir.x, dir.y, dir.z));

            //    transform.rotation = rot;
            //}
        }

        if (playersInRange.Count > 0)
        {
            // Initial dialogue has not been done
            if (MissionAvailable == true)
            {
                GameObjectManager.instance.HUD.GetComponent<HUDScript>().QuestText.text = startDialogueEscort[questDialogueNo];
                GameObjectManager.instance.HUD.GetComponent<HUDScript>().QuestText.enabled = true;

                currentPopupTime = popupTime;
                Talking = true;
                MissionAvailable = false;
            }
            // Mission had been accepted but not finished
            if (MissionAvailable == false)
            {
                // If the talk time has not finished
                if (currentPopupTime >= 0)
                {
                    Talking = true;
                    currentPopupTime -= Time.deltaTime;

                    // Rotate to look at the first player in the List
                    if (ObjectiveCompleted == false)
                    {
                        Vector3 dir = playersInRange[0].transform.position - transform.position;
                        Quaternion rot = transform.rotation;
                        rot.SetLookRotation(new Vector3(dir.x, dir.y, dir.z));

                        transform.rotation = rot;
                    }
                }
                // If they finished talking
                if (currentPopupTime <= 0)
                {
                    GameObjectManager.instance.HUD.GetComponent<HUDScript>().QuestText.enabled = false;
                    ObjectiveCompleted = true;
                    Talking = false;
                }
            }
            // If they have finished talking
            if (ObjectiveCompleted)
            {
                GetComponent<NavMeshAgent>().enabled = true;
                GetComponent<CivilianNavigation>().enabled = true;
            }
        }
        else
        {
            ObjectiveCompleted = false;
            GetComponent<CivilianNavigation>().enabled = false;
            GetComponent<NavMeshAgent>().enabled = false;
        }

        anim.SetBool("MissionAvailable", MissionAvailable);
        anim.SetBool("MissionCompleted", ObjectiveCompleted);
        anim.SetBool("Talking", Talking);
    }
    public GameObject rthi;
    void FindType()
    {
        anim.SetBool("MissionAvailable", MissionAvailable);
        anim.SetBool("MissionCompleted", MissionCompleted);
        anim.SetBool("Talking", Talking);

        if (MissionCompleted != true)
        {
            // Make sure the references to the player are still useable
            for (int i = 0; i < playersInRange.Count; i++)
            {
                if (playersInRange[i] == null)
                {
                    playersInRange.RemoveAt(i);
                    break;
                }
                if (Vector3.Distance(playersInRange[i].transform.position, transform.position) > talkDistance * 1.5)
                {
                    playersInRange.RemoveAt(i);
                    break;
                }

                if (i == 0)
                {
                    Vector3 dir = playersInRange[i].transform.position - transform.position;
                    Quaternion rot = transform.rotation;
                    rot.SetLookRotation(new Vector3(dir.x, dir.y, dir.z));

                    transform.rotation = rot;
                }
                if (MissionAvailable)
                {
                    currentPopupTime = popupTime;
                    startDialogueCollect[questDialogueNo] = startDialogueCollect[questDialogueNo].Replace("%item%", UniqueItems[uniqueItemNo].name);
                    GameObjectManager.instance.HUD.GetComponent<HUDScript>().QuestText.text = startDialogueCollect[questDialogueNo];
                    GameObjectManager.instance.HUD.GetComponent<HUDScript>().QuestText.enabled = true;
                    rthi = Instantiate(UniqueItems[uniqueItemNo], GameObjectManager.instance.questItemSpawnLocs[Random.Range(0, GameObjectManager.instance.questItemSpawnLocs.Capacity - 1)].transform);
                    rthi.GetComponent<QuestItemHandle>().Manager = this;
                    Talking = true;
                    MissionAvailable = false;
                }

                if (ObjectiveCompleted && !MissionCompleted)
                {
                    currentPopupTime = popupTime;
                    MissionCompleted = true;
                    GameObjectManager.instance.HUD.GetComponent<HUDScript>().QuestText.text = endDialogueCollect[questDialogueNo];
                    GameObjectManager.instance.HUD.GetComponent<HUDScript>().QuestText.enabled = true;
                    Talking = true;
                }
            }
        }

        if (currentPopupTime > 0)
        {
            currentPopupTime -= Time.deltaTime;

            if (currentPopupTime <= 0)
            {
                GameObjectManager.instance.HUD.GetComponent<HUDScript>().QuestText.enabled = false;
                Talking = false;

                if (MissionCompleted)
                {
                    GetComponent<CivilianNavigation>().enabled = true;
                }
            }
        }
    }

	// Update is called once per frame
	void Update ()
    {
        switch (Quest)
        {
            case QuestList.ESCORT:
                EscortType();
                break;
            case QuestList.FIND_QITEM:
                FindType();
                break;
            case QuestList.GIVE_GUN:
                break;
            case QuestList.RESCUE:
                break;
        }

        if (MissionCompleted)
        {
            bool inView = false;
            for (int i = 0; i < GameObjectManager.instance.players.Count; i++)
            {
                if (GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(GameObjectManager.instance.players[i].camera.GetComponent<Camera>()), GetComponent<Collider>().bounds))
                {
                    inView = true;
                    break;
                }
            }
            if (!inView)
            {
                Debug.Log("Can't See Me O 3 O");

                GameObjectManager.instance.civiliansEscaped++;

                // Delete this gameobject from the scene
                Destroy(gameObject);
            }
        }
    }

    void OnTriggerEnter(Collider col)
    {
        // If the thing that collides is the player
        if (col.tag == "Player" && col.GetComponent<ReviveSystem>() != null)
        {
            // If they're in range for talking
            if (Vector3.Distance(col.transform.position, transform.position) < talkDistance)
            {
                playersInRange.Add(col.gameObject);
            }
        }
    }

    void OnTriggerExit(Collider col)
    {

    }

    void OnBecameInvisible()
    {
    }
}
