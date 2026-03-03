using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    [Header("Configurações")]
    public string mainMenuSceneName = "MenuPrincipal";
    //public string loadingScreenName = "LoadingScreen";

    //public GameObject loadingScreen;
    private Slider progressBar;

    private bool sceneReadyToActivate = false;
    private bool manualMode = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            //SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /*private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryFindLoadingScreen();
    }

    private void TryFindLoadingScreen()
    {
        GameObject found = GameObject.Find(loadingScreenName);

        if (found != null)
        {
            loadingScreen = found;
            progressBar = loadingScreen.GetComponentInChildren<Slider>();
            loadingScreen.SetActive(false);
        }
    }*/

    // Carrega cena, opcional modo manual
    public void LoadScene(string sceneName, string spawnPointID = "Default", bool manual = false)
    {
        manualMode = manual;
        sceneReadyToActivate = !manual; // se manual = false, ativa automaticamente

        string current = SceneManager.GetActiveScene().name;

        if (current == mainMenuSceneName)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        StartCoroutine(LoadAsync(sceneName));
    }

    private IEnumerator LoadAsync(string sceneName)
    {
        /*if (loadingScreen == null)
            TryFindLoadingScreen();*/

        /*if (loadingScreen != null)
            loadingScreen.SetActive(true);*/

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = true; // deixa a cena ativa imediatamente

        while (!op.isDone)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f);
            if (progressBar != null)
                progressBar.value = progress;

            yield return null;
        }

        // Se for modo manual, a LoadingScreen fica até você chamar HideLoadingScreen()
        // Caso automático (ex: cena principal), desativa a LoadingScreen imediatamente
        /*if (!manualMode && loadingScreen != null)
            loadingScreen.SetActive(false);*/
    }

    // Para cenas em modo manual
    /*public void SceneIsReady()
    {
        sceneReadyToActivate = true;
        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }

    // Método antigo ainda disponível
    public void HideLoadingScreen()
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }*/

    public void LoadSceneAndRestoreSave(string sceneName)
    {
        StartCoroutine(LoadSceneAndRestoreCoroutine(sceneName));
    }

    private IEnumerator LoadSceneAndRestoreCoroutine(string sceneName)
    {
        manualMode = false;
        sceneReadyToActivate = true;

        /*if (loadingScreen == null)
            TryFindLoadingScreen();

        if (loadingScreen != null)
            loadingScreen.SetActive(true);*/

        // Carrega cena
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = true;

        // Atualiza barra de progresso
        while (!op.isDone)
        {
            if (progressBar != null)
                progressBar.value = Mathf.Clamp01(op.progress / 0.9f);

            yield return null;
        }

        // Espera 1 frame para toda a cena inicializar
        yield return null;

        // Restaura o save
        SaveSystem.Instance.LoadGameAfterSceneLoaded();

        // Espera mais 5 segundos antes de desligar o loading
        yield return new WaitForSeconds(5f);

        // Desliga loading
        /*if (loadingScreen != null)
            loadingScreen.SetActive(false);*/
    }
}