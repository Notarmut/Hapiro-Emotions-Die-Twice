using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Health Bar Settings")]
    public Slider healthSlider;
    public Image healthFill;
    public Color healthyColor = Color.red;
    public Color damagedColor = Color.white; // Color when taking damage
    public float colorLerpSpeed = 5f;
    
    private PlayerHealth playerHealth;
    private Color targetColor;
    private float damageFlashTimer;

    void Start()
    {
        playerHealth = FindObjectOfType<PlayerHealth>();
        
        if (healthSlider == null)
        {
            Debug.LogError("Health Slider not assigned!");
            return;
        }
        
        if (playerHealth != null)
        {
            healthSlider.maxValue = playerHealth.maxHealth;
            healthSlider.value = playerHealth.GetCurrentHealth();
        }
        
        // Set initial color
        if (healthFill != null)
        {
            healthFill.color = healthyColor;
            targetColor = healthyColor;
        }
    }

    void Update()
    {
        if (playerHealth == null) return;
        
        // Smoothly update health value
        healthSlider.value = Mathf.Lerp(healthSlider.value, playerHealth.GetCurrentHealth(), 10f * Time.deltaTime);
        
        // Handle damage flash effect
        if (damageFlashTimer > 0)
        {
            damageFlashTimer -= Time.deltaTime;
            if (damageFlashTimer <= 0)
            {
                targetColor = healthyColor;
            }
        }
        
        // Update health bar color
        if (healthFill != null)
        {
            healthFill.color = Color.Lerp(healthFill.color, targetColor, colorLerpSpeed * Time.deltaTime);
        }
    }

    // Call this when player takes damage
    public void OnPlayerDamaged()
    {
        if (healthFill != null)
        {
            targetColor = damagedColor;
            damageFlashTimer = 0.3f; // Flash duration
        }
    }
}