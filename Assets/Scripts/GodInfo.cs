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
		
		// FIND RESOURCES
		TreeNode moveNode = new MoveToDestination();
		TreeNode collectNode = new CollectAdjacentResources();
		TreeNode findResource = new ChooseResourceTarget();
		TreeNode[] collectResources_Nodes = { findResource, moveNode, collectNode };
		TreeNode collectResources = new Sequence( collectResources_Nodes );

		// CHECK AND COLLECT
		TreeNode checkForGods = new CheckForNearbyGods();
		TreeNode[] checkAndCollect_Nodes = { checkForGods, collectResources };
		TreeNode checkAndCollect = new SequenceParallel( checkAndCollect_Nodes );

		Decorator repeat = new RepeatUntilFail();
		repeat.child = checkAndCollect;

		controller.SetRoot( repeat );
	}
}
