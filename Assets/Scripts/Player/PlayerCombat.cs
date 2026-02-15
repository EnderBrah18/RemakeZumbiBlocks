using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    public Transform weaponHolder;
    public Transform head;

    public WeaponSO startingWeapon;
    public Weapon currentWeapon;
    public CameraEffects cameraEffects;

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
        if (currentWeapon != null) Destroy(currentWeapon.gameObject);

        GameObject weaponGO = Instantiate(weaponData.weaponPrefab, weaponHolder);

        // Define a layer para que a WeaponCamera (Overlay) capture o objeto
        SetLayerRecursive(weaponGO, LayerMask.NameToLayer("Weapon"));

        // Reseta a posição para o centro da visão da câmera overlay
        weaponGO.transform.localPosition = Vector3.zero;
        weaponGO.transform.localRotation = Quaternion.identity;

        currentWeapon = weaponGO.GetComponent<Weapon>();
        currentWeapon.weaponData = weaponData;
        currentWeapon.head = head;
        currentWeapon.playerLook = GetComponent<PlayerLook>();
    }

    // Método auxiliar para mudar a layer de tudo na arma
    void SetLayerRecursive(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform) SetLayerRecursive(child.gameObject, newLayer);
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