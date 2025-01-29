using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.UIElements;

[CustomEditor(typeof(Readme))]
[InitializeOnLoad]
sealed class ReadmeEditor : Editor
{
    const string k_ussFormat = "Assets/TutorialInfo/Scripts/Editor/ReadmeEditor{0}.uss";
    const string k_ShowedReadmeSessionStateName = "ReadmeEditor.showedReadme";
    const string k_ReadmeSourceDirectory = "Assets/TutorialInfo";

    bool m_ImGUIStyleInitialized;
    [SerializeField] GUIStyle m_TitleStyle;

    static ReadmeEditor()
        => EditorApplication.delayCall += SelectReadmeAutomatically;

    static void SelectReadmeAutomatically()
    {
        if (!SessionState.GetBool(k_ShowedReadmeSessionStateName, false))
        {
            var readme = SelectReadme();
            SessionState.SetBool(k_ShowedReadmeSessionStateName, true);

            if (readme && !readme.loadedLayout)
            {
                EditorUtility.LoadWindowLayout(Path.Combine(Application.dataPath, "TutorialInfo/Layout.wlt"));
                readme.loadedLayout = true;
            }
        }
    }

    static Readme SelectReadme()
    {
        var ids = AssetDatabase.FindAssets("Readme t:Readme");
        if (ids.Length != 1)
        {
            Debug.Log("Couldn't find a readme");
            return null;
        }

        var readmeObject = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(ids[0]));
        Selection.objects = new UnityEngine.Object[] { readmeObject };
        return (Readme)readmeObject;
    }
    
    void RemoveTutorial()
    {
        if (EditorUtility.DisplayDialog("Remove Readme Assets",
            
            $"All contents under {k_ReadmeSourceDirectory} will be removed, are you sure you want to proceed?",
            "Proceed",
            "Cancel"))
        {
            if (Directory.Exists(k_ReadmeSourceDirectory))
            {
                FileUtil.DeleteFileOrDirectory(k_ReadmeSourceDirectory);
                FileUtil.DeleteFileOrDirectory(k_ReadmeSourceDirectory + ".meta");
            }
            else
            {
                Debug.Log($"Could not find the Readme folder at {k_ReadmeSourceDirectory}");
            }

            var readmeAsset = SelectReadme();
            if (readmeAsset != null)
            {
                var path = AssetDatabase.GetAssetPath(readmeAsset);
                FileUtil.DeleteFileOrDirectory(path + ".meta");
                FileUtil.DeleteFileOrDirectory(path);
            }

            AssetDatabase.Refresh();
        }
    }

    //Remove ImGUI
    protected sealed override void OnHeaderGUI() { }
    public sealed override void OnInspectorGUI() { }

    public override VisualElement CreateInspectorGUI()
    {
        VisualElement root = new();
        root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(string.Format(k_ussFormat,"")));
        root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(string.Format(k_ussFormat, EditorGUIUtility.isProSkin ? "Dark" : "Light")));

        var readme = (Readme)target;

        //Header
        VisualElement title = new();
        title.AddToClassList("title");
        title.Add(new Image() { image = readme.icon });
        title.Add(new Label(readme.title));
        root.Add(title);

        //Content
        foreach (var section in readme.sections)
        {
            VisualElement part = new();
            part.AddToClassList("section");

            if (!string.IsNullOrEmpty(section.heading))
            {
                var header = new Label(section.heading);
                header.AddToClassList("header");
                part.Add(header);
            }

            if (!string.IsNullOrEmpty(section.text))
                part.Add(new Label(section.text));

            if (!string.IsNullOrEmpty(section.linkText))
            {
                var link = new Label(section.linkText);
                link.AddToClassList("link");
                link.RegisterCallback<ClickEvent>(evt => Application.OpenURL(section.url));
                part.Add(link);
            }

            root.Add(part);
        }

        var button = new Button(RemoveTutorial) { text = "Remove Readme Assets" };
        button.AddToClassList("remove-readme-button");
        root.Add(button);

        return root;
    }
}
