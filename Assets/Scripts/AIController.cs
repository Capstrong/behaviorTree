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
		// traverse the tree, ticking all nodes on the current path
		_treeRoot.Tick();
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

	public abstract NodeStatus status
	{
		get;
	}

	public abstract void Init( Hashtable data );
	public abstract void Tick();
}

public class RepeatUntilFail : TreeNode
{
	TreeNode child;

	public override NodeStatus status
	{
		get
		{
			if ( child.status == NodeStatus.RUNNING || child.status == NodeStatus.SUCCESS )
			{
				return NodeStatus.RUNNING;
			}
			else
			{
				return NodeStatus.SUCCESS;
			}
		}
	}

	public override void Init( Hashtable data )
	{
		child.Init( data );
	}

	public override void Tick()
	{
		child.Tick();
	}
}

public class Parallel : TreeNode
{
	public List<TreeNode> children;

	public Parallel( TreeNode[] childs )
	{
		children = new List<TreeNode>( childs );
	}

	public override NodeStatus status
	{
		get
		{
			return NodeStatus.RUNNING; // TODO return an actual
		}
	}

	public override void Init( Hashtable data )
	{
		foreach ( TreeNode child in children )
		{
			child.Init( data );
		}
	}

	public override void Tick()
	{
		foreach ( TreeNode child in children )
		{
			child.Tick();
		}
	}
}
