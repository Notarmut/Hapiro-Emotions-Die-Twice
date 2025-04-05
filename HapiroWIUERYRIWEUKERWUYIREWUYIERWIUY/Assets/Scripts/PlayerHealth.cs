using UnityEngine;
using System.Collections;
using StarterAssets;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    public float invincibilityTime = 1f;
    public bool isInvincible = false;
    private float invincibilityTimer;

    [Header("Healing Settings")]
    public int maxHealingCharges = 3;
    public int currentHealingCharges;
    public int healingPerCharge = 30;
    public float healingDuration = 2f;
    public float healCooldown = 1f;
    [Range(0.1f, 1f)] public float healingMoveSpeedMultiplier = 0.5f;
    public KeyCode healKey = KeyCode.R;
    public string gamepadHealButton = "X"; // "X" for PlayStation, "A" for Xbox

    [Header("Animation & Effects")]
    public Animator animator;
    public string healAnimationTrigger = "Drink";
    public ParticleSystem healParticles;
    public AudioClip healSound;
    public AudioClip healEmptySound;
    public GameObject potionModel;

    [Header("References")]
    [SerializeField] private ThirdPersonController movementController;
    public PlayerCombat combatController;

    // Private variables
    private bool isHealing = false;
    private float lastHealTime;
    private float originalMoveSpeed;
    private float originalSprintSpeed;
    private AudioSource audioSource;
    private bool originalDashEnabled;

    void Start()
    {
        InitializeHealth();
        CacheReferences();
    }

    void InitializeHealth()
    {
        currentHealth = maxHealth;
        currentHealingCharges = maxHealingCharges;
    }

    void CacheReferences()
    {
        audioSource = GetComponent<AudioSource>();

        if (movementController != null)
        {
            originalMoveSpeed = movementController.MoveSpeed;
            originalSprintSpeed = movementController.SprintSpeed;
            originalDashEnabled = movementController.enableDash;
        }

        if (potionModel != null)
        {
            potionModel.SetActive(false);
        }
    }

    void Update()
    {
        UpdateInvincibility();
        HandleHealInput();
    }

    void UpdateInvincibility()
    {
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0)
            {
                isInvincible = false;
            }
        }
    }

    void HandleHealInput()
    {
        bool healPressed = Input.GetKeyDown(healKey) || Input.GetButtonDown(gamepadHealButton);

        if (healPressed && CanHeal())
        {
            StartCoroutine(Heal());
        }
        else if (healPressed && currentHealingCharges <= 0 && healEmptySound != null)
        {
            audioSource.PlayOneShot(healEmptySound);
        }
    }

    bool CanHeal()
    {
        return currentHealingCharges > 0 &&
               !isHealing &&
               Time.time - lastHealTime > healCooldown &&
               currentHealth < maxHealth &&
               !isInvincible;
    }

    IEnumerator Heal()
    {
        SetupHealingState();

        // Healing process
        float healStartTime = Time.time;
        int targetHealth = Mathf.Min(currentHealth + healingPerCharge, maxHealth);
        int healthBefore = currentHealth;

        while (Time.time - healStartTime < healingDuration)
        {
            if (!isHealing) break; // Check if interrupted

            float progress = (Time.time - healStartTime) / healingDuration;
            currentHealth = healthBefore + Mathf.RoundToInt(progress * (targetHealth - healthBefore));
            yield return null;
        }

        CleanUpHealing();
    }

    void SetupHealingState()
    {
        isHealing = true;
        lastHealTime = Time.time;
        currentHealingCharges--;

        // Visual/Audio feedback
        if (potionModel != null) potionModel.SetActive(true);
        if (animator != null) animator.SetTrigger(healAnimationTrigger);
        if (healSound != null) audioSource.PlayOneShot(healSound);
        if (healParticles != null) healParticles.Play();

        // Movement restrictions
        if (movementController != null)
        {
            movementController.MoveSpeed = originalMoveSpeed * healingMoveSpeedMultiplier;
            movementController.SprintSpeed = originalMoveSpeed * healingMoveSpeedMultiplier;
            movementController.enableDash = false;
        }

        if (combatController != null) combatController.enabled = false;
    }

    void CleanUpHealing()
    {
        if (potionModel != null) potionModel.SetActive(false);

        // Restore movement
        if (movementController != null)
        {
            movementController.MoveSpeed = originalMoveSpeed;
            movementController.SprintSpeed = originalSprintSpeed;
            movementController.enableDash = originalDashEnabled;
        }

        if (combatController != null) combatController.enabled = true;

        isHealing = false;
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible || currentHealth <= 0 || isHealing) return;

        currentHealth -= damage;
        isInvincible = true;
        invincibilityTimer = invincibilityTime;

        if (isHealing) StopHealing();
        if (currentHealth <= 0) Die();
    }

    void StopHealing()
    {
        StopAllCoroutines();

        if (healParticles != null) healParticles.Stop();
        CleanUpHealing();
    }

    public void RefillHealingCharges()
    {
        currentHealingCharges = maxHealingCharges;
    }

    void Die()
    {
        Debug.Log("Player Died!");
        // Add death handling (respawn, game over, etc.)
    }

    // Public accessors
    public int GetCurrentHealth() => currentHealth;
    public int GetCurrentCharges() => currentHealingCharges;
    public bool IsHealing() => isHealing;
}