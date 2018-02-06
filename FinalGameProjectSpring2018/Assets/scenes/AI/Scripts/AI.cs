﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AI : MonoBehaviour 
{

	public Transform destination;
	public Transform DatDerBadGoober;
	private NavMeshAgent agent;

	void Start(){
        destination = GameObject.FindGameObjectWithTag("player").transform;
        //destination = GameObject.Find("player").transform;
        DatDerBadGoober = GameObject.FindGameObjectWithTag("player").transform;
        //DatDerBadGoober = GameObject.Find ("player").transform;
    }

	void Update () 
	{
		float dist = Vector3.Distance (DatDerBadGoober.position, transform.position);
		//Debug.Log ("Distance to AI: " + dist);

		if (dist <= 5) {
			agent = gameObject.GetComponent<NavMeshAgent> ();

			agent.SetDestination (destination.position);
		}
	}

}
