using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace BehaviorTree
{
	public class BehaviorTree : ScriptableObject
	{
		public TreeNode root;

		private ExecutionStack _executionStack;

		public List<EditorData> _editorData = new List<EditorData>();
		public Vector2 _editorOffset = Vector2.zero;

		public void Init( Hashtable data )
		{
			_executionStack = new ExecutionStack( root );
			_executionStack.Start( data );
		}

		public NodeStatus Tick()
		{
			return _executionStack.Tick();
		}

		public static BehaviorTree CloneTree( BehaviorTree behaviorTree )
		{
			BehaviorTree treeClone = Instantiate<BehaviorTree>( behaviorTree );
			treeClone.root = CloneNode( behaviorTree.root );
			return treeClone;
		}

		static TreeNode CloneNode( TreeNode node )
		{
			TreeNode nodeClone = Instantiate<TreeNode>( node );

			if ( node is Decorator )
			{
				( (Decorator)nodeClone )._child = CloneNode( ( (Decorator)node )._child );
			}
			else if ( node is Compositor )
			{
				List<TreeNode> cloneChildren = new List<TreeNode>();
				foreach ( TreeNode child in( (Compositor)node )._children )
				{
					cloneChildren.Add( (TreeNode)CloneNode( child ) );
				}

				( (Compositor)nodeClone )._children = cloneChildren;
			}

			return nodeClone;
		}
	}

	// This is a utility to optimize performance in a behavior tree.
	//
	// This class maintains a stack of the nodes currently being ticked
	// and only ticks the current leaf each frame. If the leaf finishes
	// execution it passes the return value up the stack until the stack
	// is emptied or a node pushes more children on (signified by the
	// RUNNING_CHILDREN status).
	public class ExecutionStack
	{
		private TreeNode _root;

		private Hashtable _data;
		private Stack<TreeNode> _executionStack = new Stack<TreeNode>();

		public ExecutionStack( TreeNode root )
		{
			this._root = root;
		}

		public void Start( Hashtable data )
		{
			_data = data;
			_executionStack.Clear();
			_executionStack.Push( _root );
			PushAllChildren( _root );
		}

		public NodeStatus Tick()
		{
			NodeStatus status = NodeStatus.SUCCESS;
			do
			{
				// Tick current node
				status = _executionStack.Peek().Tick( status );

				switch ( status )
				{
					case NodeStatus.SUCCESS:
					case NodeStatus.FAILURE:
						_executionStack.Pop();
						if ( _executionStack.Count == 0 )
						{
							return status;
						}
						break;

					case NodeStatus.RUNNING_CHILDREN:
						// rebuild the stack with new children
						PushAllChildren( _executionStack.Peek() );
						status = NodeStatus.RUNNING;
						break;

					case NodeStatus.RUNNING:
						// DON'T DO ANYTHING ELSE
						break;
				}
			}
			while ( status != NodeStatus.RUNNING );

			return NodeStatus.RUNNING;
		}

		void PushAllChildren( TreeNode node )
		{
			TreeNode child = node.Init( _data );
			while ( child != null )
			{
				_executionStack.Push( child );
				child = child.Init( _data );
			}
		}
	}
}
