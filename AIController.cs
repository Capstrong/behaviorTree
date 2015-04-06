using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BehaviorTree;

namespace BehaviorTree
{
	public class AIController : MonoBehaviour
	{
		public BehaviorTree behavior;

		private Hashtable _data = new Hashtable();

		void Awake()
		{
			_data["gameObject"] = gameObject;
			behavior = BehaviorTree.CloneTree( behavior );
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
					enabled = false; // stop trying to run the behavior tree.
					break;
			}
		}
	}
}
