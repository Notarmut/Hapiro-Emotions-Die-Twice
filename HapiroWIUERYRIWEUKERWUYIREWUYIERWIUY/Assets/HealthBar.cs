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
        if (playerHealth == null) playerHealth = FindObjectOfType<PlayerHealth>();
        
        healthSlider.maxValue = playerHealth.maxHealth;
        easeHealthSlider.maxValue = playerHealth.maxHealth;
        healthSlider.value = playerHealth.GetCurrentHealth();
        easeHealthSlider.value = playerHealth.GetCurrentHealth();
    }

    void Update()
    {
        if (playerHealth == null) return;

        healthSlider.value = playerHealth.GetCurrentHealth();

        if (Mathf.Abs(easeHealthSlider.value - healthSlider.value) > 0.01f)
        {
            easeHealthSlider.value = Mathf.Lerp(easeHealthSlider.value, 
                                             playerHealth.GetCurrentHealth(), 
                                             lerpSpeed);
        }
        else
        {
            easeHealthSlider.value = playerHealth.GetCurrentHealth();
        }
    }
}