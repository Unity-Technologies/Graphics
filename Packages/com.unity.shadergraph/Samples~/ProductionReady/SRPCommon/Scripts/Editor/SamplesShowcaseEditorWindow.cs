using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Reflection;


public class PRSSamplesWindow : EditorWindow
{
     [InitializeOnLoadMethod]
    static void Init() 
    {
        EditorSceneManager.sceneOpened += SceneOpened;
    }


    static void SceneOpened(Scene scene, OpenSceneMode openSceneMode)
    {
        OpenSamplesShowcaseWindow().UpdateSamplesWindow();

    }

    public static PRSSamplesWindow OpenSamplesShowcaseWindow()
    {
        PRSSamplesWindow window = GetWindow<PRSSamplesWindow>("Samples Showcase", true, System.Type.GetType("UnityEditor.InspectorWindow,UnityEditor.dll"));
        return window;
    }

    void CreateGUI () 
    {
         UpdateSamplesWindow();
    }
  
    private void UpdateSamplesWindow()
    {
        VisualElement root = rootVisualElement;
        var currentShowcase = (PRSSamplesShowcase)FindFirstObjectByType(typeof(PRSSamplesShowcase));
        root.Clear();
        if (currentShowcase != null)
        {
            InspectorElement showcaseInspector = new InspectorElement(currentShowcase);
            root.Add(showcaseInspector);
        }
        else
        {
            this.Close();
        }
    }

void HideOpenWindowButton()
{
    VisualElement root = rootVisualElement;
    if(root !=null)
    {
        var OpenInWindowButton =  root.Q<Button>(name = "OpenInWindowButton");
        if(OpenInWindowButton !=null)
       { 
            OpenInWindowButton.style.display = DisplayStyle.None;
       }
    }
}

void OnGUI()
{
    HideOpenWindowButton();
}


}



