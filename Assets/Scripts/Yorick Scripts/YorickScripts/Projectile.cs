using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float speed = 10f;
    [SerializeField] float hitDistance = 0.1f;

    Transform targetTransform;
    IDamageable targetDamageable;
    Collider2D targetCollider;
    float damage;
    Vector3 lastKnownPosition;
    
    public void Launch(Transform target, float damage)
    {
        targetTransform = target;
        targetDamageable = target.GetComponentInParent<IDamageable>();
        targetCollider = target.GetComponentInChildren<Collider2D>();
        this.damage = damage;
        lastKnownPosition = target.position;
    }
    
    public void Launch(UnitHealth target, float damage) => Launch(target.TargetTransform, damage);

    void Update()
    {
        bool targetAlive = targetTransform != null;
        if (targetAlive) lastKnownPosition = targetTransform.position;

        Vector3 to = lastKnownPosition - transform.position;
        to.z = 0f;

        float distance = targetAlive && targetCollider != null
            ? Vector2.Distance(transform.position, targetCollider.ClosestPoint(transform.position))
            : to.magnitude;

        if (distance <= hitDistance)
        {
            if (targetAlive && targetDamageable != null) targetDamageable.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        float angle = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        transform.position += to.normalized * (speed * Time.deltaTime);
    }
}