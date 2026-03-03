using UnityEngine;

public class ManualLoad : MonoBehaviour
{
    [Header("Configuração")]
    public bool loadOnStart = false;

    [Header("Cena e Spawn")]
    public string sceneToLoad;
    public string spawnPointID = "Default";

    private void Start()
    {
        if (loadOnStart)
        {
            Load();
        }
    }

    // Você pode chamar isso por um botão do UI
    public void Load()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("ManualLoad: Nenhuma cena definida para carregar.");
            return;
        }

        SceneLoader.Instance.LoadScene(sceneToLoad, spawnPointID);
    }
}
