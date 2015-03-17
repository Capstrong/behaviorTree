using UnityEngine;
using System.Collections;

namespace BehaviorTree
{
	public sealed class RepeatUntilFail : Decorator
	{
		private Hashtable _data;

		public override TreeNode Init( Hashtable data )
		{
			_data = data;
			return base.Init( data );
		}

		public override NodeStatus Tick( NodeStatus childStatus )
		{
			switch ( childStatus )
			{
				case NodeStatus.FAILURE:
					return NodeStatus.SUCCESS;

				case NodeStatus.SUCCESS:
					_child.Init( _data );
					return NodeStatus.RUNNING_CHILDREN;

				case NodeStatus.RUNNING:
				default:
					return NodeStatus.RUNNING;
			}
		}
	}
}
