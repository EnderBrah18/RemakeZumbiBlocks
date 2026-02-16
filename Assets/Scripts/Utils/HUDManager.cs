using UnityEngine;
using TMPro;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance; // Singleton para fácil acesso

    [Header("UI Elements")]
    public TextMeshProUGUI currentAmmoText;
    public TextMeshProUGUI stockAmmoText;
    public TextMeshProUGUI weaponNameText;

    void Awake()
    {
        Instance = this;
    }

    public void UpdateAmmoUI(int current, int stock)
    {
        currentAmmoText.text = current.ToString();
        stockAmmoText.text = "/ " + stock.ToString();

        // Efeito visual opcional: Se tiver pouca munição, fica vermelho
        currentAmmoText.color = (current <= 5) ? Color.red : Color.white;
    }

    public void UpdateWeaponName(string name)
    {
        if (weaponNameText != null) weaponNameText.text = name;
    }
}
