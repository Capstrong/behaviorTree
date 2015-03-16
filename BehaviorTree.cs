using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class BehaviorTree : ScriptableObject
{
	public TreeNode root;
}

public enum NodeStatus
{
	SUCCESS,
	FAILURE,
	RUNNING,         // Signals the node should be ticked again the next frame.
	RUNNING_CHILDREN // Like RUNNING, but signals to the AIController that it has chilren that should be pushed on to the execution stack.
}

public abstract class TreeNode : ScriptableObject
{
	public abstract void OnGUI();

	public abstract TreeNode Init( Hashtable data );

	/**
	 * @note
	 *     If the node has no children, it will be passed
	 *     NodeStatus.SUCCESS as the childStatus.
	 */
	public abstract NodeStatus Tick( NodeStatus childStatus );
}

#region Decorators

public abstract class Decorator : TreeNode
{
	public TreeNode _child;

	public override void OnGUI()
	{
		#if UNITY_EDITOR
		++EditorGUI.indentLevel;

		Type resultType = BehaviorTreeEditor.CreateNodeTypeSelector( _child );
		if ( _child )
		{
			if ( resultType != _child.GetType() )
			{
				BehaviorTreeEditor.DeleteNode( _child );
				_child = BehaviorTreeEditor.CreateNode( resultType );
			}
		}
		else
		{
			_child = BehaviorTreeEditor.CreateNode( resultType );
		}

		_child.OnGUI();

		--EditorGUI.indentLevel;
		#endif
	}

	public override TreeNode Init( Hashtable data )
	{
		return _child;
	}
}

public class RepeatUntilFail : Decorator
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

public class Invert : Decorator
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

public class Succeed : Decorator
{
	public override NodeStatus Tick( NodeStatus childStatus )
	{
		switch ( childStatus )
		{
			case NodeStatus.FAILURE:
			case NodeStatus.SUCCESS:
				return NodeStatus.SUCCESS;

			case NodeStatus.RUNNING:
			default:
				return NodeStatus.RUNNING;
		}
	}
}

public class Fail : Decorator
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

#endregion

#region Compositors

public abstract class Compositor : TreeNode
{
	public List<TreeNode> _children = new List<TreeNode>();

	public override void OnGUI()
	{
		#if UNITY_EDITOR
		++EditorGUI.indentLevel;

		for ( int childIndex = 0; childIndex < _children.Count; ++childIndex )
		{
			Type resultType = BehaviorTreeEditor.CreateNodeTypeSelector( _children[childIndex] );
			if ( resultType != _children[childIndex].GetType() )
			{
				BehaviorTreeEditor.DeleteNode( _children[childIndex] );
				_children[childIndex] = BehaviorTreeEditor.CreateNode( resultType );
			}

			_children[childIndex].OnGUI();
		}

		if ( GUILayout.Button( "Add Child" ) )
		{
			_children.Add( BehaviorTreeEditor.nullNode );
		}

		--EditorGUI.indentLevel;
		#endif
	}
}

public class Sequence : Compositor
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

			case NodeStatus.RUNNING:
			default:
				return NodeStatus.RUNNING;
		}
	}
}

#endregion

#region Parallel Sequences

public class Subtree
{
	public TreeNode root;

	private Hashtable _data;
	private Stack<TreeNode> _executionStack;

	public Subtree( TreeNode root )
	{
		this.root = root;
	}

	public void Start( Hashtable data )
	{
		_data = data;
		_executionStack = new Stack<TreeNode>();
		_executionStack.Push( root );
		PushAllChildren( root );
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
					// if no more nodes on stack, we're done
					if ( _executionStack.Count == 0 )
					{
						return status;
					}

					// otherwise walk up the stack
					_executionStack.Pop();
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
					if ( successes == _children.Count ) return NodeStatus.SUCCESS;
					break;

				case NodeStatus.RUNNING:
				default:
					break;
			}
		}
		return NodeStatus.RUNNING;
	}
}

#endregion

#region Leaves

public abstract class LeafNode : TreeNode
{
	public override void OnGUI() { }

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

public class NullNode : LeafNode
{
	public override void InitSelf( Hashtable data ) { }

	public override NodeStatus TickSelf()
	{
		return NodeStatus.SUCCESS;
	}
}

#endregion
