using UnityEngine;
using UnityEngine.SceneManagement;
public class GISyncronousMode : MonoBehaviour
{
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Setting realtime GI to synchronousMode.");
        DynamicGI.synchronousMode = true;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        DynamicGI.synchronousMode = false;
    }
}
