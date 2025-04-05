using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Combat States")]
    public bool swordDrawn = false;
    public bool isDrawingSword = false;

    [Header("Attack Parameters")]
    public float attackRange = 1.5f;
    public int[] attackDamage = { 25, 30 };
    public float attackCooldown = 0.5f;
    public float comboWindow = 0.8f;
    private float lastAttackTime = -1f;
    private int currentCombo = 0;

    [Header("Drawing Sword")]
    public float drawSwordTime = 0.5f;
    private float drawSwordTimer = 0f;

    [Header("References")]
    public Transform attackPoint;
    public LayerMask enemyLayers;
    public ParticleSystem bloodParticles;
    public Animator animator;
    
    [Header("Animation Timing")]
    public float[] hitFrames = { 0.3f, 0.2f };
    public float[] resetTimes = { 0.5f, 0.6f };
    private float animationTimer = 0f;
    private bool attackInProgress = false;
    private bool hitDetected = false;
    private bool resetTriggered = false;

    void Update()
    {
        // Handle sword drawing
        if (isDrawingSword)
        {
            drawSwordTimer += Time.deltaTime;
            if (drawSwordTimer >= drawSwordTime)
            {
                FinishDrawingSword();
            }
            return; // Can't do anything else while drawing sword
        }

        // Handle automatic return to idle
        if (attackInProgress)
        {
            animationTimer += Time.deltaTime;
            
            // Check if we should return to idle
            if (ShouldReturnToIdle())
            {
                ResetToIdle();
                return;
            }

            // Normal attack update
            UpdateAttack();
        }

        if (Input.GetButtonDown("Fire1"))
        {
            if (!swordDrawn)
            {
                StartDrawingSword();
            }
            else if (CanAttack())
            {
                StartAttack();
            }
            else if (CanCombo())
            {
                ContinueCombo();
            }
        }
    }

    void StartDrawingSword()
    {
        isDrawingSword = true;
        drawSwordTimer = 0f;
        animator.SetTrigger("drawSword");
    }

    void FinishDrawingSword()
    {
        isDrawingSword = false;
        swordDrawn = true;
        animator.SetBool("swordDrawn", true);
    }

    bool ShouldReturnToIdle()
    {
        // If combo window expired and we're not in a combo
        if (currentCombo == 0 && Time.time > lastAttackTime + comboWindow)
            return true;
        
        // If we've passed the reset time for current attack
        if (animationTimer >= resetTimes[currentCombo] + 0.2f)
            return true;
            
        return false;
    }

    void ResetToIdle()
    {
        attackInProgress = false;
        currentCombo = 0;
        animationTimer = 0f;
        animator.SetInteger("comboStep", 0);
        animator.SetTrigger("attackReset");
        animator.ResetTrigger("attackTrigger");
    }

    bool CanAttack()
    {
        return swordDrawn && Time.time >= lastAttackTime + attackCooldown && !attackInProgress;
    }

    bool CanCombo()
    {
        return swordDrawn && attackInProgress && 
               currentCombo < attackDamage.Length - 1 && 
               animationTimer >= hitFrames[currentCombo] && 
               Time.time < lastAttackTime + comboWindow;
    }

    void StartAttack()
    {
        InitiateAttack(0);
    }

    void ContinueCombo()
    {
        InitiateAttack(currentCombo + 1);
    }

    void InitiateAttack(int comboStep)
    {
        attackInProgress = true;
        hitDetected = false;
        resetTriggered = false;
        animationTimer = 0f;
        currentCombo = comboStep;
        lastAttackTime = Time.time;
        
        animator.ResetTrigger("attackReset");
        animator.SetInteger("comboStep", currentCombo);
        animator.SetTrigger("attackTrigger");
    }

    void UpdateAttack()
    {
        // Hit detection
        if (!hitDetected && animationTimer >= hitFrames[currentCombo])
        {
            PerformHitDetection();
            hitDetected = true;
        }
        
        // Automatic reset after attack completes
        if (!resetTriggered && animationTimer >= resetTimes[currentCombo])
        {
            resetTriggered = true;
            if (currentCombo == 1) // Only force reset after second attack
            {
                ResetToIdle();
            }
        }
    }

    void PerformHitDetection()
    {
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayers);
        
        foreach (Collider enemy in hitEnemies)
        {
            Enemy enemyHealth = enemy.GetComponent<Enemy>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(attackDamage[currentCombo]);
                SpawnBloodParticles(enemy.ClosestPoint(attackPoint.position));
            }
        }
    }
    
    void SpawnBloodParticles(Vector3 position)
    {
        if (bloodParticles != null)
        {
            ParticleSystem blood = Instantiate(bloodParticles, position, Quaternion.identity);
            Destroy(blood.gameObject, blood.main.duration);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}