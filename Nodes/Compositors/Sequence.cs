using System.Collections;

namespace BehaviorTree
{
	public sealed class Sequence : Compositor
	{
		private int _currentChild = 0;

		/**
		 * @brief Initialize the first child in the sequence.
		 * 
		 * @details
		 *     Sequence only initializes its first child when it is
		 *     initialized. It then initializes each child when it
		 *     becomes the current one.
		 */
		public override TreeNode Init( Hashtable data )
		{
			return _children[_currentChild];
		}

		public override NodeStatus Tick( NodeStatus childStatus )
		{
			// check child status and act as necessary
			switch ( childStatus )
			{
				case NodeStatus.SUCCESS:
					++_currentChild;
					if ( _currentChild >= _children.Count )
					{
						_currentChild = 0;
						return NodeStatus.SUCCESS;
					}
					else
					{
						return NodeStatus.RUNNING_CHILDREN;
					}

				case NodeStatus.FAILURE:
					_currentChild = 0;
					return NodeStatus.FAILURE;

				default:
				case NodeStatus.RUNNING:
					return NodeStatus.RUNNING;
			}
		}
	}
}
