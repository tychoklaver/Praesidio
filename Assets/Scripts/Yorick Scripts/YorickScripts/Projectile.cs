using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float speed = 8f;
    [SerializeField] float hitDistance = 0.15f;

    Transform target;
    float damage;
    public void Initialize(Transform target, float damage)
    {
        this.target = target;
        this.damage = damage;
    }
    void Update()
    {
        if (target == null) { Destroy(gameObject); return; }

        Vector3 toTarget = target.position - transform.position;
        if (toTarget.magnitude <= hitDistance)
        {
            target.GetComponentInParent<IDamageable>()?.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        transform.position += toTarget.normalized * speed * Time.deltaTime;
    }
}
