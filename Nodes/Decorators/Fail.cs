using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BehaviorTree
{
	public sealed class Fail : Decorator
	{
		public override NodeStatus Tick( NodeStatus childStatus )
		{
			switch ( childStatus )
			{
				case NodeStatus.SUCCESS:
				case NodeStatus.FAILURE:
					return NodeStatus.FAILURE;

				case NodeStatus.RUNNING:
				default:
					return NodeStatus.RUNNING;
			}
		}
	}
}
