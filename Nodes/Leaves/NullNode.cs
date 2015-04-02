using System.Collections;

namespace BehaviorTree
{
	public class NullNode : LeafNode
	{
		public override void InitSelf( Hashtable data ) { }

		public override NodeStatus TickSelf()
		{
			return NodeStatus.SUCCESS;
		}
	}
}
