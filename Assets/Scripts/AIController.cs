using UnityEngine;
using System.Collections;

public class AIController : MonoBehaviour
{
	private TreeNode _treeRoot;

	private Hashtable data = new Hashtable();

	void Awake()
	{
		_treeRoot = new MoveToDestination();
		data["gameObject"] = gameObject;
	}

	void Start()
	{
		_treeRoot.Init( data );
	}

	void Update()
	{
		// traverse the tree, ticking all nodes on the current path
		_treeRoot.Tick();
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
	//private AIController _controller;

	//public AIController controller
	//{
	//	get
	//	{
	//		return _controller;
	//	}
	//}

	public abstract NodeStatus status
	{
		get;
	}

	public abstract void Init( Hashtable data );
	public abstract void Tick();
}
