using UnityEngine;


public enum FireType { Semi, Auto }
public enum WeaponType { Hitscan, Projectile }

[CreateAssetMenu(menuName = "FPS/Weapon")]
public class WeaponSO : ScriptableObject
{
    [Header("Prefab da arma")]
    public GameObject weaponPrefab;

    [Header("Info")]
    public string weaponName;


    [Header("Combate")]
    public float damage = 10f;
    public float fireRate = 10f;
    public float range = 100f;

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
}
