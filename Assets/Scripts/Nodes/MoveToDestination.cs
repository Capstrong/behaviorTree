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
