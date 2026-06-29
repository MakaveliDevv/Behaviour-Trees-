using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GuardEnemy : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private List<Transform> patrolWaypoints;

    [Header("Detection")]
    [SerializeField] private Transform viewOrigin;
    [SerializeField] private float viewRange = 12f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Combat")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackCooldown = 1f;

    [Header("Weapons")]
    [SerializeField] private float weaponSearchRange = 15f;
    [SerializeField] private LayerMask weaponMask;

    [SerializeField] private float patrolSpeed = 5f;
    [SerializeField] private float rotationSpeed = 5f;

    private NavMeshAgent agent;
    private BehaviourTree tree;
    private GuardBlackboard blackboard;

    public GuardState CurrentState => blackboard.currentState;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        blackboard = new GuardBlackboard
        {
            guard = transform,
            viewOrigin = viewOrigin,
            agent = agent,
            player = player,
            playerHealth = playerHealth,
            viewRange = viewRange,
            viewAngle = viewAngle,
            attackRange = attackRange,
            attackDamage = attackDamage,
            attackCooldown = attackCooldown,
            weaponSearchRange = weaponSearchRange,
            patrolWaypoints = patrolWaypoints,
            patrolSpeed = patrolSpeed,
            rotationSpeed = rotationSpeed,
            obstacleMask = obstacleMask,
            weaponMask = weaponMask,
            hasWeapon = false
        };

        tree = BuildTree();
    }

    private BehaviourTree BuildTree()
    {
        BehaviourTree tree = new("Guard Tree");

        PrioritySelector root = new("Guard Root");

        Sequence handlePlayer = new("Handle Player", 100);
        handlePlayer.AddChild(new Leaf("Can See Player?", new CanSeePlayerStrategy(blackboard)));
        handlePlayer.AddChild(new Leaf("Player Alive?", new IsPlayerAliveStrategy(blackboard)));

        Selector weaponOrCombat = new("Weapon Or Combat");

        Sequence getWeapon = new("Get Weapon");
        getWeapon.AddChild(new Leaf("Has No Weapon?", new HasNoWeaponStrategy(blackboard)));
        getWeapon.AddChild(new Leaf("Find Weapon", new FindNearestWeaponStrategy(blackboard)));
        getWeapon.AddChild(new Leaf("Move To Weapon", new MoveToWeaponStrategy(blackboard)));
        getWeapon.AddChild(new Leaf("Pick Up Weapn", new PickUpWeaponStrategy(blackboard)));

        Selector attackOrChase = new("Attack Or Chase");

        Sequence attack = new("Attack");
        attack.AddChild(new Leaf("Has Weapon?", new HasWeaponStrategy(blackboard)));
        attack.AddChild(new Leaf("Player In Attack Range?", new PlayerInAttackRangeStrategy(blackboard)));
        attack.AddChild(new Leaf("Attack Player", new AttackPlayerStrategy(blackboard)));

        Sequence chase = new("Chase");
        chase.AddChild(new Leaf("Has Weapon?", new HasWeaponStrategy(blackboard)));
        chase.AddChild(new Leaf("Player Not In Attack Range?", new PlayerNotInAttackRangeStrategy(blackboard)));
        chase.AddChild(new Leaf("Chase Player", new ChasePlayerStrategy(blackboard)));

        attackOrChase.AddChild(attack);
        attackOrChase.AddChild(chase);

        weaponOrCombat.AddChild(getWeapon);
        weaponOrCombat.AddChild(attackOrChase);
        
        handlePlayer.AddChild(weaponOrCombat);

        Leaf patrol = new("Patrol", new GuardPatrolStrategy(blackboard));

        root.AddChild(handlePlayer);
        root.AddChild(patrol);

        tree.AddChild(root);

        return tree;
    }

    private void Update() => tree.Process();
}
