﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestItemHandle : MonoBehaviour
{
    public CivilianAI Manager;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void OnTriggerEnter(Collider col)
    {
        if (col.tag == "Player" && col.GetComponent<ReviveSystem>() != null)
        {
            Manager.ObjectiveCompleted = true;
            Destroy(gameObject);
        }
    }
}
