using UnityEngine;

[RequireComponent(typeof(Rigidbody))] // Ensures enemy has a Rigidbody
public class Enemy : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float stoppingDistance = 1.5f; // Distance to stop from player
    public float rotationSpeed = 5f;
    
    [Header("Attack Settings")]
    public int attackDamage = 10;
    public float attackCooldown = 2f;
    public float attackRange = 1.5f;
    
    [Header("Effects")]
    public ParticleSystem deathParticles;
    public ParticleSystem attackParticles;
    
    private int currentHealth;
    private Transform player;
    private float lastAttackTime;
    private bool isDead = false;
    private Rigidbody rb;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        
        // Find player by tag (make sure your player has the "Player" tag)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("Player not found! Make sure your player has the 'Player' tag.");
        }
    }

    void Update()
    {
        if (isDead || player == null) return;
        
        // Always face the player
        FacePlayer();
        
        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= attackRange)
        {
            // Attack the player if cooldown is over
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                AttackPlayer();
                lastAttackTime = Time.time;
            }
        }
        else if (distanceToPlayer > stoppingDistance)
        {
            // Move towards the player
            MoveTowardsPlayer();
        }
    }

    void FacePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Keep enemy upright
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                rotationSpeed * Time.deltaTime
            );
        }
    }

    void MoveTowardsPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        rb.MovePosition(transform.position + direction * moveSpeed * Time.deltaTime);
    }

    void AttackPlayer()
    {
        if (attackParticles != null)
        {
            attackParticles.Play();
        }
        
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
            Debug.Log("Enemy attacked player for " + attackDamage + " damage!");
        }
        else
        {
            Debug.LogError("PlayerHealth component not found on player!");
        }
    }

    public bool TakeDamage(int damage)
    {
        if (isDead) return true;
        
        currentHealth -= damage;
        
        if (currentHealth <= 0)
        {
            Die();
            return true; // Enemy died
        }
        
        return false; // Enemy still alive
    }

    void Die()
    {
        isDead = true;
        
        // Play death effects
        if (deathParticles != null)
        {
            ParticleSystem deathFX = Instantiate(deathParticles, transform.position, Quaternion.identity);
            deathFX.Play();
            Destroy(deathFX.gameObject, deathFX.main.duration);
        }
        
        // Disable enemy
        Destroy(gameObject);
    }
}