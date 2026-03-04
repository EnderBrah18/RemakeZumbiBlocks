using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    public enum AmmoType { Rifle, Pistol }
    public AmmoType type;
    public int amount = 30;

    // Método chamado pelo Raycast (Interação manual)
    public void GiveAmmo(PlayerCombat player)
    {
        Collect(player);
    }

    // Método chamado pelo Trigger (Passar por cima)
    private void OnTriggerEnter(Collider other)
    {
        // Verifica se o objeto que entrou no trigger tem a tag Player
        if (other.CompareTag("Player"))
        {
            PlayerCombat player = other.GetComponent<PlayerCombat>();
            if (player != null)
            {
                Collect(player);
            }
        }
    }

    // Lógica centralizada de coleta
    private void Collect(PlayerCombat player)
    {
        // Adiciona munição ao stock correto
        if (type == AmmoType.Rifle) player.rifleAmmo += amount;
        else if (type == AmmoType.Pistol) player.pistolAmmo += amount;

        // Atualiza o HUD do player
        player.RefreshHUD();

        // Destrói a caixa
        Destroy(gameObject);
    }
}
