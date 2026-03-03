using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    private Dictionary<string, ISavable> savables = new Dictionary<string, ISavable>();

    private Dictionary<string, ISOSavable> soSavables =
    new Dictionary<string, ISOSavable>();

    private string saveFilePath => Path.Combine(Application.persistentDataPath, "save.json");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Debug.Log("SAVE SYSTEM AWAKE");

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Registrar ISavable
    public void RegisterSavable(ISavable s)
    {
        var key = s.GetSaveKey();
        if (!savables.ContainsKey(key))
        {
            savables.Add(key, s);
            Debug.Log($"[SaveSystem] Registered savable: {key}");
        }
        else
        {
            Debug.LogWarning($"[SaveSystem] Attempt to register already registered key: {key}");
        }
    }

    public void UnregisterSavable(ISavable savable)
    {
        string key = savable.GetSaveKey();
        if (savables.ContainsKey(key))
            savables.Remove(key);
    }

    // Registrar ISOSavable
    public void RegisterSOSavable(ISOSavable so)
    {
        var key = so.GetSaveKey();
        if (!soSavables.ContainsKey(key))
            soSavables.Add(key, so);
    }

    public void UnregisterSOSavable(ISOSavable so)
    {
        var key = so.GetSaveKey();
        if (soSavables.ContainsKey(key))
            soSavables.Remove(key);
    }

    // Salvar todos os ISavable
    public void SaveGame()
    {
        Dictionary<string, string> saveData = new Dictionary<string, string>();
        foreach (var savable in savables.Values)
        {
            saveData[savable.GetSaveKey()] = savable.SaveData();
        }

        foreach (var so in soSavables.Values)
        {
            saveData[so.GetSaveKey()] = so.SaveData();
        }

        string json = JsonUtility.ToJson(new SerializationWrapper(saveData), true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Jogo salvo em: " + saveFilePath);
    }

    // Carregar todos os ISavable
    public void LoadGame()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("Nenhum save encontrado.");
            return;
        }

        string json = File.ReadAllText(saveFilePath);
        var wrapper = JsonUtility.FromJson<SerializationWrapper>(json);

        foreach (var kvp in wrapper.ToDictionary())
        {
            if (savables.TryGetValue(kvp.Key, out ISavable savable))
            {
                savable.LoadData(kvp.Value);
            }
        }

        foreach (var kvp in wrapper.ToDictionary())
        {
            if (savables.TryGetValue(kvp.Key, out ISavable savable))
                savable.LoadData(kvp.Value);
            else if (soSavables.TryGetValue(kvp.Key, out ISOSavable so))
                so.LoadData(kvp.Value);
        }

        Debug.Log("Jogo carregado!");
    }

    public void LoadGameAfterSceneLoaded()
    {
        string json = File.ReadAllText(saveFilePath); // seu método que pega o json salvo

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("Nenhum save encontrado.");
            return;
        }

        var wrapper = JsonUtility.FromJson<SerializationWrapper>(json);

        var dict = wrapper.ToDictionary();

        Debug.Log("Restaurando save (" + dict.Count + " itens)");

        // Restaurar todos ISavables existentes na cena
        foreach (var kvp in dict)
        {
            if (savables.TryGetValue(kvp.Key, out ISavable savable))
                savable.LoadData(kvp.Value);
            else if (soSavables.TryGetValue(kvp.Key, out ISOSavable so))
                so.LoadData(kvp.Value);
        }

        Debug.Log("Save restaurado com sucesso!");
    }
}
