using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public float invincibilityTime = 1f;
    
    [Header("Healing Settings")]
    public int maxHealingCharges = 3;
    public int healingPerCharge = 30;
    public float healingDuration = 2f;
    public float healCooldown = 1f;
    
    [Header("Animation")]
    public Animator animator;
    public string healAnimationName = "HealingPotion";
    public float animationCancelTime = 0.5f;
    
    [Header("Effects")]
    public ParticleSystem healParticles;
    public AudioClip healSound;
    public AudioClip healEmptySound;
    public GameObject potionModel; // Your healing potion model
    
    private int currentHealth;
    private int currentHealingCharges;
    private float invincibilityTimer;
    private bool isInvincible = false;
    private bool isHealing = false;
    private float lastHealTime;
    private AudioSource audioSource;
    private int healAnimationHash;

    void Start()
    {
        currentHealth = maxHealth;
        currentHealingCharges = maxHealingCharges;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        healAnimationHash = Animator.StringToHash(healAnimationName);
        
        if (potionModel != null)
        {
            potionModel.SetActive(false);
        }
    }

    void Update()
    {
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0)
            {
                isInvincible = false;
            }
        }
        
        if (Input.GetKeyDown(KeyCode.R) && CanHeal())
        {
            StartCoroutine(Heal());
        }
    }

    bool CanHeal()
    {
        return currentHealingCharges > 0 && 
               !isHealing && 
               Time.time - lastHealTime > healCooldown &&
               currentHealth < maxHealth;
    }

    IEnumerator Heal()
    {
        if (currentHealingCharges <= 0)
        {
            if (healEmptySound != null)
            {
                audioSource.PlayOneShot(healEmptySound);
            }
            yield break;
        }
        
        isHealing = true;
        lastHealTime = Time.time;
        currentHealingCharges--;
        
        // Show potion and play animation
        if (potionModel != null)
        {
            potionModel.SetActive(true);
        }
        
        // Trigger healing animation
        if (animator != null)
        {
            animator.SetTrigger(healAnimationHash);
            yield return new WaitForSeconds(animationCancelTime); // Wait for animation to start
        }
        
        // Play heal sound
        if (healSound != null)
        {
            audioSource.PlayOneShot(healSound);
        }
        
        // Healing process
        float healStartTime = Time.time;
        int targetHealth = Mathf.Min(currentHealth + healingPerCharge, maxHealth);
        int healthBefore = currentHealth;
        
        if (healParticles != null)
        {
            healParticles.Play();
        }
        
        while (Time.time - healStartTime < healingDuration)
        {
            if (!isHealing) break; // Cancel if interrupted
            
            float progress = (Time.time - healStartTime) / healingDuration;
            currentHealth = healthBefore + Mathf.RoundToInt(progress * (targetHealth - healthBefore));
            yield return null;
        }
        
        // Hide potion after healing
        if (potionModel != null)
        {
            potionModel.SetActive(false);
        }
        
        isHealing = false;
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible || currentHealth <= 0 || isHealing) return;
        
        currentHealth -= damage;
        isInvincible = true;
        invincibilityTimer = invincibilityTime;
        
        // Cancel healing if taking damage
        if (isHealing)
        {
            StopHealing();
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void StopHealing()
    {
        StopAllCoroutines();
        isHealing = false;
        
        if (potionModel != null)
        {
            potionModel.SetActive(false);
        }
        
        if (healParticles != null)
        {
            healParticles.Stop();
        }
    }

    public void RefillHealingCharges()
    {
        currentHealingCharges = maxHealingCharges;
    }

    void Die()
    {
        Debug.Log("Player Died!");
        Destroy(gameObject);
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetCurrentCharges() => currentHealingCharges;
}