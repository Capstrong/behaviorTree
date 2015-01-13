using UnityEngine;
using System.Collections;

public class MoveToDestination : TreeNode
{
	public Transform destination;

	private GameObject gameObject;
	private Transform transform;
	private GodInfo info;

	public override NodeStatus status
	{
		get
		{
			if ( Vector3.Distance( transform.position, destination.position ) < 0.1f )
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
		gameObject = (GameObject)data["gameObject"];
		transform = gameObject.GetComponent<Transform>();
		info = gameObject.GetComponent<GodInfo>();
	}

	public override void Tick()
	{
		destination = info.destination; // I guess retrieve the destination every tick because we're dumb
		Vector3 direction = destination.position - transform.position;
		transform.Translate( direction.normalized * 10.0f * Time.deltaTime );
	}
}

public class CollectAdjacentResources : TreeNode
{
	private GodInfo info;
	private GameObject gameObject;
	private Transform transform;

	private NodeStatus _status = NodeStatus.RUNNING;

	public override NodeStatus status
	{
		get
		{
			return _status;
		}
	}

	public override void Init( Hashtable data )
	{
		gameObject = (GameObject)data["gameObject"];
		transform = gameObject.GetComponent<Transform>();
		info = gameObject.GetComponent<GodInfo>();
	}

	public override void Tick()
	{
		// default to failure
		_status = NodeStatus.FAILURE;

		foreach ( GameObject resource in GameObject.FindGameObjectsWithTag( "Resource" ) )
		{
			float distanceSquared = ( resource.GetComponent<Transform>().position - transform.position ).sqrMagnitude;
			if ( distanceSquared < ( info.resourceCollectionDistance * info.resourceCollectionDistance ) )
			{
				// collect resource
				++info.resources;
				GameObject.Destroy( resource );
				_status = NodeStatus.SUCCESS;
			}
		}
	}
}

public class ChooseResourceTarget : TreeNode
{
	private GameObject gameObject;
	private Transform transform;
	private GodInfo info;

	private NodeStatus _status = NodeStatus.RUNNING;

	public override NodeStatus status
	{
		get
		{
			return _status;
		}
	}

	public override void Init( Hashtable data )
	{
		gameObject = (GameObject)data["gameObject"];
		transform = gameObject.GetComponent<Transform>();
		info = gameObject.GetComponent<GodInfo>();
		_status = NodeStatus.RUNNING;
	}

	public override void Tick()
	{
		_status = NodeStatus.FAILURE;
		info.destination = null;
		foreach ( GameObject resource in GameObject.FindGameObjectsWithTag( "Resource" ) )
		{
			_status = NodeStatus.SUCCESS;
			Transform resourceTransform = resource.GetComponent<Transform>();
			if ( info.destination == null )
			{
				info.destination = resourceTransform;
			}
			else if ( ( transform.position - resourceTransform.position ).sqrMagnitude <
			          ( transform.position - info.destination.position ).sqrMagnitude )
			{
				info.destination = resourceTransform;
			}
		}
	}
}

public class CheckForNearbyGods : TreeNode
{
	private NodeStatus _status = NodeStatus.RUNNING;

	public override NodeStatus status
	{
		get
		{
			return NodeStatus.SUCCESS;
		}
	}

	public override void Init( Hashtable data )
	{
		_status = NodeStatus.RUNNING;
	}

	public override void Tick()
	{
	}
}
