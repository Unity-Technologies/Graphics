#if ENABLE_METALFX_MODULE && (UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_TVOS || UNITY_VISIONOS)
using UnityEditor;
using UnityEngine.Rendering.MetalFX;

[CustomEditor(typeof(MetalFXTemporalOptions))]
public class MetalFXTemporalOptionsEditor : UpscalerOptionsEditor
{
    protected override bool showsReactiveMaskGeneration => true;
}
#endif
