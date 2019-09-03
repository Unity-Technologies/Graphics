using UnityEngine;
using UnityEngine.SceneManagement;

public class GotoNextScene : MonoBehaviour
{
    private int m_SceneIndex;
    public void Awake()
    {
        m_SceneIndex = 0;
        DontDestroyOnLoad(gameObject);
    }

    public void Update()
    {
        if ((Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) ||
            Input.GetMouseButtonDown(0))
        {
            m_SceneIndex = (m_SceneIndex + 1) % SceneManager.sceneCountInBuildSettings;
            SceneManager.LoadScene(m_SceneIndex);
        }
    }
}
