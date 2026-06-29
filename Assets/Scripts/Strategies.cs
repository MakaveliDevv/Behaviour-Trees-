using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public interface IStrategy
{
    Node.Status Process();
    void Reset()
    {
        // Noop
    }
}

public class CanSeePlayerStrategy : IStrategy
{
    private readonly GuardBlackboard blackboard;
    public CanSeePlayerStrategy(GuardBlackboard blackboard) => this.blackboard = blackboard;
    public Node.Status Process() => GuardChecks.CanSeePlayer(blackboard) ? Node.Status.Succes : Node.Status.Failure;    
}

public class IsPlayerAliveStrategy : IStrategy
{
    private readonly GuardBlackboard blackboard;
    public IsPlayerAliveStrategy(GuardBlackboard blackboard) => this.blackboard = blackboard;
    public Node.Status Process() => GuardChecks.PlayerIsAlive(blackboard) ? Node.Status.Succes : Node.Status.Failure;
}

public class HasWeaponStrategy : IStrategy
{
    private readonly GuardBlackboard blackboard;
    public HasWeaponStrategy(GuardBlackboard blackboard) => this.blackboard = blackboard;
    public Node.Status Process() => blackboard.hasWeapon ? Node.Status.Succes : Node.Status.Failure;    
}

public class HasNoWeaponStrategy : IStrategy
{
    private readonly GuardBlackboard blackboard;
    public HasNoWeaponStrategy(GuardBlackboard blackboard) => this.blackboard = blackboard;
    public Node.Status Process() => !blackboard.hasWeapon ? Node.Status.Succes : Node.Status.Failure;    
}

public class FindNearestWeaponStrategy : IStrategy
{
    private readonly GuardBlackboard blackboard;

    private const float minSearchDistance = 8f;
    private const float visitedSearchPointRadius = 2f;
    private const int maxRememberedSearchPoints = 12;

    private Vector3 searchDestination;
    private bool hasSearchDestination;
    private float nextSearchDestinationTime;
    private readonly List<Vector3> visitedSearchDestinations = new();

    public FindNearestWeaponStrategy(GuardBlackboard blackboard) => this.blackboard = blackboard;

    public Node.Status Process()
    {
        if(!GuardChecks.PlayerIsValidTarget(blackboard))
            return Node.Status.Failure;

        blackboard.currentState = GuardState.SearchingWeapon;

        WeaponPickup closest = FindClosestWeapon();

        if(closest != null)
        {
            blackboard.selectedWeapon = closest;
            hasSearchDestination = false;
            return Node.Status.Succes;
        }

        SearchRandomNearbyPoint();
        return Node.Status.Running;
    }

    public void Reset()
    {
        hasSearchDestination = false;
        blackboard.selectedWeapon = null;
        visitedSearchDestinations.Clear();
    }

    private WeaponPickup FindClosestWeapon()
    {
        Collider[] hits = Physics.OverlapSphere(
            blackboard.guard.position,
            blackboard.weaponSearchRange,
            blackboard.weaponMask
        );

        WeaponPickup closest = null;
        float closestDistance = Mathf.Infinity;

        foreach(Collider hit in hits)
        {
            if(!hit.TryGetComponent<WeaponPickup>(out var weapon))
                continue;

            if(!weapon.isAvailable)
                continue;

            float distance = Vector3.Distance(
                blackboard.guard.position,
                weapon.transform.position
            );

            if(distance < closestDistance)
            {
                closestDistance = distance;
                closest = weapon;
            }
        }

        return closest;
    }

    private void SearchRandomNearbyPoint()
    {
        if(!ShouldPickNewSearchDestination())
        {
            blackboard.agent.SetDestination(searchDestination);
            return;
        }

        for(int i = 0; i < 40; i++)
        {
            if(i == 20)
                visitedSearchDestinations.Clear();

            float maxSearchDistance = Mathf.Max(minSearchDistance, blackboard.weaponSearchRange);
            float distance = UnityEngine.Random.Range(minSearchDistance, maxSearchDistance);

            Vector2 randomDirection = UnityEngine.Random.insideUnitCircle;

            if(randomDirection.sqrMagnitude < .001f)
                continue;

            randomDirection.Normalize();

            Vector3 randomPoint = blackboard.guard.position + new Vector3
            (
                randomDirection.x * distance,
                0f,
                randomDirection.y * distance
            );

            if(NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                if(IsNearVisitedSearchDestination(hit.position))
                    continue;

                searchDestination = hit.position;
                hasSearchDestination = true;
                nextSearchDestinationTime = Time.time + 2f;
                RememberSearchDestination(searchDestination);

                blackboard.agent.speed = blackboard.patrolSpeed;
                blackboard.agent.SetDestination(searchDestination);
                return;
            }
        }
    }

    private bool ShouldPickNewSearchDestination()
    {
        if(!hasSearchDestination)
            return true;

        if(Time.time >= nextSearchDestinationTime)
            return true;

        return !blackboard.agent.pathPending &&
               blackboard.agent.remainingDistance <= blackboard.agent.stoppingDistance + .3f;
    }

