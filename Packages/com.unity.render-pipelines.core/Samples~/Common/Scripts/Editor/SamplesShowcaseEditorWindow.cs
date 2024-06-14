using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Reflection;


public class SamplesWindow : EditorWindow
{
     [InitializeOnLoadMethod]
    static void Init() 
    {
        EditorSceneManager.sceneOpened += SceneOpened;
    }


    static void SceneOpened(Scene scene, OpenSceneMode openSceneMode)
    {
        var currentShowcase = (SamplesShowcase)FindFirstObjectByType(typeof(SamplesShowcase));
        if(currentShowcase != null)
            Selection.activeGameObject = currentShowcase.gameObject;
    }



}



