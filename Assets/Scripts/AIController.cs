﻿using UnityEngine;
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
		if ( _treeRoot.status == NodeStatus.RUNNING )
		{
			_treeRoot.Tick();
		}
		else
		{
			Debug.Log( "Behavior tree completed, root finished with value " + _treeRoot.status );
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

	/**
	 * @todo remove TreeNode.status and have Tick() return the status instead.
	 */
	public abstract NodeStatus status
	{
		get;
	}

	public abstract void Init( Hashtable data );
	public abstract void Tick();
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

	public override NodeStatus status
	{
		get
		{
			if ( child.status == NodeStatus.FAILURE )
			{
				return NodeStatus.SUCCESS;
			}
			else
			{
				return NodeStatus.RUNNING;
			}
		}
	}

	public override void Init( Hashtable data )
	{
		base.Init( data );
		_data = data;
	}

	public override void Tick()
	{
		Debug.Log( "Ticking: " + this );

		child.Tick();

		if ( child.status == NodeStatus.SUCCESS )
		{
			// restart child when it completes
			child.Init( _data );
		}
	}
}

public class Invert : Decorator
{
	public override NodeStatus status
	{
		get
		{
			switch ( child.status )
			{
				case NodeStatus.FAILURE:
					return NodeStatus.SUCCESS;
				case NodeStatus.SUCCESS:
					return NodeStatus.FAILURE;
			}
			return NodeStatus.RUNNING;
		}
	}

	public Invert( TreeNode child )
		: base( child )
	{
	}

	public override void Tick()
	{
		Debug.Log( "Ticking: " + this );

		child.Tick();
	}
}

public class Succeed : Decorator
{
	private NodeStatus _status = NodeStatus.RUNNING;

	public Succeed( TreeNode child )
		: base( child )
	{
	}

	public override NodeStatus status
	{
		get { return _status; }
	}

	public override void Init( Hashtable data )
	{
		base.Init( data );
		_status = NodeStatus.RUNNING;
	}

	public override void Tick()
	{
		child.Tick();
		if ( child.status != NodeStatus.RUNNING )
		{
			_status = NodeStatus.SUCCESS;
		}
	}
}

public class Fail : Decorator
{
	private NodeStatus _status = NodeStatus.RUNNING;

	public Fail( TreeNode child )
		: base( child )
	{
	}

	public override NodeStatus status
	{
		get { return _status; }
	}

	public override void Init( Hashtable data )
	{
		base.Init( data );
		_status = NodeStatus.RUNNING;
	}

	public override void Tick()
	{
		child.Tick();
		if ( child.status != NodeStatus.RUNNING )
		{
			_status = NodeStatus.FAILURE;
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
	private NodeStatus _status = NodeStatus.RUNNING;
	private Hashtable _data;

	public Sequence( TreeNode[] nodes )
		: base( nodes )
	{
	}

	public override NodeStatus status
	{
		get
		{
			return _status;
		}
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
		_status = NodeStatus.RUNNING;
	}

	public override void Tick()
	{
		Debug.Log( "Ticking: " + this );

		children[currentChild].Tick();

		// check child status and act as necessary
		switch (children[currentChild].status)
		{
			case NodeStatus.SUCCESS:
				++currentChild;
				if ( currentChild >= children.Count )
				{
					_status = NodeStatus.SUCCESS;
				}
				else
				{
					children[currentChild].Init( _data );
				}
				break;
			case NodeStatus.FAILURE:
				_status = NodeStatus.FAILURE;
				currentChild = 0;
				break;
		}
	}
}

public class SequenceParallel : Compositor
{
	private NodeStatus _status = NodeStatus.RUNNING;

	public SequenceParallel( TreeNode[] childs )
		: base( childs )
	{
	}

	public override NodeStatus status
	{
		get
		{
			return _status; // TODO return an actual
		}
	}

	public override void Init( Hashtable data )
	{
		base.Init( data );
		_status = NodeStatus.RUNNING;
	}

	public override void Tick()
	{
		Debug.Log( "Ticking: " + this );

		foreach ( TreeNode child in children )
		{
			child.Tick();

			_status = NodeStatus.SUCCESS;
			switch ( child.status )
			{
				case NodeStatus.FAILURE:
					_status = NodeStatus.FAILURE;
					return;
				case NodeStatus.RUNNING:
					// what do we do when one of our children finishes before the others?
					// for now make the status success and continue running other children.
					if ( _status != NodeStatus.FAILURE )
					{
						_status = NodeStatus.RUNNING;
					}
					break;
			}
		}
	}
}

public class Selector : Compositor
{
	private NodeStatus _status = NodeStatus.RUNNING;
	private int _currentChild = 0;

	public Selector( TreeNode[] childs )
		: base( childs )
	{
	}

	public override NodeStatus status
	{
		get
		{
			return _status;
		}
	}

	public override void Init( Hashtable data )
	{
		base.Init( data );
		_status = NodeStatus.RUNNING;
		_currentChild = 0;
	}

	public override void Tick()
	{
		Debug.Log( "Ticking: " + this );

		children[_currentChild].Tick();

		switch ( children[_currentChild].status )
		{
			case NodeStatus.SUCCESS:
				_status = NodeStatus.SUCCESS;
				return;
			case NodeStatus.FAILURE:
				++_currentChild;
				if ( _currentChild >= children.Count )
				{
					_status = NodeStatus.FAILURE;
					return;
				}
				break;
		}
	}
}

#endregion
