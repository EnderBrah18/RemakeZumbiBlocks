using UnityEngine;

public class SaveTestUI : MonoBehaviour
{
    public void SaveGame()
    {
        SaveSystem.Instance.SaveGame();
        Debug.Log("Jogo salvo!");
    }

    public void LoadGame()
    {
        SaveSystem.Instance.LoadGame();
        Debug.Log("Jogo carregado!");
    }
}
