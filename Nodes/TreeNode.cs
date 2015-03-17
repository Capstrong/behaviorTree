using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BehaviorTree
{
	public enum NodeStatus
	{
		SUCCESS,
		FAILURE,
		RUNNING,         // Signals the node should be ticked again the next frame.
		RUNNING_CHILDREN // Like RUNNING, but signals to the AIController that it has chilren that should be pushed on to the execution stack.
	}

	public abstract class TreeNode : ScriptableObject
	{
		public abstract TreeNode Init( Hashtable data );

		/**
		 * @note
		 *     If the node has no children, it will be passed
		 *     NodeStatus.SUCCESS as the childStatus.
		 */
		public abstract NodeStatus Tick( NodeStatus childStatus );
	}

	public abstract class Decorator : TreeNode
	{
		public TreeNode _child;

		public override TreeNode Init( Hashtable data )
		{
			return _child;
		}
	}

	public abstract class Compositor : TreeNode
	{
		public List<TreeNode> _children = new List<TreeNode>();
	}

	public abstract class Parallel : Compositor
	{
		public List<Subtree> _subtrees = new List<Subtree>();

		public override TreeNode Init( Hashtable data )
		{
			if ( _subtrees.Count != _children.Count )
			{
				// If we don't have the right number of subtrees,
				// recreate the subtree list.
				_subtrees = new List<Subtree>();
				foreach ( TreeNode child in _children )
				{
					Subtree subtree = new Subtree( child );
					_subtrees.Add( subtree );
					subtree.Start( data );
				}
			}

			return null; // pretend we're a leaf node, we need to be ticked every frame
		}
	}

	public abstract class LeafNode : TreeNode
	{
		public override TreeNode Init( Hashtable data )
		{
			InitSelf( data );
			return null;
		}

		public override NodeStatus Tick( NodeStatus childStatus )
		{
			return TickSelf();
		}

		public abstract void InitSelf( Hashtable data );
		public abstract NodeStatus TickSelf();
	}
}
