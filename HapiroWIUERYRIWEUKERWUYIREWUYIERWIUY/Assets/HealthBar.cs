using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider healthSlider;
    public Slider easeHealthSlider;
    public PlayerHealth playerHealth;
    public float lerpSpeed = 0.05f;

    void Start()
    {
        if (playerHealth == null) 
        {
            // Updated to use the new recommended method
            playerHealth = FindAnyObjectByType<PlayerHealth>();
            
            // Alternatively, if you specifically want the first instance:
            // playerHealth = FindFirstObjectByType<PlayerHealth>();
        }
        
        // Initialize sliders
        healthSlider.maxValue = playerHealth.maxHealth;
        easeHealthSlider.maxValue = playerHealth.maxHealth;
        healthSlider.value = playerHealth.GetCurrentHealth();
        easeHealthSlider.value = playerHealth.GetCurrentHealth();
        
        // Subscribe to health change events
        PlayerHealth.OnHealthChanged += UpdateHealthBar;
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        PlayerHealth.OnHealthChanged -= UpdateHealthBar;
    }

    void UpdateHealthBar(int currentHealth)
    {
        healthSlider.value = currentHealth;
    }

    void Update()
    {
        // Smooth the ease health slider
        if (Mathf.Abs(easeHealthSlider.value - healthSlider.value) > 0.01f)
        {
            easeHealthSlider.value = Mathf.Lerp(easeHealthSlider.value, 
                                             healthSlider.value, 
                                             lerpSpeed);
        }
        else
        {
            easeHealthSlider.value = healthSlider.value;
        }
    }
}