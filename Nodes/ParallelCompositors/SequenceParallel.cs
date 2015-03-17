using System.Collections;

namespace BehaviorTree
{
	public class SequenceParallel : Parallel
	{
		private Hashtable _data;

		public override TreeNode Init( Hashtable data )
		{
			_data = data;
			return base.Init( _data );
		}

		public override NodeStatus Tick( NodeStatus childStatus )
		{
			// make sure we're being treated as a leaf node
			DebugUtils.Assert( childStatus == NodeStatus.SUCCESS );

			int successes = 0;
			foreach ( Subtree subtree in _subtrees )
			{
				switch ( subtree.Tick() )
				{
					case NodeStatus.FAILURE:
						return NodeStatus.FAILURE;

					case NodeStatus.SUCCESS:
						// count successes, if all children succeed then we win
						++successes;

						// restart a subtree if it succeeds
						subtree.Start( _data );
						if ( successes == _subtrees.Count ) return NodeStatus.SUCCESS;
						break;

					case NodeStatus.RUNNING:
					default:
						break;
				}
			}
			return NodeStatus.RUNNING;
		}
	}
}
