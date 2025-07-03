using UnityEngine;
using System.Collections;

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

    [Header("Animation & Effects")]
    public Animator animator;
    public string healAnimationTrigger = "Drink";
    public ParticleSystem healParticles;
    public AudioClip healSound;
    public AudioClip healEmptySound;
    public GameObject potionModel;

    private bool isHealing = false;
    private float lastHealTime;
    private AudioSource audioSource;

    // Event for health changes
    public delegate void HealthChanged(int currentHealth);
    public static event HealthChanged OnHealthChanged;

    void Start()
    {
        currentHealth = maxHealth;
        currentHealingCharges = maxHealingCharges;
        audioSource = GetComponent<AudioSource>();
        if (potionModel != null) potionModel.SetActive(false);
        
        // Initialize health bar
        OnHealthChanged?.Invoke(currentHealth);
    }

    void Update()
    {
        UpdateInvincibility();
        
        if (Input.GetKeyDown(healKey))
        {
            if (CanHeal())
            {
                StartCoroutine(Heal());
            }
            else if (currentHealingCharges <= 0 && healEmptySound != null)
            {
                audioSource.PlayOneShot(healEmptySound);
            }
        }
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
        isHealing = true;
        lastHealTime = Time.time;
        currentHealingCharges--;

        if (potionModel != null) potionModel.SetActive(true);
        if (animator != null) animator.SetTrigger(healAnimationTrigger);
        if (healSound != null) audioSource.PlayOneShot(healSound);
        if (healParticles != null) healParticles.Play();

        float healStartTime = Time.time;
        int targetHealth = Mathf.Min(currentHealth + healingPerCharge, maxHealth);
        int healthBefore = currentHealth;

        while (Time.time - healStartTime < healingDuration)
        {
            float progress = (Time.time - healStartTime) / healingDuration;
            currentHealth = healthBefore + Mathf.RoundToInt(progress * (targetHealth - healthBefore));
            OnHealthChanged?.Invoke(currentHealth); // Update health bar during healing
            yield return null;
        }

        currentHealth = targetHealth; // Ensure we reach exact target
        OnHealthChanged?.Invoke(currentHealth); // Final update
        
        if (potionModel != null) potionModel.SetActive(false);
        isHealing = false;
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible || currentHealth <= 0 || isHealing) return;

        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth); // Update health bar on damage
        
        isInvincible = true;
        invincibilityTimer = invincibilityTime;

        if (isHealing) StopHealing();
        if (currentHealth <= 0) Die();
    }

    public void SetTemporaryInvincibility(float duration)
    {
        isInvincible = true;
        invincibilityTimer = duration;
    }

    void StopHealing()
    {
        StopAllCoroutines();
        if (healParticles != null) healParticles.Stop();
        if (potionModel != null) potionModel.SetActive(false);
        isHealing = false;
    }

    public void RefillHealingCharges()
    {
        currentHealingCharges = maxHealingCharges;
    }

    void Die()
    {
        Debug.Log("Player Died!");
        // Add death handling here
    }

    // Getters
    public int GetCurrentHealth() => currentHealth;
    public int GetCurrentCharges() => currentHealingCharges;
    public bool IsHealing() => isHealing;
    public bool IsInvincible() => isInvincible;
}