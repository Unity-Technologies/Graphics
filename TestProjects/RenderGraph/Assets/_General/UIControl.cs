using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIControl : MonoBehaviour
{
    public float scale = 1f;
    public void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    public void Start()
    {
        NextScene();
    }

    void OnGUI()
    {
        GUI.skin.label.fontSize = Mathf.RoundToInt ( 16 * scale );
        //GUI.backgroundColor = new Color(0, 0, 0, .80f);
        GUI.color = new Color(1, 1, 1, 1);
        float w = 410 * scale;
        float h = 90 * scale;
        GUILayout.BeginArea(new Rect(Screen.width - w -5, Screen.height - h -5, w, h), GUI.skin.box);

        //GUI.backgroundColor = new Color(1, 1, 1, .80f);
        GUIStyle customButton = new GUIStyle("button");
        customButton.fontSize = GUI.skin.label.fontSize;
        customButton.fixedHeight = 50 * scale;

        GUILayout.BeginHorizontal();
        if(GUILayout.Button("Prev",customButton)) PrevScene();
        if(GUILayout.Button("Next",customButton)) NextScene();
        GUILayout.EndHorizontal();

        int currentpage = SceneManager.GetActiveScene().buildIndex;
        int totalpages = SceneManager.sceneCountInBuildSettings-1;
        GUILayout.Label( currentpage + " / " + totalpages + " " + SceneManager.GetActiveScene().name );

        GUILayout.EndArea();
    }

    public void NextScene()
    {
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (sceneIndex < SceneManager.sceneCountInBuildSettings - 1)
            SceneManager.LoadScene(sceneIndex + 1);
        else
            SceneManager.LoadScene(1);
    }

    public void PrevScene()
    {
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (sceneIndex > 1)
            SceneManager.LoadScene(sceneIndex - 1);
        else
            SceneManager.LoadScene(SceneManager.sceneCountInBuildSettings - 1);
    }
}
