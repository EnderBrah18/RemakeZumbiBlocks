using DG.Tweening;
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

    [Header("Interação")]
    public float interactRange = 3f;
    public LayerMask interactableLayer;
    public GameObject worldItemPrefab;

    [Header("Animação de Troca")]
    public float equipTime = 0.5f;
    private bool isSwapping = false;

    [Header("Inventário")]
    public WeaponSO[] inventory = new WeaponSO[2]; // Slot 0 e Slot 1
    public int[] ammoInSlots = new int[2];
    public int currentSlot = 0;

    private bool isFiring;

    [Header("Stock de Munição")]
    public int pistolAmmo = 60;
    public int rifleAmmo = 120;
    public int shotgunAmmo = 20;

    void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    

    void OnEnable()
    {
        inputActions.Enable();

        inputActions.Player.Fire.performed += _ => StartFiring();
        inputActions.Player.Fire.canceled += _ => StopFiring();

        inputActions.Player.Reload.performed += _ => {
            if (currentWeapon != null) currentWeapon.TryReload();
        };

        inputActions.Player.Interact.performed += _ => TryPickupItem();
        inputActions.Player.Drop.performed += _ => DropCurrentWeapon();

        // Adicionar inputs para trocar de arma (Teclas 1 e 2)
        inputActions.Player.Slot1.performed += _ => SwitchWeapon(0);
        inputActions.Player.Slot2.performed += _ => SwitchWeapon(1);
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Start()
    {
        // Inicializa a munição dos slots que já começam com arma
        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i] != null && ammoInSlots[i] == 0)
            {
                ammoInSlots[i] = inventory[i].magSize;
            }
        }

        if (inventory[currentSlot] != null)
            EquipWeapon(inventory[currentSlot], ammoInSlots[currentSlot]);
    }

    void Update()
    {


        if (isSwapping || currentWeapon == null) return;

        // Lógica de Scroll (Mantida)
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll > 0) SwitchWeapon(0);
        else if (scroll < 0) SwitchWeapon(1);

        // Tiro automático
        if (isFiring && currentWeapon.weaponData.fireType == FireType.Auto)
        {
            currentWeapon.TryShoot();
        }
    }

    void SwitchWeapon(int slotIndex)
    {
        if (inventory[slotIndex] == null)
        {
            Debug.Log("Slot vazio! Não há nada para equipar.");
            return;
        }

        // Só troca se não estiver já trocando, se o slot for diferente e se houver arma
        if (isSwapping || slotIndex == currentSlot || inventory[slotIndex] == null) return;

        StartCoroutine(SwapRoutine(slotIndex));
    }

    private System.Collections.IEnumerator SwapRoutine(int slotIndex)
    {
        isSwapping = true;
        isFiring = false;

        if (currentWeapon != null)
        {
            // SALVA a munição da arma atual no slot antes de a destruir!
            ammoInSlots[currentSlot] = currentWeapon.GetCurrentAmmo();

            weaponHolder.DOLocalMoveY(-1f, equipTime / 2).SetEase(Ease.InBack);
            yield return new WaitForSeconds(equipTime / 2);
        }

        currentSlot = slotIndex;

        // EQUIPA passando a munição que estava guardada no novo slot
        EquipWeapon(inventory[currentSlot], ammoInSlots[currentSlot]);
        RefreshHUD();

        weaponHolder.localPosition = new Vector3(0, -1f, 0);
        weaponHolder.DOLocalMoveY(0, equipTime / 2).SetEase(Ease.OutBack);

        yield return new WaitForSeconds(equipTime / 2);
        isSwapping = false;
    }

    public void EquipWeapon(WeaponSO weaponData, int savedAmmo = -1)
    {
        if (currentWeapon != null) Destroy(currentWeapon.gameObject);

        GameObject weaponGO = Instantiate(weaponData.weaponPrefab, weaponHolder);
        SetLayerRecursive(weaponGO, LayerMask.NameToLayer("Weapon"));

        weaponGO.transform.localPosition = Vector3.zero;
        weaponGO.transform.localRotation = Quaternion.identity;

        currentWeapon = weaponGO.GetComponent<Weapon>();

        // PASSA A REFERÊNCIA DO PLAYER PARA A ARMA
        currentWeapon.playerCombat = this;

        currentWeapon.weaponData = weaponData;
        currentWeapon.head = head;
        currentWeapon.playerLook = GetComponent<PlayerLook>();

        // Define a munição
        if (savedAmmo != -1)
            currentWeapon.SetAmmo(savedAmmo);
        else
            currentWeapon.SetAmmo(weaponData.magSize);
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

        if (isSwapping || currentWeapon == null) return;

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

    void TryPickupItem()
    {
        Ray ray = new Ray(head.position, head.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
        {
            // --- CASO 1: É UMA ARMA ---
            if (hit.collider.TryGetComponent(out ItemPickup weaponPickup))
            {
                int targetSlot = -1;

                // 1. Procura um slot vazio primeiro
                for (int i = 0; i < inventory.Length; i++)
                {
                    if (inventory[i] == null)
                    {
                        targetSlot = i;
                        break;
                    }
                }

                // 2. Se não achou slot vazio, substitui a atual
                if (targetSlot == -1)
                {
                    targetSlot = currentSlot;
                    DropCurrentWeapon();
                }

                // 3. Equipar (Usando o nome correto: weaponPickup)
                inventory[targetSlot] = weaponPickup.weaponData;
                ammoInSlots[targetSlot] = weaponPickup.ammoRemaining; // Guarda a munição do chão no slot
                currentSlot = targetSlot;
                EquipWeapon(weaponPickup.weaponData, weaponPickup.ammoRemaining);

                Destroy(hit.collider.gameObject);
            }

            // --- CASO 2: É UMA CAIXA DE MUNIÇÃO ---
            else if (hit.collider.TryGetComponent(out AmmoPickup ammoBox))
            {
                ammoBox.GiveAmmo(this);
                // O Destroy já acontece dentro do método GiveAmmo do script que criamos antes
            }
        }
    }

    void DropCurrentWeapon()
    {
        // 1. Verifica se temos uma arma equipada e se o slot atual não está vazio
        if (currentWeapon == null || inventory[currentSlot] == null) return;

        // 2. Instancia o prefab físico no mundo
        Vector3 spawnOffset = head.forward * 1.5f + head.up * 0.5f;
        Vector3 dropPos = head.position + spawnOffset;

        GameObject droppedItem = Instantiate(inventory[currentSlot].worldModelPrefab, dropPos, head.rotation);

        // Garante que a arma no chão volte para a layer padrão (para ser vista pela Main Camera)
        SetLayerRecursive(droppedItem, LayerMask.NameToLayer("Interactable"));

        // 3. Aplica física
        if (droppedItem.TryGetComponent(out Rigidbody rb))
        {
            rb.AddForce(head.forward * 5f + Vector3.up * 2f, ForceMode.Impulse);
        }

        if (droppedItem.TryGetComponent(out ItemPickup pickup))
        {
            pickup.ammoRemaining = currentWeapon.GetCurrentAmmo();
        }

        // --- A SOLUÇÃO ESTÁ AQUI ---
        // 4. Limpa a referência no array do inventário para que o slot fique vazio
        inventory[currentSlot] = null;

        // 5. Destrói o objeto que estava na mão do player
        Destroy(currentWeapon.gameObject);
        currentWeapon = null;

        // 6. Reseta o estado de tiro
        isFiring = false;

        Debug.Log("Arma dropada. Slot " + currentSlot + " agora está vazio.");
    }

    public int GetAmmoFromStock(AmmoType type, int amountNeeded)
    {
        // Verifique se os nomes batem com o que você criou no Enum
        if (type == AmmoType.Rifle)
        {
            int available = Mathf.Min(amountNeeded, rifleAmmo);
            rifleAmmo -= available;
            return available;
        }
        if (type == AmmoType.Pistol)
        {
            int available = Mathf.Min(amountNeeded, pistolAmmo);
            pistolAmmo -= available;
            return available;
        }
        // Adicione os outros tipos aqui...
        return 0;
    }

    public int GetStockForType(AmmoType type)
    {
        switch (type)
        {
            case AmmoType.Pistol: return pistolAmmo;
            case AmmoType.Rifle: return rifleAmmo;
            case AmmoType.Shotgun: return shotgunAmmo;
            default: return 0;
        }
    }

    // Chame isso sempre que trocar de arma ou pegar munição
    public void RefreshHUD()
    {
        if (currentWeapon == null) return;

        int stock = GetStockForType(currentWeapon.weaponData.ammoType);
        HUDManager.Instance.UpdateAmmoUI(currentWeapon.GetCurrentAmmo(), stock);
        HUDManager.Instance.UpdateWeaponName(currentWeapon.weaponData.weaponName);
    }
}