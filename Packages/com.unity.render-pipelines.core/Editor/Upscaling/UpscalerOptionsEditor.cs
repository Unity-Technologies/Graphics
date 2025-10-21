#if ENABLE_UPSCALER_FRAMEWORK
#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// This custom editor ensures that when drawing any UpscalerOptions object,
/// the default "Script" field is not shown. Applies to all derived options.
/// </summary>
[CustomEditor(typeof(UnityEngine.Rendering.UpscalerOptions), true)]
public class UpscalerOptionsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // DrawDefaultInspector renders all serialized fields except for the "Script" field
        // and any fields marked with [HideInInspector].
        DrawDefaultInspector();
    }
}
#endif
#endif
