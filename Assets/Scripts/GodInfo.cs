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
		TreeNode collectNode = new CollectAdjacentResources();
		TreeNode findResource = new ChooseResourceTarget();

		TreeNode[] sequenceNodes = { findResource, moveNode, collectNode };
		TreeNode sequence = new Sequence( sequenceNodes );

		Decorator repeat = new RepeatUntilFail();
		repeat.child = sequence;

		controller.SetRoot( repeat );
	}
}
