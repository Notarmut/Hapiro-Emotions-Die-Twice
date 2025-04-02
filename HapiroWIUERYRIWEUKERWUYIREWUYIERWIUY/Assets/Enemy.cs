using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;
    public ParticleSystem deathParticles;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public bool TakeDamage(int damage)
    {
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
        // Play death animation or effects
        if (deathParticles != null)
        {
            ParticleSystem deathFX = Instantiate(deathParticles, transform.position, Quaternion.identity);
            deathFX.Play();
            Destroy(deathFX.gameObject, deathFX.main.duration);
        }
        
        // Disable enemy
        Destroy(gameObject);
        // Or use: gameObject.SetActive(false); if you want to pool objects
    }
}