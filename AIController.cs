using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class AIController : MonoBehaviour
{
	public BehaviorTree behavior;

	private Hashtable _data = new Hashtable();

	void Awake()
	{
		_data["gameObject"] = gameObject;
		behavior = BehaviorTreeEditor.CloneTree( behavior );
	}

	void Start()
	{
		behavior.Init( _data );
	}

	void Update()
	{
		NodeStatus status = behavior.Tick();
		switch ( status )
		{
			case NodeStatus.FAILURE:
			case NodeStatus.SUCCESS:
				Debug.Log( "Behavior tree completed with status " + status );
				break;
		}
	}
}
