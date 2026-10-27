using UnityEngine;

public class BurnStatus : MonoBehaviour
{
    IDamageable damageable;
    float dps, timeLeft;

    public static void Apply(Transform target, float dps, float duration)
    {
        BurnStatus burn = target.GetComponent<BurnStatus>();
        if (burn == null) burn = target.gameObject.AddComponent<BurnStatus>();
        burn.Ignite(dps, duration);
    }

    void Awake() => damageable = GetComponent<IDamageable>();

    void Ignite(float newDps, float duration)
    {
        dps = timeLeft > 0f ? Mathf.Max(dps, newDps) : newDps;
        timeLeft = Mathf.Max(timeLeft, duration);
    }

    void Update()
    {
        if (timeLeft <= 0f || damageable == null) return;

        timeLeft -= Time.deltaTime;
        damageable.TakeDamage(dps * Time.deltaTime);
    }
}