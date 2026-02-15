using DG.Tweening;
using UnityEngine;
using UnityEngine.LowLevel;

public class Weapon : MonoBehaviour
{
    public WeaponSO weaponData;

    public Transform firePoint;
    public Transform head;

    [HideInInspector] public PlayerLook playerLook;

    float nextFireTime;

    [Header("Recoil Visual")]
    public Transform recoilPivot;


    Vector3 currentPos;
    Vector3 currentRot;

    Vector3 targetPos;
    Vector3 targetRot;

    [Header("Recoil Settings")]
    public float snappiness = 12f;
    public float returnSpeed = 6f;

    void Awake()
    {
        firePoint = transform.Find("FirePoint");
    }


    public void TryShoot()
    {
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + 1f / weaponData.fireRate;
        Shoot();
    }

    void Shoot()
    {
        ApplyRecoil();
        if (weaponData.weaponType == WeaponType.Hitscan) HitscanShot();
        else ProjectileShot();
    }

    public void ApplyRecoil()
    {
        // 1. Recoil da Câmera no PlayerLook (que move a cabeça do Cinemachine)
        playerLook.AddRecoil(weaponData.recoil.cameraKick);

        // 2. Recoil Visual da Arma usando DOTween
        recoilPivot.DOKill(); // Reseta animações anteriores se atirar rápido

        // Puxão para trás (Efeito de coice)
        recoilPivot.DOLocalMoveZ(-weaponData.recoil.weaponBack, 0.05f).SetLoops(2, LoopType.Yoyo);

        // Rotação para cima (Cano levantando)
        recoilPivot.DOLocalRotate(new Vector3(-weaponData.recoil.weaponUp, 0, 0), 0.07f).SetLoops(2, LoopType.Yoyo);
    }

    void HitscanShot()
    {
        Ray ray = new Ray(head.position, head.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, weaponData.range))
        {
            float finalDamage = CalculateDamage(hit);
            ApplyDamage(hit.collider, finalDamage);
        }
    }

    float CalculateDamage(RaycastHit hit)
    {
        float damage = weaponData.damage;

        float distancePercent = hit.distance / weaponData.maxDistance;
        float distanceMultiplier = weaponData.damageOverDistance.Evaluate(distancePercent);
        damage *= distanceMultiplier;

        if (hit.collider.CompareTag("Head"))
            damage *= weaponData.headshotMultiplier;

        return damage;
    }

    void ApplyDamage(Collider target, float damage)
    {
        IDamageable dmg = target.GetComponentInParent<IDamageable>();

        if (dmg != null)
            dmg.Damage(damage);
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