    private bool IsNearVisitedSearchDestination(Vector3 position)
    {
        foreach(Vector3 visitedPosition in visitedSearchDestinations)
        {
            if(Vector3.Distance(position, visitedPosition) <= visitedSearchPointRadius)
                return true;
        }

        return false;
    }

    private void RememberSearchDestination(Vector3 position)
    {
        visitedSearchDestinations.Add(position);

        if(visitedSearchDestinations.Count > maxRememberedSearchPoints)
            visitedSearchDestinations.RemoveAt(0);
    }
}

public class MoveToWeaponStrategy : IStrategy
{
    private readonly GuardBlackboard blackboard;
    public MoveToWeaponStrategy(GuardBlackboard blackboard) => this.blackboard = blackboard;
    public Node.Status Process()
    {
        if(!GuardChecks.PlayerIsValidTarget(blackboard)) return Node.Status.Failure;

        if(blackboard.selectedWeapon == null) return Node.Status.Failure;

        if(!blackboard.selectedWeapon.isAvailable) return Node.Status.Failure;

        blackboard.currentState = GuardState.MovingToWeapon;

        blackboard.agent.SetDestination(blackboard.selectedWeapon.transform.position);

        if(!blackboard.agent.pathPending && 
            blackboard.agent.remainingDistance <= blackboard.agent.stoppingDistance + .2f) return Node.Status.Succes;
        
        return Node.Status.Running;
    }

    public void Reset() => blackboard.selectedWeapon = null;
}

public class PickUpWeaponStrategy : IStrategy
{
    private readonly GuardBlackboard blackboard;
    public PickUpWeaponStrategy(GuardBlackboard blackboard) => this.blackboard = blackboard;
    public Node.Status Process()
    {
        if(!GuardChecks.PlayerIsValidTarget(blackboard)) return Node.Status.Failure;

        if(blackboard.selectedWeapon == null) return Node.Status.Failure;
        
        blackboard.currentState = GuardState.PickingUpWeapon;
        blackboard.selectedWeapon.PickUp();

        blackboard.hasWeapon = true;
        blackboard.selectedWeapon = null;

        return Node.Status.Succes;
    }
}

public class PlayerInAttackRangeStrategy : IStrategy
{
    private readonly GuardBlackboard blackboard;
    public PlayerInAttackRangeStrategy(GuardBlackboard blackboard) => this.blackboard = blackboard;
    public Node.Status Process()
    {
        float distance = Vector3.Distance(blackboard.guard.position, blackboard.player.position);

        return distance <= blackboard.attackRange ? Node.Status.Succes : Node.Status.Failure;
    }
}

public class PlayerNotInAttackRangeStrategy : IStrategy
{
    private readonly GuardBlackboard blackboard;
    public PlayerNotInAttackRangeStrategy(GuardBlackboard blackboard) => this.blackboard = blackboard;
    public Node.Status Process()
    {
        float distance = Vector3.Distance(blackboard.guard.position, blackboard.player.position);

        return distance > blackboard.attackRange ? Node.Status.Succes : Node.Status.Failure;
    }
}

public class ChasePlayerStrategy : IStrategy
{
    private readonly GuardBlackboard blackboard;
    public ChasePlayerStrategy(GuardBlackboard blackboard) => this.blackboard = blackboard;

    private readonly float yawSpeed = 180f;
    private readonly float pitchSpeed = 90f;

    public Node.Status Process()
    {
        if(!GuardChecks.PlayerIsValidTarget(blackboard)) return Node.Status.Failure;

        float distance = Vector3.Distance(blackboard.guard.position, blackboard.player.position);
        if(distance <= blackboard.attackRange) return Node.Status.Succes;

        blackboard.currentState = GuardState.ChasingPlayer;
        blackboard.agent.SetDestination(blackboard.player.position);

        return Node.Status.Running;
    }

    public void LookAtPlayer()
    {
        Vector3 localDirection = blackboard.guard.InverseTransformDirection(blackboard.player.position - blackboard.guard.position);     

        float targetYaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;

        float flatDistance = new Vector2(localDirection.x, localDirection.z).magnitude;
        float targetPitch = -Mathf.Atan2(localDirection.y, flatDistance) * Mathf.Rad2Deg;

        float yawStep = yawSpeed * Time.deltaTime;
        float pitchStep = pitchSpeed * Time.deltaTime;

        float yaw = Mathf.MoveTowardsAngle(0f, targetYaw, yawStep);
        float pitch = Mathf.MoveTowardsAngle(0f, targetPitch, pitchStep);

        blackboard.guard.Rotate(Vector3.up, yaw, Space.Self);
        blackboard.guard.Rotate(Vector3.right, pitch, Space.Self);
    }

}

public class AttackPlayerStrategy : IStrategy
{
    private readonly GuardBlackboard blackboard;
    private float lastAttackTime;

    private readonly float yawSpeed = 180f;
    private readonly float pitchSpeed = 90f;

