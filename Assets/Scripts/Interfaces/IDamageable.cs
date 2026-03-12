using UnityEngine;

public interface IDamageable
{
    void Damage(float damage);
    Transform GetAttackPoint();
}
