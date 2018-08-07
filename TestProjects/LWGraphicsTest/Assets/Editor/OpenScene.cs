using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class MyWindow : EditorWindow
{
    string scene;

    // Add menu named "My Window" to the Window menu
    [MenuItem("Window/My Window")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        MyWindow window = (MyWindow)EditorWindow.GetWindow(typeof(MyWindow));
        window.Show();
    }

    void OnGUI()
    {
        scene = EditorGUILayout.TextField("Scene:", scene);

        if (GUILayout.Button("Load"))
            EditorSceneManager.OpenScene(scene); 
    }
}
