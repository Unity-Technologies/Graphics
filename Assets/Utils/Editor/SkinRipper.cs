using UnityEngine;
using UnityEditor;

public class SkinRipper : EditorWindow
{
    [MenuItem("Tools/Rip Editor Skin")]
    static public void SaveEditorSkin()
    {
        GUISkin skin = Instantiate<GUISkin>(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector));
        AssetDatabase.CreateAsset(skin, "Assets/RippedEditorSkin.guiskin");
    }
}
