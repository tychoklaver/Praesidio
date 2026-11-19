using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float speed = 10f;
    [SerializeField] float hitDistance = 0.1f;

    [SerializeField] float splashRadius;
    [SerializeField, Range(0f, 1f)] float edgeDamage = 0.5f;

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

    public void Launch(UnitHealth target, float damage, float radius, float edgeDamage)
    {
        Launch(target.TargetTransform, damage);

        this.splashRadius = radius;
        this.edgeDamage = edgeDamage;
    }

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
            Vector3 impactCenter = targetAlive
                ? targetTransform.position
                : lastKnownPosition;

            if (splashRadius > 0f)
            {
                ApplySplashDamage(
                    impactCenter,
                    damage,
                    splashRadius,
                    edgeDamage
                );
            }
            else if (targetAlive && targetDamageable != null)
            {
                targetDamageable.TakeDamage(damage);
            }

            Destroy(gameObject);
            return;
        }

        float angle = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        transform.position += to.normalized * (speed * Time.deltaTime);
    }

    public static void ApplySplashDamage(
        Vector3 center,
        float damage,
        float radius,
        float edgeDamage)
    {
        if (radius <= 0f) return;

        edgeDamage = Mathf.Clamp01(edgeDamage);

        foreach (UnitHealth unit in UnitHealth.all)
        {
            if (unit == null || !unit.IsAlive) continue;

            float distance = Vector2.Distance(
                center,
                unit.transform.position
            );

            if (distance > radius) continue;

            // 0 = midden van de explosie
            // 1 = rand van de explosie
            float t = distance / radius;

            // Bereken de schade op basis van de afstand
            float damageMultiplier = Mathf.Lerp(1f, edgeDamage, t);

            unit.TakeDamage(damage * damageMultiplier);
        }
    }
}