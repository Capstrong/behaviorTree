using UnityEngine;
using System.Collections;

namespace BehaviorTree
{
	public sealed class RepeatUntilSuccess : Decorator
	{
		public override NodeStatus Tick( NodeStatus childStatus )
		{
			switch ( childStatus )
			{
				case NodeStatus.FAILURE:
					return NodeStatus.RUNNING_CHILDREN;

				case NodeStatus.SUCCESS:
					return NodeStatus.SUCCESS;

				default:
					return NodeStatus.RUNNING;
			}
		}
	}
}
