﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class BehaviorTree : ScriptableObject, ISerializationCallbackReceiver
{
	[Serializable]
	public class SerializedNode
	{
		public String typename;
		public List<int> children;
	}

	public TreeNode root;

	public List<SerializedNode> _serializedNodes;

	public void OnBeforeSerialize()
	{
		_serializedNodes = new List<SerializedNode>();
		if ( root != null )
		{
			root.Serialize( _serializedNodes );
		}
	}

	public void OnAfterDeserialize()
	{
		root = ( _serializedNodes.Count > 0 ? DeserializeNode( 0 ) : null );
	}

	TreeNode DeserializeNode( int index )
	{
		List<TreeNode> children = new List<TreeNode>();
		foreach ( int childIndex in _serializedNodes[index].children )
		{
			children.Add( DeserializeNode( childIndex ) );
		}

		TreeNode node = (TreeNode)Activator.CreateInstance( Type.GetType( _serializedNodes[index].typename ) );
		node.SetChildren( children );
		return node;
	}
}

public enum NodeStatus
{
	SUCCESS,
	FAILURE,
	RUNNING
}

public abstract class TreeNode
{
	public abstract int Serialize( List<BehaviorTree.SerializedNode> serializeList );
	public abstract void SetChildren( List<TreeNode> childs );

	public abstract void Init( Hashtable data );
	public abstract NodeStatus Tick();
}

public abstract class LeafNode : TreeNode
{
	public override int Serialize( List<BehaviorTree.SerializedNode> serializeList )
	{
		serializeList.Add( new BehaviorTree.SerializedNode()
		{
			typename = GetType().AssemblyQualifiedName,
			children = new List<int>()
		} );

		return serializeList.Count - 1;
	}

	public override void SetChildren( List<TreeNode> childs )
	{
		if ( childs.Count > 0 )
		{
			throw new NotImplementedException();
		}
	}
}

#region Decorators

public abstract class Decorator : TreeNode
{
	public TreeNode child;

	public override int Serialize( List<BehaviorTree.SerializedNode> serializeList )
	{
		int index = serializeList.Count;
		serializeList.Add( new BehaviorTree.SerializedNode()
		{
			typename = GetType().AssemblyQualifiedName,
			children = new List<int>()
		} );

		BehaviorTree.SerializedNode serializedNode = serializeList[index];
		serializedNode.children.Add( child.Serialize( serializeList ) );
		return index;
	}

	public override void SetChildren( List<TreeNode> childs )
	{
		if ( childs.Count != 1 )
		{
			throw new ArgumentException( "A decorator can only have one child." );
		}

		child = childs[0];
	}

	public override void Init( Hashtable data )
	{
		child.Init( data );
	}
}

public class RepeatUntilFail : Decorator
{
	private Hashtable _data;

	public override void Init( Hashtable data )
	{
		base.Init( data );
		_data = data;
	}

	public override NodeStatus Tick()
	{
		Debug.Log( "Ticking: " + this );

		switch ( child.Tick() )
		{
			case NodeStatus.FAILURE:
				return NodeStatus.SUCCESS;
			case NodeStatus.SUCCESS:
				child.Init( _data );
				return NodeStatus.RUNNING;
			case NodeStatus.RUNNING:
			default:
				return NodeStatus.RUNNING;
		}
	}
}

public class Invert : Decorator
{

	public override NodeStatus Tick()
	{
		Debug.Log( "Ticking: " + this );

		switch ( child.Tick() )
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

	public override NodeStatus Tick()
	{
		Debug.Log( "Ticking: " + this );

		switch ( child.Tick() )
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

	public override NodeStatus Tick()
	{
		Debug.Log( "Ticking: " + this );

		switch ( child.Tick() )
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
	public List<TreeNode> children;

	public override int Serialize( List<BehaviorTree.SerializedNode> serializeList )
	{
		int index = serializeList.Count;

		serializeList.Add( new BehaviorTree.SerializedNode()
		{
			typename = GetType().AssemblyQualifiedName,
			children = new List<int>()
		} );

		foreach ( TreeNode child in children )
		{
			serializeList[index].children.Add( child.Serialize( serializeList ) );
		}

		return index;
	}

	public override void SetChildren( List<TreeNode> childs )
	{
		if ( childs.Count == 0 )
		{
			throw new ArgumentException( "A compositor must have at least one child." );
		}

		children = childs;
	}

	public override void Init( Hashtable data )
	{
		foreach ( TreeNode child in children )
		{
			child.Init( data );
		}
	}
}

public class Sequence : Compositor
{
	private int currentChild;
	private Hashtable _data;

	/**
	 * @brief Initialize the first child in the sequence.
	 * 
	 * @details
	 *     Sequence only initializes its first child when it is
	 *     initialized. It then initializes each child when it
	 *     becomes the current one.
	 */
	public override void Init( Hashtable data )
	{
		_data = data;
		currentChild = 0;
		children[currentChild].Init( _data );
	}

	public override NodeStatus Tick()
	{
		Debug.Log( "Ticking: " + this );

		// check child status and act as necessary
		switch ( children[currentChild].Tick() )
		{
			case NodeStatus.SUCCESS:
				++currentChild;
				if ( currentChild >= children.Count )
				{
					return NodeStatus.SUCCESS;
				}
				else
				{
					children[currentChild].Init( _data );
					return NodeStatus.RUNNING;
				}
			case NodeStatus.FAILURE:
				return NodeStatus.FAILURE;
			default:
			case NodeStatus.RUNNING:
				return NodeStatus.RUNNING;
		}
	}
}

public class SequenceParallel : Compositor
{
	private Hashtable _data;

	public override void Init( Hashtable data )
	{
		base.Init( data );
		_data = data;
	}

	public override NodeStatus Tick()
	{
		Debug.Log( "Ticking: " + this );

		int successes = 0;
		foreach ( TreeNode child in children )
		{
			switch ( child.Tick() )
			{
				case NodeStatus.FAILURE:
					return NodeStatus.FAILURE;
				case NodeStatus.SUCCESS:
					// if all children succeed at once then we win forever
					++successes;
					child.Init( _data );
					if ( successes == children.Count ) return NodeStatus.SUCCESS;
					break;
				case NodeStatus.RUNNING:
				default:
					break;
			}
		}
		return NodeStatus.RUNNING;
	}
}

public class Selector : Compositor
{
	private int _currentChild = 0;

	public override void Init( Hashtable data )
	{
		base.Init( data );
		_currentChild = 0;
	}

	public override NodeStatus Tick()
	{
		Debug.Log( "Ticking: " + this );

		switch ( children[_currentChild].Tick() )
		{
			case NodeStatus.SUCCESS:
				return NodeStatus.SUCCESS;
			case NodeStatus.FAILURE:
				++_currentChild;
				if ( _currentChild >= children.Count ) return NodeStatus.FAILURE;
				return NodeStatus.RUNNING;
			case NodeStatus.RUNNING:
			default:
				return NodeStatus.RUNNING;
		}
	}
}

#endregion