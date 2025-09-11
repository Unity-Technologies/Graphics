using UnityEngine;
using UnityEngine.SceneManagement;

public class SwitchScene : MonoBehaviour
{
    public float delayInSeconds = 1.0f;
    public string sceneToLoad;
    public string sceneToUnload;

    private float m_StartTime;
    private bool m_SceneSwitched = false;

    void Start()
    {
        m_StartTime = Time.time;
        m_SceneSwitched = false;
    }

    // Update is called once per frame
    void Update()
    {
        if ((Time.time - m_StartTime) > delayInSeconds && !m_SceneSwitched)
        {
            m_SceneSwitched = true;

            var loadHandle = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
            loadHandle.completed += operation =>
            {
                var unloadHandle = SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName(sceneToUnload), UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
                unloadHandle.completed += operation => {
                    // Unblock the waiting so we can take a screenshot
                    var settings = Object.FindObjectOfType<GraphicsTestSettingsCustom>();
                    if (settings)
                        settings.Wait = false;
                };
            };
        }
    }
}
