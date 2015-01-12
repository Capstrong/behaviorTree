using UnityEngine;
using System.Collections;

public class GodInfo : MonoBehaviour
{
	public Transform destination;
	public int resources;
	public float resourceCollectionDistance;

	public void Start()
	{
		AIController controller = GetComponent<AIController>();
		
		TreeNode moveNode = new MoveToDestination();
		TreeNode collectNode = new CollectResource();

		TreeNode[] parallelNodes = { moveNode, collectNode };
		TreeNode parallel = new Parallel( parallelNodes );

		controller.SetRoot( parallel );
	}
}
