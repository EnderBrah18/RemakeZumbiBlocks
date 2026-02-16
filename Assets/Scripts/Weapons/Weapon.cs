using DG.Tweening;
using UnityEngine;
using UnityEngine.LowLevel;

public class Weapon : MonoBehaviour
{
    public WeaponSO weaponData;


    public Transform firePoint;
    public Transform head;

    [HideInInspector] public PlayerCombat playerCombat;
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

    [Header("Ammo Status")]
    [SerializeField] private int currentAmmo;
    private bool isReloading = false;
    private bool ammoInitialized = false;

    void Awake()
    {
        firePoint = transform.Find("FirePoint");
    }

    void Start()
    {
        // Só define munição cheia se NINGUÉM (como o PlayerCombat) definiu antes
        if (!ammoInitialized)
        {
            currentAmmo = weaponData.magSize;
            ammoInitialized = true;
        }
    }

    public int GetCurrentAmmo() => currentAmmo;

    public void SetAmmo(int amount)
    {
        currentAmmo = amount;
        ammoInitialized = true;
    }

    public void TryShoot()
    {
        // Impede de atirar se estiver recarregando ou sem munição
        if (isReloading || currentAmmo <= 0) return;

        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + 1f / weaponData.fireRate;

        currentAmmo--;

        Shoot();

        Debug.Log("Munição Atual: " + currentAmmo);
    }

    public void TryReload()
    {
        // Só recarrega se não estiver recarregando e se o pente não estiver cheio
        if (isReloading || currentAmmo == weaponData.magSize) return;

        StartCoroutine(ReloadCoroutine());
    }

    private System.Collections.IEnumerator ReloadCoroutine()
    {
        int ammoNeeded = weaponData.magSize - currentAmmo;
        if (ammoNeeded <= 0) yield break;

        // Usa a referência que passamos no EquipWeapon
        if (playerCombat == null) playerCombat = GetComponentInParent<PlayerCombat>();
        if (playerCombat == null) yield break;

        isReloading = true;

        // Animação...
        recoilPivot.DOLocalRotate(new Vector3(15, -10, 5), 0.3f);
        recoilPivot.DOLocalMoveY(-0.05f, 0.3f);

        yield return new WaitForSeconds(weaponData.reloadTime);

        // Tira do stock
        int ammoToLoad = playerCombat.GetAmmoFromStock(weaponData.ammoType, ammoNeeded);
        currentAmmo += ammoToLoad;

        // Reset Visual...
        recoilPivot.DOLocalRotate(Vector3.zero, 0.2f);
        recoilPivot.DOLocalMoveY(0, 0.2f);

        isReloading = false;
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

        Debug.DrawRay(head.position, head.forward * weaponData.range, Color.red, 1f);

        if (Physics.Raycast(ray, out RaycastHit hit, weaponData.range))
        {
            Debug.Log("Atingiu: " + hit.collider.name);

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
