using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class UniqueID : MonoBehaviour
{
    [SerializeField] private string id;
    public string ID => id;

#if UNITY_EDITOR
    private void Awake()
    {
        // Garante que exista um id ao editar/instanciar
        if (string.IsNullOrEmpty(id))
        {
            id = System.Guid.NewGuid().ToString();
            EditorUtility.SetDirty(this);
            // Se for prefab instance, marca cena para salvar
            if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
    }
#endif
}
