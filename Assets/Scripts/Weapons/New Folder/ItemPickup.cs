using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public WeaponSO weaponData; // Qual arma este item concede?
    public int ammoRemaining;

    [Header("Polimento Visual")]
    public float rotationSpeed = 50f; // Para a arma ficar rodando levemente no ar (opcional)

    void Update()
    {
        // Rotação constante para o jogador perceber que é um item coletável
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }
}
