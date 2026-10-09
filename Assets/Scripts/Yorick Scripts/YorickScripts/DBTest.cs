using UnityEngine;

public class DBTest : MonoBehaviour, IDamageable
{
    [SerializeField] float health = 100f;

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0f) Destroy(gameObject);
    }
}
