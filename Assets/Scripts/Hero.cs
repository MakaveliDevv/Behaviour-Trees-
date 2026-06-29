using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Hero : MonoBehaviour
{
    [SerializeField] private List<Transform> waypoints = new();
    [SerializeField] GameObject treasure;
    [SerializeField] GameObject treasure2;
    [SerializeField] GameObject safeSpot;
    [SerializeField] bool inDanger;

    private NavMeshAgent agent;

    private BehaviourTree tree;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        tree = new("Hero");

        PrioritySelector actions = new PrioritySelector("Agent Lofic");

        ReactiveSequence runToSafetySeq = new ReactiveSequence ("RunToSafety", 100);

        bool IsSafe()
        {
            if(!inDanger)
            {
                runToSafetySeq.Reset();
                return false;
            }

            return true;
        }

        runToSafetySeq.AddChild(new Leaf("isSafe?", new Condition(IsSafe)));
        runToSafetySeq.AddChild(new Leaf("Go To Safety", new MoveToTarget(transform, agent, safeSpot.transform)));
        actions.AddChild(runToSafetySeq);

        Selector goToTreasure = new RandomSelector("GoToTreasure", 50);
        
        Sequence getTreasure1 = new Sequence("GetTreasure1");
        getTreasure1.AddChild(new Leaf("isTreasure1?", new Condition(() => treasure.activeSelf)));
        getTreasure1.AddChild(new Leaf("GoToTreasure1", new MoveToTarget(transform, agent, treasure.transform)));
        getTreasure1.AddChild(new Leaf("PickUpTreasure1", new ActionStrategy(() => treasure.SetActive(false))));
        goToTreasure.AddChild(getTreasure1);

        Sequence getTreasure2 = new Sequence("GetTreasure2");
        getTreasure2.AddChild(new Leaf("isTreasure2?", new Condition(() => treasure2.activeSelf)));
        getTreasure2.AddChild(new Leaf("GoToTreasure2", new MoveToTarget(transform, agent, treasure2.transform)));
        getTreasure2.AddChild(new Leaf("PickUpTreasure2", new ActionStrategy(() => treasure2.SetActive(false))));
        goToTreasure.AddChild(getTreasure2);

        actions.AddChild(goToTreasure);

        Leaf patrol = new Leaf("Patrol", new PatrolStrategy(transform, agent, waypoints));
        actions.AddChild(patrol);

        tree.AddChild(actions);

        // tree.AddChild(new Leaf("Patrol", new PatrolStrategy(transform, agent, waypoints)));

        // Sequence GoToTreasure = new Sequence("GoToTreasure", 20);
        // GoToTreasure.AddChild(new Leaf("IsTreasurePresent", new Condition(() => treasure.activeSelf)));
        // GoToTreasure.AddChild(new Leaf("MoveToTreasure", new ActionStrategy(() => agent.SetDestination(treasure.transform.position))));
        
        // Sequence GoToTreasure2 = new Sequence("GoToTreasure2", 10);
        // GoToTreasure2.AddChild(new Leaf("IsTreasure2Present", new Condition(() => treasure2.activeSelf)));
        // GoToTreasure2.AddChild(new Leaf("MoveToTreasure2", new ActionStrategy(() => agent.SetDestination(treasure2.transform.position))));

        // // Selector goToTreasures = new Selector("GoToTreasures");
        // PrioritySelector goToTreasures = new PrioritySelector("GoToTreasures");
        // goToTreasures.AddChild(GoToTreasure2);
        // goToTreasures.AddChild(GoToTreasure);

        // tree.AddChild(goToTreasures);
    }
    
    private void Update()
    {
        tree.Process();
    }
}