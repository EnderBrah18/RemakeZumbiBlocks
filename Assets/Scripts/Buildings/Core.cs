using UnityEngine;

public class Core : MonoBehaviour, IDamageable
{
    public int Health = 100;
    public Transform attackPoint;

    public Transform GetAttackPoint()
    {
        return attackPoint != null ? attackPoint : transform;
    }

    public void Damage(float amount)
    {
        Health -= 10;


        if (Health <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}
