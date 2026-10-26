using UnityEngine;
using UnityEditor;
using System.IO;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[CustomEditor(typeof(Readme))]
sealed partial class ReadmeEditor : Editor
{
    const string k_ShowedReadmeSessionStateName = "ReadmeEditor.showedReadme";
    const string k_ReadmeSourceDirectory = "Assets/TutorialInfo";

    [OnCodeLoaded]
    static void OnCodeLoaded()
        => EditorApplication.delayCall += SelectReadmeAutomatically;

    static void SelectReadmeAutomatically()
    {
        if (!SessionState.GetBool(k_ShowedReadmeSessionStateName, false))
        {
            SelectReadme();
            SessionState.SetBool(k_ShowedReadmeSessionStateName, true);
        }
    }

    static Readme SelectReadme()
    {
        var ids = AssetDatabase.FindAssetGUIDs("Readme t:Readme");
        if (ids.Length != 1)
        {
            Debug.LogWarning("Couldn't find a readme");
            return null;
        }

        var readmeObject = AssetDatabase.LoadAssetByGUID<Readme>(ids[0]);
        Selection.activeObject = readmeObject;
        return readmeObject;
    }

    static void RemoveTutorial()
    {
        if (EditorUtility.DisplayDialog("Remove Readme Assets",
                                        $"All contents under {k_ReadmeSourceDirectory} will be removed, are you sure you want to proceed?",
                                        "Proceed",
                                        "Cancel"))
        {
            if (Directory.Exists(k_ReadmeSourceDirectory))
                AssetDatabase.DeleteAsset(k_ReadmeSourceDirectory);
            else
                Debug.LogWarning($"Could not find the Readme folder at {k_ReadmeSourceDirectory}");

            var readmeAsset = SelectReadme();
            if (readmeAsset != null)
            {
                var path = AssetDatabase.GetAssetPath(readmeAsset);
                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.Refresh();
        }
    }

    public override VisualElement CreateInspectorGUI()
    {
        var readme = (Readme)target;

        VisualElement root = new();
        root.styleSheets.Add(readme.commonStyle);
        root.styleSheets.Add(EditorGUIUtility.isProSkin ? readme.darkStyle : readme.lightStyle);

        VisualElement ChainWithClass(VisualElement created, string className)
        {
            created.AddToClassList(className);
            return created;
        }

        //Header
        VisualElement title = new();
        title.AddToClassList("title");
        title.Add(ChainWithClass(new Image { image = readme.icon }, "title__icon"));
        title.Add(ChainWithClass(new Label(readme.title), "title__text"));
        root.Add(title);

        //Content
        foreach (var section in readme.sections)
        {
            VisualElement part = new();
            part.AddToClassList("section");

            if (!string.IsNullOrEmpty(section.heading))
                part.Add(ChainWithClass(new Label(section.heading), "section__header"));

            if (!string.IsNullOrEmpty(section.text))
                part.Add(ChainWithClass(new Label(section.text), "section__body"));

            if (!string.IsNullOrEmpty(section.linkText))
            {
                var link = ChainWithClass(new Label(section.linkText), "section__link");
                link.RegisterCallback<ClickEvent, string>((_, url) => Application.OpenURL(url), section.url);
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
