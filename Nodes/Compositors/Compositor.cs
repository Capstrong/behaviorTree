using System;
using System.Collections;

namespace BehaviorTree
{
	public class Selector : Compositor
	{
		private int _currentChild = 0;

		public override TreeNode Init( Hashtable data )
		{
			return _children[_currentChild];
		}

		public override NodeStatus Tick( NodeStatus childStatus )
		{
			switch ( childStatus )
			{
				case NodeStatus.SUCCESS:
					_currentChild = 0;
					return NodeStatus.SUCCESS;

				case NodeStatus.FAILURE:
					++_currentChild;
					if ( _currentChild >= _children.Count ) 
					{
						_currentChild = 0;
						return NodeStatus.FAILURE;
					}
					return NodeStatus.RUNNING_CHILDREN;

				default:
					return NodeStatus.RUNNING;
			}
		}
	}
}
