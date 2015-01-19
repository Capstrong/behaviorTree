﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class AIController : MonoBehaviour
{
	public BehaviorTree behavior;

	private Hashtable data = new Hashtable();

	void Awake()
	{
		data["gameObject"] = gameObject;
	}

	void Start()
	{
		Debug.Log( "behavior: " + behavior );
		if ( behavior != null )
		{
			Debug.Log( "Root: " + behavior.root );
		}
		behavior.root.Init( data );
	}

	void Update()
	{
		NodeStatus status = behavior.root.Tick();
		if ( status != NodeStatus.RUNNING )
		{
			Debug.Log( "Behavior tree completed, root finished with value " + status );
			Destroy( this );
		}
	}
}
