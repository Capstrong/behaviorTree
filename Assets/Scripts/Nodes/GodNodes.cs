using UnityEngine;
using System.Collections;

public class MoveToDestination : TreeNode
{
	public Transform destination;

	private GameObject gameObject;
	private Transform transform;

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
		destination = gameObject.GetComponent<GodInfo>().destination; //(Transform)data["destination"];
	}

	public override void Tick()
	{
		Vector3 direction = destination.position - transform.position;
		transform.Translate( direction.normalized * 10.0f * Time.deltaTime );
	}
}

public class CollectResource : TreeNode
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
