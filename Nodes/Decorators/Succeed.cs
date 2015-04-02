using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BehaviorTree
{
	public sealed class Succeed : Decorator
	{
		public override NodeStatus Tick( NodeStatus childStatus )
		{
			switch ( childStatus )
			{
				case NodeStatus.FAILURE:
				case NodeStatus.SUCCESS:
					return NodeStatus.SUCCESS;

				case NodeStatus.RUNNING:
				default:
					return NodeStatus.RUNNING;
			}
		}
	}
}
