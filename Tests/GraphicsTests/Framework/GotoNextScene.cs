using UnityEngine;
using UnityEngine.SceneManagement;

public class GotoNextScene : MonoBehaviour 
{
	public int m_NextSceneIndex = 0;

	void Update()
	{
		if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
			SceneManager.LoadScene(m_NextSceneIndex);
	}
}
