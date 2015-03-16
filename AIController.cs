using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class AIController : MonoBehaviour
{
	public BehaviorTree behavior;

	private Hashtable _data = new Hashtable();
	private Stack<TreeNode> _executionStack = new Stack<TreeNode>();

	[ReadOnly]
	public TreeNode currentLeaf;

	void Awake()
	{
		_data["gameObject"] = gameObject;
		//behavior = (BehaviorTree)ScriptableObject.Instantiate( behavior );
	}

	void Start()
	{
		_executionStack.Push( behavior.root );
		PushAllChildren( behavior.root );
	}

	void Update()
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
					// node is done, return to parent
					if ( PopExecutionStack() )
					{
						break;
					}
					else
					{
						Debug.Log( "AIController exited with status " + status );
						enabled = false;
						return;
					}

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
	}

	void PushAllChildren( TreeNode node )
	{
		TreeNode child = node.Init( _data );
		while ( child != null )
		{
			_executionStack.Push( child );
			child = child.Init( _data );
		}

		currentLeaf = _executionStack.Peek();
	}

	/**
	 * @returns
	 *     True if there are still more nodes on the stack,
	 *     false otherwise.
	 */
	bool PopExecutionStack()
	{
		_executionStack.Pop();

		if ( _executionStack.Count > 0 )
		{
			currentLeaf = _executionStack.Peek();
			return true;
		}

		return false;
	}
}
