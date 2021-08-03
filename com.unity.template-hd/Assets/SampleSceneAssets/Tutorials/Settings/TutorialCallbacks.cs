using UnityEngine;
using UnityEditor;
using Unity.Tutorials.Core.Editor;

/// <summary>
/// Implement your Tutorial callbacks here.
/// </summary>
[CreateAssetMenu(fileName = DefaultFileName, menuName = "Tutorials/" + DefaultFileName + " Instance")]
public class TutorialCallbacks : ScriptableObject
{
    /// <summary>
    /// The default file name used to create asset of this class type.
    /// </summary>
    public const string DefaultFileName = "TutorialCallbacks";

    /// <summary>
    /// Creates a TutorialCallbacks asset and shows it in the Project window.
    /// </summary>
    /// <param name="assetPath">
    /// A relative path to the project's root. If not provided, the Project window's currently active folder path is used.
    /// </param>
    /// <returns>The created asset</returns>
    public static ScriptableObject CreateAndShowAsset(string assetPath = null)
    {
        assetPath = assetPath ?? $"{TutorialEditorUtils.GetActiveFolderPath()}/{DefaultFileName}.asset";
        var asset = CreateInstance<TutorialCallbacks>();
        AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(assetPath));
        EditorUtility.FocusProjectWindow(); // needed in order to make the selection of newly created asset to really work
        Selection.activeObject = asset;
        return asset;
    }

    /// <summary>
    /// Example callback for basic UnityEvent
    /// </summary>
    public void ExampleMethod()
    {
        Debug.Log("ExampleMethod");
    }

    /// <summary>
    /// Example callbacks for ArbitraryCriterion's BoolCallback
    /// </summary>
    /// <returns></returns>
    public bool DoesFooExist()
    {
        return GameObject.Find("Foo") != null;
    }

    /// <summary>
    /// Implement the logic to automatically complete the criterion here, if wanted/needed.
    /// </summary>
    /// <returns>True if the auto-completion logic succeeded.</returns>
    public bool AutoComplete()
    {
        var foo = GameObject.Find("Foo");
        if (!foo)
            foo = new GameObject("Foo");
        return foo != null;
    }
}
