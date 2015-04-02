using UnityEngine;
using System.Collections;

namespace BehaviorTree
{
	public sealed class RepeatUntilFail : Decorator
	{
		public override NodeStatus Tick( NodeStatus childStatus )
		{
			switch ( childStatus )
			{
				case NodeStatus.FAILURE:
					return NodeStatus.SUCCESS;

				case NodeStatus.SUCCESS:
					return NodeStatus.RUNNING_CHILDREN;

				case NodeStatus.RUNNING:
				default:
					return NodeStatus.RUNNING;
			}
		}
	}
}
