// UUM-92491: This solve an issue where in 2022.3 FullScreenPassRendererFeature got introduced with another
// guid than in the 6000.0 version. To update the GUID, we are bound to this strange inheritance and migration.
// See also FullScreenPassRendererFeature_OldGUIDEditor, for the in editor downcasting.

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;


[System.Obsolete("Kept for migration purpose only. Do not use (see script for more info) #from(6000.0) (UnityUpgradable) -> FullScreenPassRendererFeature", true)]
class FullScreenPassRendererFeature_OldGUID : UnityEngine.Rendering.Universal.FullScreenPassRendererFeature, ISerializationCallbackReceiver
{
    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
#if UNITY_EDITOR
        // InternalCreate cannot be called in serialization callback... Delaying
        EditorApplication.delayCall += DownCast;
#endif
    }
    
#if UNITY_EDITOR
    void DownCast()
    {
        if (this == null || this.Equals(null)) return;

        const string newGUID = "b00045f12942b46c698459096c89274e";
        const string oldGUID = "6d613f08f173d4dd895bb07b3230baa9";

        // Check current GUID to be extra sure it is the old one to update
        var serializedObject = new SerializedObject(this);
        var scriptProperty = serializedObject.FindProperty("m_Script");
        MonoScript currentScript = scriptProperty.objectReferenceValue as MonoScript;
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(currentScript.GetInstanceID(), out var currentGUID, out var _);
        if (currentGUID != oldGUID)
            return;

        // Mutate to base FullScreenPassRendererFeature script
        var newScript = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(newGUID));
        scriptProperty.objectReferenceValue = newScript;
        serializedObject.ApplyModifiedProperties();
    }
#endif
}
