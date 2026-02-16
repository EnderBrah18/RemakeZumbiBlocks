using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    public enum AmmoType { Rifle, Pistol }
    public AmmoType type;
    public int amount = 30;

    // Podes usar o mesmo sistema de Raycast que usaste para as armas
    public void GiveAmmo(PlayerCombat player)
    {
        if (type == AmmoType.Rifle) player.rifleAmmo += amount;
        else player.pistolAmmo += amount;

        Destroy(gameObject);
    }
}
