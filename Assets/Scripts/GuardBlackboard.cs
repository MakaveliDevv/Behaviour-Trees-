using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum GuardState
{
    Patrolling,
    SearchingWeapon,
    MovingToWeapon,
    PickingUpWeapon,
    ChasingPlayer,
    AttackingPlayer    
}

public class GuardBlackboard 
{
    public Transform guard;
    public Transform viewOrigin;
    public NavMeshAgent agent;

    public Transform player;
    public PlayerHealth playerHealth;
    public List<Transform> patrolWaypoints;

    public bool hasWeapon;
    public WeaponPickup selectedWeapon;

    public float viewRange;
    public float viewAngle;
    public float attackRange;
    public int attackDamage;
    public float attackCooldown;
    public float weaponSearchRange;
    public float patrolSpeed;
    public float rotationSpeed;

    public LayerMask obstacleMask;
    public LayerMask weaponMask;

    public GuardState currentState;
}

public static class GuardChecks
{
    public static bool PlayerIsAlive(GuardBlackboard blackboard) => blackboard.playerHealth != null && blackboard.playerHealth.IsAlive; 

    public static bool CanSeePlayer(GuardBlackboard blackboard)
    {
        Transform viewTransform = blackboard.viewOrigin != null ? blackboard.viewOrigin : blackboard.guard;
        Vector3 eyePosition = blackboard.viewOrigin != null ? blackboard.viewOrigin.position : blackboard.guard.position + Vector3.up;

        Vector3 toPlayer = blackboard.player.position - eyePosition;
        toPlayer.y = 0f;

        float distance = toPlayer.magnitude;

        if(distance > blackboard.viewRange)
            return false;

        Vector3 forward = viewTransform.forward;
        forward.y = 0f;

        float angle = Vector3.Angle(forward.normalized, toPlayer.normalized);

        Debug.DrawRay(eyePosition, forward.normalized * blackboard.viewRange, Color.blue);
        Debug.DrawRay(eyePosition, toPlayer.normalized * distance, angle <= blackboard.viewAngle * .5f ? Color.green : Color.red);

        if(angle > blackboard.viewAngle * .5f)
            return false;

        bool blocked = Physics.Linecast(
            eyePosition,
            blackboard.player.position + Vector3.up,
            out RaycastHit hit,
            blackboard.obstacleMask
        );

        if(blocked)
        {
            Debug.Log($"View blocked by: {hit.collider.name}");
            return false;
        }

        return true;
    }

    public static bool PlayerIsValidTarget(GuardBlackboard blackboard) => PlayerIsAlive(blackboard) && CanSeePlayer(blackboard);
}
