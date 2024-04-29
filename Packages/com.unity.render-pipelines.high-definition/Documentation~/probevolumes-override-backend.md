# Override the Adaptive Probe Volumes baking backend

Unity provides a default implementation for baking all the data required by Adaptive Probe Volumes. However, you can override the default bakers to customize how HDRP bakes individual data types.
	
When you create and apply a custom baker to override a default baker, all baking functionalities in Unity use that baker. This includes baking from any tab of the Lighting window, and preview bakes via the Probe Adjustment Volume Inspector. It also includes bakes that you launch via script with [Lightmapping](https://docs.unity3d.com/ScriptReference/Lightmapping.html) APIs and [AdaptiveProbeVolumes](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/api/UnityEngine.Rendering.AdaptiveProbeVolumes.html) APIs.
	

There are three types of baked data, which you can override independently: Lighting data, Virtual Offset data, and sky occlusion data.

### Lighting data
	
The Lighting data baker bakes the incoming irradiance as spherical harmonics, and bakes the validity for every probe.

To override the default Lighting data baker:

1. Create a class that inherits from [LightingBaker](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/api/UnityEngine.Rendering.AdaptiveProbeVolumes.LightingBaker.html), and provide an implementation for all abstract methods. In the HDRP Graphics repo, refer to [ProbeGIBaking.LightTransport.cs](https://github.com/Unity-Technologies/Graphics/blob/master/Packages/com.unity.render-pipelines.core/Editor/Lighting/ProbeVolume/ProbeGIBaking.LightTransport.cs#L57) for reference implementation.
2. Use [SetLightingBakerOverride](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/api/UnityEngine.Rendering.AdaptiveProbeVolumes.html#UnityEngine_Rendering_AdaptiveProbeVolumes_SetLightingBakerOverride_UnityEngine_Rendering_AdaptiveProbeVolumes_LightingBaker_) and set your new `LightingBaker` class as the parameter.

### Virtual Offset data
	
The Virtual Offset data baker bakes the offset that HDRP should apply to a probe in order to move it out of surrounding geometry. 

To override the default Virtual Offset data baker:

1. Create a class that inherits from [VirtualOffsetBaker](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/api/UnityEngine.Rendering.AdaptiveProbeVolumes.VirtualOffsetBaker.html) class. In the HDRP Graphics repo, refer to [ProbeGIBaking.VirtualOffset.cs](https://github.com/Unity-Technologies/Graphics/blob/master/Packages/com.unity.render-pipelines.core/Editor/Lighting/ProbeVolume/ProbeGIBaking.VirtualOffset.cs#L44) for reference implementation.
2. Use [SetVirtualOffsetBakerOverride](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/api/UnityEngine.Rendering.AdaptiveProbeVolumes.html#UnityEngine_Rendering_AdaptiveProbeVolumes_SetVirtualOffsetBakerOverride_UnityEngine_Rendering_AdaptiveProbeVolumes_VirtualOffsetBaker_) and set your new `VirtualOffsetBaker` class as the parameter.

### Sky occlusion data
	
The default sky occlusion data baker computes the amount of lighting that comes from the sky in every direction as a spherical harmonic. It also optionally computes the direction from which APV should sample the sky lighting at runtime for every probe.

To override the default Virtual Offset data baker:

1. Create a class that inherits from [SkyOcclusionBaker](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/api/UnityEngine.Rendering.AdaptiveProbeVolumes.SkyOcclusionBaker.html) class. In the HDRP Graphics repo, refer to [ProbeGIBaking.SkyOcclusion.cs](https://github.com/Unity-Technologies/Graphics/blob/master/Packages/com.unity.render-pipelines.core/Editor/Lighting/ProbeVolume/ProbeGIBaking.SkyOcclusion.cs#L116) for reference implementation.
2. Use [SetSkyOcclusionBakerOverride](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/api/UnityEngine.Rendering.AdaptiveProbeVolumes.html#UnityEngine_Rendering_AdaptiveProbeVolumes_SetSkyOcclusionBakerOverride_UnityEngine_Rendering_AdaptiveProbeVolumes_SkyOcclusionBaker_) and set your new `SkyOcclusionBaker` class as the parameter.

## Example script

The following example script creates a tab in the Lighting window, and creates a custom `LightingBaker` that sets all probes to a constant color.
	
```cs
class CustomBakerLightingTab : LightingWindowTab
{
    bool active = false;
    CustomLightTransport backend;
    Vector2 scrollPosition = Vector2.zero;

    static class Styles
    {
        public static readonly GUIContent generateLighting = new GUIContent("Generate Lighting");
        public static readonly string[] bakeOptionsText = { "Clear Baked Data" };
    }

    public override void OnEnable()
    {
        titleContent = new GUIContent("Custom Baker");
        backend = new CustomLightTransport();
    }

    public override void OnGUI()
    {
        EditorGUIUtility.hierarchyMode = true;
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        active = EditorGUILayout.Toggle("Override Lighting Backend", active);
        AdaptiveProbeVolumes.SetLightingBakerOverride(active ? backend : null);

        using (new EditorGUI.DisabledScope(!active))
        using (new EditorGUI.IndentLevelScope())
        {
            backend.overrideColor = EditorGUILayout.ColorField(new GUIContent("Override Color"), backend.overrideColor, false, false, true);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space();
    }

    public override void OnBakeButtonGUI()
    {
        void BakeButtonCallback(object data)
        {
            switch ((int)data)// Order of options defined by Styles.bakeOptionsText
            {
                case 0:
                {
                    Lightmapping.ClearLightingDataAsset();
                    Lightmapping.Clear();
                    break;
                }
                default: Debug.Log("invalid option in BakeButtonCallback"); break;
            }
        }

        if (EditorGUI.LargeSplitButtonWithDropdownList(Styles.generateLighting, Styles.bakeOptionsText, BakeButtonCallback))
            Lightmapping.BakeAsync();
    }

    class CustomLightTransport : AdaptiveProbeVolumes.LightingBaker
    {
        public Color overrideColor = Color.red;

        int bakedProbeCount;
        NativeArray<Vector3> positions;
        SphericalHarmonicsL2 sh;
        
        public override ulong currentStep => (ulong)bakedProbeCount;
        public override ulong stepCount => (ulong)positions.Length;
        
        public NativeArray<SphericalHarmonicsL2> irradianceResults;
        public NativeArray<float> validityResults;
        
        public override NativeArray<SphericalHarmonicsL2> irradiance => irradianceResults;
        public override NativeArray<float> validity => validityResults;

        public override void Initialize(NativeArray<Vector3> probePositions)
        {
            bakedProbeCount = 0;
            positions = probePositions;

            irradianceResults = new NativeArray<SphericalHarmonicsL2>(positions.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            validityResults = new NativeArray<float>(positions.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            sh = default;
            sh.AddAmbientLight(overrideColor);
        }

        public override bool Step()
        {
            // Compute a subset of probes per step to avoid blocking the UI
            const int stepSize = 100;

            for (int i = 0; i < stepSize && currentStep < stepCount; i++)
            {
                irradianceResults[bakedProbeCount] = sh; // Use constant color (may appear black depending on your exposure)
                validityResults[bakedProbeCount] = 0.0f; // Mark all probes as valid
                bakedProbeCount++;
            }

            return true;
        }

        public override void Dispose()
        {
            irradianceResults.Dispose();
            validityResults.Dispose();
        }
    }
}
```
