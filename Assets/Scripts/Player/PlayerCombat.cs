using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    public Transform weaponHolder;
    public Transform head;

    public WeaponSO startingWeapon;
    public Weapon currentWeapon;

    private PlayerInputActions inputActions;

    private bool isFiring;

    void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    

    void OnEnable()
    {
        inputActions.Enable();

        inputActions.Player.Fire.performed += _ => StartFiring();
        inputActions.Player.Fire.canceled += _ => StopFiring();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Start()
    {
        if (startingWeapon != null)
        {
            EquipWeapon(startingWeapon);
        }
    }

    void Update()
    {
        if (currentWeapon == null) return;

        // Arma automática
        if (isFiring && currentWeapon.weaponData.fireType == FireType.Auto)
        {
            currentWeapon.TryShoot();
        }
    }

    public void EquipWeapon(WeaponSO weaponData)
    {
        // remove arma antiga
        if (currentWeapon != null)
            Destroy(currentWeapon.gameObject);

        // cria nova arma
        GameObject weaponGO = Instantiate(weaponData.weaponPrefab, weaponHolder);
        weaponGO.transform.localPosition = Vector3.zero;
        weaponGO.transform.localRotation = Quaternion.identity;

        // pega o script
        currentWeapon = weaponGO.GetComponent<Weapon>();

        // passa os dados do SO pra arma
        currentWeapon.weaponData = weaponData;

        // passa referência da câmera/head
        currentWeapon.head = head;
    }

    void StartFiring()
    {
        Debug.Log("INPUT DE TIRO");

        if (currentWeapon.weaponData.fireType == FireType.Semi)
        {
            currentWeapon.TryShoot();
        }
        else
        {
            isFiring = true;
        }
    }

    void StopFiring()
    {
        isFiring = false;
    }
}