using System.Collections.Generic;
using UnityEngine;

public class CannonTower : DefenseBuilding
{
    [Header("Splash")]
    [SerializeField] float splashRadius = 2f;
    [SerializeField, Range(0f, 1f)] float edgeDamage = 0.5f;
 
    
    protected override void Fire(UnitHealth enemy)
    {
        if (ProjectilePrefab == null)
        {
            if (splashRadius > 0f)
            {
                Projectile.ApplySplashDamage(
                    enemy.transform.position,
                    Damage,
                    splashRadius,
                    edgeDamage
                );
            }
            else
            {
                enemy.TakeDamage(Damage);
            }

            return;
        }

        Projectile shell = Instantiate(
            ProjectilePrefab,
            FirePosition,
            Quaternion.identity
        );

        shell.Launch(enemy, Damage, splashRadius, edgeDamage);
    }
}
