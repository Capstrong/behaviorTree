using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AIController : MonoBehaviour
{
	private TreeNode _treeRoot;

	private Hashtable data = new Hashtable();

	void Awake()
	{
		_treeRoot = new MoveToDestination();
		data["gameObject"] = gameObject;
	}

	void Update()
	{
		NodeStatus status = _treeRoot.Tick();
		if ( status != NodeStatus.RUNNING )
		{
			Debug.Log( "Behavior tree completed, root finished with value " + status );
			Destroy( this );
		}
	}

	public void SetRoot( TreeNode root )
	{
		_treeRoot = root;
		_treeRoot.Init( data );
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
	public TreeNode parent;

	public abstract void Init( Hashtable data );
	public abstract NodeStatus Tick();
}

#region Decorators

public abstract class Decorator : TreeNode
{
	public TreeNode child;

	public Decorator( TreeNode child )
	{
		this.child = child;
	}

	public override void Init( Hashtable data )
	{
		child.Init( data );
	}
}

public class RepeatUntilFail : Decorator
{
	private Hashtable _data;

	public RepeatUntilFail( TreeNode child )
		: base( child )
	{
	}

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
	public Invert( TreeNode child )
		: base( child )
	{
	}

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
	public Succeed( TreeNode child )
		: base( child )
	{
	}

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
	public Fail( TreeNode child )
		: base( child )
	{
	}

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

	public Compositor( TreeNode[] childs )
	{
		children = new List<TreeNode>( childs );
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

	public Sequence( TreeNode[] nodes )
		: base( nodes )
	{
	}

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

	public SequenceParallel( TreeNode[] childs )
		: base( childs )
	{
	}

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

	public Selector( TreeNode[] childs )
		: base( childs )
	{
	}

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
