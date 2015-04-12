using UnityEngine;
using System.Collections;

using BehaviorTree;

public class Probability : LeafNode
{
	[Range( 0.0f, 1.0f )]
	public float probability;

	public override void InitSelf( Hashtable data ) { }

	public override NodeStatus TickSelf()
	{
		if ( Random.Range( 0.0f, 1.0f ) < probability )
		{
			return NodeStatus.SUCCESS;
		}

		return NodeStatus.FAILURE;
	}
}