    public AttackPlayerStrategy(GuardBlackboard blackboard) => this.blackboard = blackboard;
    public Node.Status Process()
    {
        if(!GuardChecks.PlayerIsValidTarget(blackboard)) return Node.Status.Failure;

        float distance = Vector3.Distance(blackboard.guard.position, blackboard.player.position);
        if(distance > blackboard.attackRange) return Node.Status.Failure;

        blackboard.currentState = GuardState.AttackingPlayer;

        blackboard.agent.ResetPath();

        LookAtPlayer();

        if(Time.time >= lastAttackTime + blackboard.attackCooldown)
        {
            blackboard.playerHealth.TakeDamage(blackboard.attackDamage);
            lastAttackTime = Time.time;
        }

        return Node.Status.Running;
    }

    public void LookAtPlayer()
    {
        Vector3 localDirection = blackboard.guard.InverseTransformDirection(blackboard.player.position - blackboard.guard.position);     

        float targetYaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;

        float flatDistance = new Vector2(localDirection.x, localDirection.z).magnitude;
        float targetPitch = -Mathf.Atan2(localDirection.y, flatDistance) * Mathf.Rad2Deg;

        float yawStep = yawSpeed * Time.deltaTime;
        float pitchStep = pitchSpeed * Time.deltaTime;

        float yaw = Mathf.MoveTowardsAngle(0f, targetYaw, yawStep);
        float pitch = Mathf.MoveTowardsAngle(0f, targetPitch, pitchStep);

        blackboard.guard.Rotate(Vector3.up, yaw, Space.Self);
        blackboard.guard.Rotate(Vector3.right, pitch, Space.Self);
    }
}

public class GuardPatrolStrategy : IStrategy
{
    private readonly GuardBlackboard blackboard;
    private int currentIndex;

    public GuardPatrolStrategy(GuardBlackboard blackboard) => this.blackboard = blackboard;
    public Node.Status Process()
    {
        blackboard.currentState = GuardState.Patrolling;

        if(blackboard.patrolWaypoints.Count == 0) return Node.Status.Failure;

        Transform target = blackboard.patrolWaypoints[currentIndex];
        blackboard.agent.SetDestination(target.position);
        blackboard.agent.speed = blackboard.patrolSpeed;

        Vector3 lookTarget = target.position;
        lookTarget.y = 1f;
        blackboard.agent.transform.LookAt(lookTarget);

        if(!blackboard.agent.pathPending &&
            blackboard.agent.remainingDistance <= blackboard.agent.stoppingDistance + .2f)
        {
            currentIndex++;
            if(currentIndex >= blackboard.patrolWaypoints.Count) currentIndex = 0;
        }

        return Node.Status.Running;
    }
}

public class ActionStrategy : IStrategy
{
    readonly Action doSomething;

    public ActionStrategy(Action doSomething)
    {
        this.doSomething = doSomething;
    }

    public Node.Status Process()
    {
        doSomething();
        return Node.Status.Succes;
    }
}

public class PatrolStrategy : IStrategy
{
    private readonly Transform entity;
    private readonly NavMeshAgent agent;
    private readonly List<Transform> patrolPoints;
    private readonly float patrolSpeed;
    private int currentIndex;
    private bool isPathCalculated;

    public PatrolStrategy
    (
        Transform entity,
        NavMeshAgent agent,
        List<Transform> patrolPoints,
        float patrolSpeed = 2f
    )
    {
        this.entity = entity;
        this.agent = agent;
        this.patrolPoints = patrolPoints;
        this.patrolSpeed = patrolSpeed;
    }

    public Node.Status Process()
    {
        if(currentIndex == patrolPoints.Count) return Node.Status.Succes;

        Transform target = patrolPoints[currentIndex];
        agent.SetDestination(target.position);
        agent.speed = patrolSpeed;
        entity.LookAt(target);

        if(isPathCalculated && agent.remainingDistance < 0.1f)
        {
            currentIndex++;
            isPathCalculated = false;
        }

        if(agent.pathPending) isPathCalculated = true;

        return Node.Status.Running;
    }

    public void Reset() => currentIndex = 0;
}

public class MoveToTarget : IStrategy
{
    private readonly Transform entity;
    private readonly NavMeshAgent agent;
    private readonly Transform targetPoint;
    private readonly float moveSpeed;

    public MoveToTarget
    (
        Transform entity,
        NavMeshAgent agent,
        Transform targetPoint,
        float moveSpeed = 4f
    )
    {
        this.entity = entity;
        this.agent = agent;
        this.targetPoint = targetPoint;
        this.moveSpeed = moveSpeed;
    }

    public Node.Status Process()
    {
        agent.SetDestination(targetPoint.position);
        agent.speed = moveSpeed;
        entity.LookAt(targetPoint);

        if(!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f) return Node.Status.Succes;

        return Node.Status.Running;
    }
}

public class Condition : IStrategy
{
    private readonly Func<bool> predicate;

    public Condition(Func<bool> predicate)
    {
        this.predicate = predicate;
    }

    public Node.Status Process() => predicate() ? Node.Status.Succes : Node.Status.Failure;
}
