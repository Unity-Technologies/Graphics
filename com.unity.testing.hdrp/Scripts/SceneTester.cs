using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
//using UnityEngine.TestTools.Graphics;

public class SceneTester : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Load test scenes.");

        DontDestroyOnLoad(gameObject);

        // On scene load, set the application to windowed and target resolution.
        /*
        SceneManager.sceneLoaded += (Scene scene, LoadSceneMode loadSceneMode) =>
        {
            GraphicsTestSettings testSettings = FindObjectOfType<GraphicsTestSettings>();
            if (testSettings != null)
            {
                Screen.SetResolution( testSettings.ImageComparisonSettings.TargetWidth, testSettings.ImageComparisonSettings.TargetHeight, FullScreenMode.Windowed );
            }
        };
        */

        if (SceneManager.sceneCountInBuildSettings > 1)
            SceneManager.LoadScene(1);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Switch to next scene.");
            // Load next scene or scene index 1 if looped
            SceneManager.LoadScene(Mathf.Max(1, (SceneManager.GetActiveScene().buildIndex + 1) % SceneManager.sceneCountInBuildSettings));
        }
    }
}
