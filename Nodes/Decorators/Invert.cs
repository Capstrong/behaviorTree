using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BehaviorTree
{
	public sealed class Invert : Decorator
	{
		public override NodeStatus Tick( NodeStatus childStatus )
		{
			switch ( childStatus )
			{
				case NodeStatus.SUCCESS:
					return NodeStatus.FAILURE;

				case NodeStatus.FAILURE:
					return NodeStatus.SUCCESS;

				case NodeStatus.RUNNING:
				default:
					return NodeStatus.RUNNING;
			}
		}
	}
}
