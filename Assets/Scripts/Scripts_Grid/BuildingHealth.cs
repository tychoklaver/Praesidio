using System;
using UnityEngine;

public class BuildingHealth : MonoBehaviour, IDamageable
{
    [SerializeField] float maxHealth = 100f;

    float health;

    public float Health => health;
    public float MaxHealth => maxHealth;
    public float Normalized => maxHealth > 0f ? health / maxHealth : 0f;
    public bool IsAlive => this != null && health > 0f;
    
    public event Action<float, float> OnHealthChanged;
    public event Action OnDied;

    void Awake() => health = maxHealth;

    public void TakeDamage(float amount)
    {
        if (!IsAlive) return;

        health = Mathf.Max(0f, health - amount);
        OnHealthChanged?.Invoke(health, maxHealth);

        if (health <= 0f)
        {
            OnDied?.Invoke();
            Destroy(gameObject);
        }
    }

    public void Heal(float amount)
    {
        if (!IsAlive) return;

        health = Mathf.Min(maxHealth, health + amount);
        OnHealthChanged?.Invoke(health, maxHealth);
    }
}