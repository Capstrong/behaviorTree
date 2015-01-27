using UnityEngine;
using UnityEditor;
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

	public TreeNode root = new NullNode();

	public List<SerializedNode> _serializedNodes;

	public void OnBeforeSerialize()
	{
		_serializedNodes = new List<SerializedNode>();
		if ( root != null )
		{
			root.Serialize( _serializedNodes );
		}

		EditorUtility.SetDirty( this );
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
	public abstract void OnGUI();

	public abstract void Init( Hashtable data );
	public abstract NodeStatus Tick();
}

#region Decorators

public abstract class Decorator : TreeNode
{
	public TreeNode _child = new NullNode();

	public override int Serialize( List<BehaviorTree.SerializedNode> serializeList )
	{
		int index = serializeList.Count;
		serializeList.Add( new BehaviorTree.SerializedNode()
		{
			typename = GetType().AssemblyQualifiedName,
			children = new List<int>()
		} );

		BehaviorTree.SerializedNode serializedNode = serializeList[index];
		serializedNode.children.Add( _child.Serialize( serializeList ) );
		return index;
	}

	public override void SetChildren( List<TreeNode> childs )
	{
		if ( childs.Count != 1 )
		{
			throw new ArgumentException( "A decorator can only have one child." );
		}

		_child = childs[0];
	}

	public override void OnGUI()
	{
		++EditorGUI.indentLevel;

		Type resultType = BehaviorTreeEditor.CreateNodeTypeSelector( _child );
		if ( resultType != _child.GetType() )
		{
			_child = BehaviorTreeEditor.CreateNode( resultType );
		}

		_child.OnGUI();

		--EditorGUI.indentLevel;
	}

	public override void Init( Hashtable data )
	{
		_child.Init( data );
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
		switch ( _child.Tick() )
		{
			case NodeStatus.FAILURE:
				return NodeStatus.SUCCESS;
			case NodeStatus.SUCCESS:
				_child.Init( _data );
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
		switch ( _child.Tick() )
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
		switch ( _child.Tick() )
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
		switch ( _child.Tick() )
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
	public List<TreeNode> _children = new List<TreeNode>() { new NullNode() };

	public override int Serialize( List<BehaviorTree.SerializedNode> serializeList )
	{
		int index = serializeList.Count;

		serializeList.Add( new BehaviorTree.SerializedNode()
		{
			typename = GetType().AssemblyQualifiedName,
			children = new List<int>()
		} );

		foreach ( TreeNode child in _children )
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

		_children = childs;
	}

	public override void OnGUI()
	{
		++EditorGUI.indentLevel;

		for ( int childIndex = 0; childIndex < _children.Count; ++childIndex )
		{
			Type resultType = BehaviorTreeEditor.CreateNodeTypeSelector( _children[childIndex] );
			if ( resultType != _children[childIndex].GetType() )
			{
				_children[childIndex] = BehaviorTreeEditor.CreateNode( resultType );
			}

			_children[childIndex].OnGUI();
		}

		if ( GUILayout.Button( "Add Child" ) )
		{
			_children.Add( new NullNode() );
		}

		--EditorGUI.indentLevel;
	}

	public override void Init( Hashtable data )
	{
		foreach ( TreeNode child in _children )
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
		_children[currentChild].Init( _data );
	}

	public override NodeStatus Tick()
	{
		// check child status and act as necessary
		switch ( _children[currentChild].Tick() )
		{
			case NodeStatus.SUCCESS:
				++currentChild;
				if ( currentChild >= _children.Count )
				{
					return NodeStatus.SUCCESS;
				}
				else
				{
					_children[currentChild].Init( _data );
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
		int successes = 0;
		foreach ( TreeNode child in _children )
		{
			switch ( child.Tick() )
			{
				case NodeStatus.FAILURE:
					return NodeStatus.FAILURE;
				case NodeStatus.SUCCESS:
					// if all children succeed at once then we win forever
					++successes;
					child.Init( _data );
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
		switch ( _children[_currentChild].Tick() )
		{
			case NodeStatus.SUCCESS:
				return NodeStatus.SUCCESS;
			case NodeStatus.FAILURE:
				++_currentChild;
				if ( _currentChild >= _children.Count ) return NodeStatus.FAILURE;
				return NodeStatus.RUNNING;
			case NodeStatus.RUNNING:
			default:
				return NodeStatus.RUNNING;
		}
	}
}

#endregion

#region Leaves

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

	public override void OnGUI() { }
}

public class NullNode : LeafNode
{
	public override void Init( Hashtable data ) { }
	public override NodeStatus Tick()
	{
		return NodeStatus.SUCCESS;
	}
}

#endregion
