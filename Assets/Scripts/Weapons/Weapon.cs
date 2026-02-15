using UnityEngine;

public class Weapon : MonoBehaviour
{
    public WeaponSO weaponData;

    public Transform firePoint;
    public Transform head;

    private float nextFireTime;

    void Awake()
    {
        firePoint = transform.Find("FirePoint");
    }

    public void TryShoot()
    {
        Debug.Log("TRY SHOOT");

        if (Time.time < nextFireTime)
            return;

        nextFireTime = Time.time + 1f / weaponData.fireRate;

        Shoot();
    }

    void Shoot()
    {
        Debug.Log("ATIROU com dano: " + weaponData.damage);

        if (weaponData.weaponType == WeaponType.Hitscan)
            HitscanShot();
        else
            ProjectileShot();
    }

    void HitscanShot()
    {
        PlayMuzzleFlash();

        Ray ray = new Ray(head.position, head.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, weaponData.range))
        {
            Damage(hit.collider);
            PlayHitEffect(hit);
        }
    }

    void Damage(Collider target)
    {
        Enemy enemy = target.GetComponentInParent<Enemy>();

        if (enemy != null)
        {
            enemy.Damage(weaponData.damage);
        }
    }

    void PlayHitEffect(RaycastHit hit)
    {
        if (weaponData.hitEffect == null) return;

        GameObject fx = Instantiate(
            weaponData.hitEffect,
            hit.point + hit.normal * 0.001f,
            Quaternion.LookRotation(hit.normal)
        );

        Destroy(fx, 1f);
    }

    void PlayMuzzleFlash()
    {
        if (weaponData.muzzleFlash == null || firePoint == null) return;

        GameObject fx = Instantiate(
            weaponData.muzzleFlash,
            firePoint.position,
            firePoint.rotation,
            firePoint
        );

        Destroy(fx, 0.05f);
    }

    void ProjectileShot()
    {
        GameObject proj = Instantiate(
            weaponData.projectilePrefab,
            firePoint.position,
            firePoint.rotation
        );

        Rigidbody rb = proj.GetComponent<Rigidbody>();
        rb.linearVelocity = firePoint.forward * weaponData.projectileSpeed;
    }

}
