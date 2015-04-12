using System;
using System.Collections;

namespace BehaviorTree
{
	class Subtree : LeafNode
	{
		public BehaviorTree behaviorTree;

		public override void InitSelf( Hashtable data )
		{
			behaviorTree = BehaviorTree.CloneTree( behaviorTree );
			behaviorTree.Init( data );
		}

		public override NodeStatus TickSelf()
		{
			return behaviorTree.Tick();
		}
	}
}
