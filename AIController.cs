using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BehaviorTree;

public class AIController : MonoBehaviour
{
	public BehaviorTree.BehaviorTree behavior;

	private Hashtable _data = new Hashtable();

	void Awake()
	{
		_data["gameObject"] = gameObject;
		behavior = CloneTree( behavior );
	}

	void Start()
	{
		behavior.Init( _data );
	}

	void Update()
	{
		NodeStatus status = behavior.Tick();
		switch ( status )
		{
			case NodeStatus.FAILURE:
			case NodeStatus.SUCCESS:
				Debug.Log( "Behavior tree completed with status " + status );
				enabled = false; // stop trying to run the behavior tree.
				break;
		}
	}

	static BehaviorTree.BehaviorTree CloneTree( BehaviorTree.BehaviorTree behaviorTree )
	{
		BehaviorTree.BehaviorTree treeClone = Instantiate<BehaviorTree.BehaviorTree>( behaviorTree );
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
