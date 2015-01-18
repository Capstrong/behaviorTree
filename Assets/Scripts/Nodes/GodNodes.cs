using UnityEngine;
using System.Collections;

public class MoveToDestination : TreeNode
{
	private GameObject gameObject;
	private Transform transform;
	private GodInfo info;

	public override void Init( Hashtable data )
	{
		gameObject = (GameObject)data["gameObject"];
		transform = gameObject.GetComponent<Transform>();
		info = gameObject.GetComponent<GodInfo>();
	}

	public override NodeStatus Tick()
	{
		Debug.Log( "Ticking: " + this );

		Vector3 direction = info.destination.position - transform.position;
		transform.Translate( direction.normalized * 10.0f * Time.deltaTime );

		if ( Vector3.Distance( transform.position, info.destination.position ) < 0.5f )
		{
			return NodeStatus.SUCCESS;
		}
		else
		{
			return NodeStatus.RUNNING;
		}
	}
}

public class CollectAdjacentResources : TreeNode
{
	private GodInfo info;
	private GameObject gameObject;
	private Transform transform;

	public override void Init( Hashtable data )
	{
		gameObject = (GameObject)data["gameObject"];
		transform = gameObject.GetComponent<Transform>();
		info = gameObject.GetComponent<GodInfo>();
	}

	public override NodeStatus Tick()
	{
		Debug.Log( "Ticking: " + this );

		foreach ( GameObject resource in GameObject.FindGameObjectsWithTag( "Resource" ) )
		{
			float distanceSquared = ( resource.GetComponent<Transform>().position - transform.position ).sqrMagnitude;
			if ( distanceSquared < ( info.resourceCollectionDistance * info.resourceCollectionDistance ) )
			{
				// collect resource
				++info.resources;
				GameObject.Destroy( resource );
				return NodeStatus.SUCCESS;
			}
		}

		return NodeStatus.FAILURE;
	}
}

/**
 * @brief Check if there are any resources left in the world.
 *
 * @details
 *     Status is SUCCESS if resources exist, FAILURE otherwise.
 */
public class ResourcesPresent : TreeNode
{
	public override void Init( Hashtable data )
	{
	}

	public override NodeStatus Tick()
	{
		Debug.Log( "Ticking: " + this );

		if ( GameObject.FindGameObjectsWithTag( "Resource" ).Length > 0 )
		{
			return NodeStatus.SUCCESS;
		}
		else
		{
			return NodeStatus.FAILURE;
		}
	}
}

public class ChooseResourceTarget : TreeNode
{
	private GameObject gameObject;
	private Transform transform;
	private GodInfo info;

	public override void Init( Hashtable data )
	{
		gameObject = (GameObject)data["gameObject"];
		transform = gameObject.GetComponent<Transform>();
		info = gameObject.GetComponent<GodInfo>();
	}

	public override NodeStatus Tick()
	{
		Debug.Log( "Ticking: " + this );

		info.destination = null;
		NodeStatus status = NodeStatus.FAILURE;
		foreach ( GameObject resource in GameObject.FindGameObjectsWithTag( "Resource" ) )
		{
			status = NodeStatus.SUCCESS;
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
		return status;
	}
}

/**
 * @brief Check if any gods are within the watch distance.
 *
 * @details
 *     Status is FAILURE if no other gods, SUCCESS otherwise.
 */
public class GodsWithinWatchDistance : TreeNode
{
	private Transform transform;
	private GodInfo info;

	public override void Init( Hashtable data )
	{
		GameObject gameObject = (GameObject)data["gameObject"];
		transform = gameObject.GetComponent<Transform>();
		info = gameObject.GetComponent<GodInfo>();
	}

	public override NodeStatus Tick()
	{
		Debug.Log( "Ticking: " + this );

		foreach ( GameObject god in GameObject.FindGameObjectsWithTag( "Enemy God" ) )
		{
			if ( ( transform.position - god.GetComponent<Transform>().position )
				.sqrMagnitude < info.watchDistance * info.watchDistance )
			{
				return NodeStatus.SUCCESS;
			}
		}
		return NodeStatus.FAILURE;
	}
}

public class ChooseTargetGod : TreeNode
{
	private Transform transform;
	private GodInfo info;

	public override void Init( Hashtable data )
	{
		GameObject gameObject = (GameObject)data["gameObject"];
		transform = gameObject.GetComponent<Transform>();
		info = gameObject.GetComponent<GodInfo>();
	}

	public override NodeStatus Tick()
	{
		Debug.Log( "Ticking: " + this );

		foreach ( GameObject god in GameObject.FindGameObjectsWithTag( "Enemy God" ) )
		{
			if ( ( transform.position - god.GetComponent<Transform>().position )
				.sqrMagnitude < info.watchDistance * info.watchDistance )
			{
				info.destination = god.GetComponent<Transform>();
				return NodeStatus.SUCCESS;
			}
		}
		return NodeStatus.FAILURE;
	}
}
