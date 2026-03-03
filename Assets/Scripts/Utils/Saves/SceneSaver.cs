using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class SceneSaver : MonoBehaviour, ISavable
{
    public static SceneSaver Instance;

    private const string SAVE_KEY_SCENE = "SavedSceneName";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.RegisterSavable(this);
        else
            StartCoroutine(WaitAndRegister());
    }

    private IEnumerator WaitAndRegister()
    {
        yield return new WaitUntil(() => SaveSystem.Instance != null);
        SaveSystem.Instance.RegisterSavable(this);
    }

    private void OnDisable()
    {
        SaveSystem.Instance.UnregisterSavable(this);
    }

    /// <summary>
    /// Salva o nome da cena atual.
    /// </summary>
     // --------- AJUSTAR ESSES 3 MÉTODOS QUANDO ENVIAR SEU ISavable ---------

    public string GetSaveKey()
    {
        return SAVE_KEY_SCENE;
    }

    public string SaveData()
    {
        string scene = SceneManager.GetActiveScene().name;
        return JsonUtility.ToJson(new SceneSaveData(scene));
    }

    public void LoadData(string json)
    {
        SceneSaveData data = JsonUtility.FromJson<SceneSaveData>(json);
        SceneLoader.Instance.LoadScene(data.sceneName);
    }

    [System.Serializable]
    public class SceneSaveData
    {
        public string sceneName;

        public SceneSaveData(string scene)
        {
            sceneName = scene;
        }
    }
}

