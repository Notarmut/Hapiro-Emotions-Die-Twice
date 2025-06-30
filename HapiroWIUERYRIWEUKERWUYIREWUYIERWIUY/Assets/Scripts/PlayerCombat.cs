using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("General")]
    public Animator animator;
    public Transform attackPoint;
    public LayerMask enemyLayers;
    public ParticleSystem bloodEffect;

    [Header("Sword")]
    public float drawSwordTime = 0.5f;
    private bool swordDrawn = false;

    [Header("Attack")]
    public float[] attackTimings = { 0.3f, 0.25f, 0.2f }; // Add timing for the third attack
    public int[] attackDamages = { 20, 30, 40 }; // Add damage for the third attack
    public float comboResetTime = 1f;
    public float attackCooldown = 0.4f;
    public float attackRange = 1.5f;

    private float comboInputTimer = 0f;
    private bool waitingForComboInput = false;

    private int comboIndex = 0;
    private float lastAttackTime = -99f;
    private float attackTimer = 0f;
    private bool hitOccurred = false;

    private enum CombatState { Idle, Drawing, Attacking }
    private CombatState state = CombatState.Idle;

    private ThirdPersonControllerr movementController;

        





    void Start()
    {
        movementController = GetComponent<ThirdPersonControllerr>();
        

    }

    void Update()
    {
        HandleInput();

        if (state == CombatState.Attacking)
            HandleAttackTiming();
        else if (state == CombatState.Drawing && Time.time - lastAttackTime >= drawSwordTime)
            FinishDrawingSword();

        // Track combo input timeout
        if (waitingForComboInput)
        {
            comboInputTimer += Time.deltaTime;
            if (comboInputTimer >= comboResetTime)
            {
                ResetCombat();
            }
        }
    }

    void HandleInput()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            comboInputTimer = 0f;
            waitingForComboInput = true;

            if (!swordDrawn)
            {
                DrawSword();
            }
            else if (state == CombatState.Idle && Time.time - lastAttackTime >= attackCooldown)
            {
                StartAttack(0); // Start the first attack
            }
            else if (state == CombatState.Attacking && CanChainCombo())
            {
                StartAttack(comboIndex + 1); // Continue combo chain
            }
        }
    }

    void DrawSword()
    {
        state = CombatState.Drawing;
        lastAttackTime = Time.time;
        animator.SetTrigger("drawSword");
    }

    void FinishDrawingSword()
    {
        state = CombatState.Idle;
        swordDrawn = true;
        animator.SetBool("swordDrawn", true);
    }

    void StartAttack(int index)
    {
        if (index >= attackDamages.Length) return;

        comboIndex = index;
        lastAttackTime = Time.time;
        attackTimer = 0f;
        hitOccurred = false;

        state = CombatState.Attacking;
        animator.SetInteger("attackIndex", comboIndex);
        animator.SetTrigger("attackTrigger");

        Debug.Log("Starting attack. Applying lunge.");
        if (movementController != null)
        {
            movementController.ApplyAttackLunge();
        }
        else
        {
            Debug.LogWarning("movementController is null!");
        }
    }

    void HandleAttackTiming()
    {
        attackTimer += Time.deltaTime;

        if (!hitOccurred && attackTimer >= attackTimings[comboIndex])
        {
            DetectHit();
            hitOccurred = true;
        }

        if (attackTimer >= comboResetTime)
        {
            ResetCombat();
        }
    }

    bool CanChainCombo()
    {
        return comboIndex + 1 < attackDamages.Length &&
               attackTimer >= attackTimings[comboIndex] &&
               Time.time - lastAttackTime <= comboResetTime;
    }
    
    void DetectHit()
    {
        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider col in hits)
        {
            if (col.TryGetComponent(out Enemy enemy))
            {
                enemy.TakeDamage(attackDamages[comboIndex]);
                SpawnBlood(col.ClosestPoint(attackPoint.position));

               
               
            }

        }
    }

    void SpawnBlood(Vector3 pos)
    {
        if (bloodEffect != null)
        {
            ParticleSystem blood = Instantiate(bloodEffect, pos, Quaternion.identity);
            Destroy(blood.gameObject, blood.main.duration);
        }
    }

    void ResetCombat()
    {
        state = CombatState.Idle;
        comboIndex = 0;
        attackTimer = 0f;
        comboInputTimer = 0f;
        waitingForComboInput = false;
        animator.SetTrigger("attackReset");
        Debug.Log("combat reseted");
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}