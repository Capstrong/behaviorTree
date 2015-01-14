using UnityEngine;
using System.Collections;

public class GodInfo : MonoBehaviour
{
	public Transform destination;
	public int resources;
	public float resourceCollectionDistance;
	public float watchDistance;

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
		TreeNode checkForNoGods = new Invert( new GodsWithinWatchDistance() );
		TreeNode[] checkAndCollect_Nodes = { checkForNoGods, collectResources };
		TreeNode checkAndCollect = new SequenceParallel( checkAndCollect_Nodes );

		// FIGHT DEM GODS
		TreeNode chooseGodToChase = new ChooseTargetGod();
		TreeNode chaseGod = new MoveToDestination();
		TreeNode[] chaseSequence_Nodes = { chooseGodToChase, chaseGod };
		TreeNode chaseSequence = new Sequence( chaseSequence_Nodes );

		TreeNode godsPresent = new GodsWithinWatchDistance();
		TreeNode[] fightGods_Nodes = { godsPresent, chaseSequence };
		TreeNode fightGods = new SequenceParallel( fightGods_Nodes );

		TreeNode[] mainSequence = { new Fail( fightGods ),
		                            new Fail( checkAndCollect ),
		                            new ResourcesPresent() };
		TreeNode repeat = new RepeatUntilFail( new Selector( mainSequence ) );
		controller.SetRoot( repeat );
	}
}
