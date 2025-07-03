using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class BossChase : MonoBehaviour
{
    [Header("Core References")]
    public Transform player;
    private NavMeshAgent agent;
    public Animator animator;

    [Header("Chase Settings")]
    public float chaseSpeed = 3.5f;
    public float chaseAcceleration = 8f;
    public float stoppingDistance = 2f;
    public float rotationSpeed = 120f;

    [Header("Animation")]
    public string moveSpeedParam = "MoveSpeed"; // Changed to match your animator parameter
    public float animationDampTime = 0.15f;
    public float walkAnimationThreshold = 0.1f; // Speed threshold to trigger walk animation

    void Start()
    {
        // Get required components
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        // Configure NavMeshAgent
        agent.speed = chaseSpeed;
        agent.acceleration = chaseAcceleration;
        agent.stoppingDistance = stoppingDistance;
        agent.angularSpeed = rotationSpeed;
        agent.updatePosition = true;
        agent.updateRotation = true;

        // Find player if not assigned
        if (!player)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (!player) Debug.LogError("Player reference not found in BossChase!");
        }

        // Make sure we start on the NavMesh
        StartCoroutine(InitializeNavMeshAgent());
    }

    IEnumerator InitializeNavMeshAgent()
    {
        // Wait one frame to ensure NavMesh system is ready
        yield return null;
        
        if (!agent.isOnNavMesh)
        {
            // Attempt to place on nearest valid NavMesh position
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                Debug.LogError("Failed to place boss on NavMesh!");
                enabled = false;
            }
        }
    }

    void Update()
    {
        if (!CanChase()) return;

        // Set destination every frame (NavMeshAgent will handle pathfinding)
        agent.SetDestination(player.position);

        // Update animation based on agent's velocity
        UpdateAnimation();
    }

    bool CanChase()
    {
        if (!player) return false;
        if (!agent.enabled || !agent.isOnNavMesh) return false;
        return true;
    }

    void UpdateAnimation()
    {
        if (!animator) return;

        // Calculate speed percentage based on current velocity
        float speedPercent = agent.velocity.magnitude / agent.speed;

        // Only set animation parameter if we're moving above the threshold
        if (speedPercent > walkAnimationThreshold)
        {
            animator.SetFloat(
                moveSpeedParam, 
                speedPercent, 
                animationDampTime, 
                Time.deltaTime
            );
        }
        else
        {
            // Smoothly transition back to idle when stopping
            animator.SetFloat(
                moveSpeedParam, 
                0f, 
                animationDampTime, 
                Time.deltaTime
            );
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!agent || !agent.enabled) return;

        // Draw path to destination
        Gizmos.color = Color.cyan;
        if (agent.hasPath)
        {
            for (int i = 0; i < agent.path.corners.Length - 1; i++)
            {
                Gizmos.DrawLine(agent.path.corners[i], agent.path.corners[i + 1]);
                Gizmos.DrawSphere(agent.path.corners[i], 0.1f);
            }
            Gizmos.DrawSphere(agent.path.corners[agent.path.corners.Length - 1], 0.2f);
        }

        // Draw line to current destination
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, agent.destination);
    }

    void OnDisable()
    {
        if (agent && agent.enabled)
        {
            agent.ResetPath();
        }
    }
}
