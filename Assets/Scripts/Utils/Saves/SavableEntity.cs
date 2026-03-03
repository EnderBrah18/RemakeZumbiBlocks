using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(UniqueID))]
public abstract class SavableEntity : MonoBehaviour, ISavable
{
    private UniqueID _uid;
    protected string SceneKey => SceneManager.GetActiveScene().name;

    protected virtual void Awake()
    {
        _uid = GetComponent<UniqueID>();
        if (_uid == null) Debug.LogError($"UniqueID missing on {name}");
    }

    public string GetSaveKey()
    {
        if (_uid == null) return $"{SceneKey}|{gameObject.GetInstanceID()}";
        return $"{SceneKey}|{_uid.ID}";
    }

    public abstract string SaveData();
    public abstract void LoadData(string json);
}
