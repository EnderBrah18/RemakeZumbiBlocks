using UnityEngine;

public class Door : MonoBehaviour
{
    public string sceneToLoad;
    public string spawnIDInNewScene;
    public bool ManualLoad = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SceneLoader.Instance.LoadScene(sceneToLoad, spawnIDInNewScene, ManualLoad);
        }
    }
}
