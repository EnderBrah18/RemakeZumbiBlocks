using UnityEngine;


public enum FireType { Semi, Auto }
public enum WeaponType { Hitscan, Projectile }

public enum AmmoType { Pistol, Rifle, Shotgun, Sniper }

[CreateAssetMenu(menuName = "FPS/Weapon")]
public class WeaponSO : ScriptableObject
{
    public RecoilData recoil;

    [Header("Prefab da arma")]
    public GameObject weaponPrefab;
    public GameObject worldModelPrefab;

    [Header("Info")]
    public string weaponName;

    [Header("Recoil")]
    public float recoilAmount = 2f;

    [Header("Visual Recoil")]
    public float recoilBack = 0.1f;
    public float recoilUp = 5f;
    public float recoilReturnSpeed = 8f;
    public float recoilSnappiness = 12f;

    [Header("Combate")]
    public float damage = 10f;
    public float fireRate = 10f;
    public float range = 100f;

    [Header("Munição")]
    public int magSize = 30;       // Capacidade do pente
    public float reloadTime = 1.5f; // Quanto tempo demora a recarga

    [Header("Headshot")]
    public float headshotMultiplier = 2f;

    [Header("Distance Damage")]
    public AnimationCurve damageOverDistance;
    public float maxDistance = 100f;

    [Header("Tipo")]
    public FireType fireType;
    public WeaponType weaponType;

    [Header("Impacto")]
    public GameObject hitEffect;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 30f;

    [Header("Visual")]
    public GameObject muzzleFlash;

    public AmmoType ammoType;
}
