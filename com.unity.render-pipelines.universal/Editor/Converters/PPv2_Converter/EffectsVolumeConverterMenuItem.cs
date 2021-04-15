#if PPV2_EXISTS
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using BIRPRendering = UnityEngine.Rendering.PostProcessing;
using URPRendering = UnityEngine.Rendering.Universal;

public class EffectsVolumeConverterMenuItem : EditorWindow
{
    [MenuItem("PS Converters/1 - Convert Selected Volume")]
    private static void ConvertSelectedVolume()
    {
        var volumes = Selection.GetFiltered<BIRPRendering.PostProcessVolume>(SelectionMode.Deep);

        // TODO: Cache this first
        var converters = GetAllBIRPConverters();

        foreach (var oldVolume in volumes)
        {
            // TODO: Port this converter logic
            ConvertVolumeComponent(oldVolume, converters);
        }

        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();
    }

    [MenuItem("PS Converters/1 - Convert Selected Volume", isValidateFunction: true)]
    private static bool ConvertSelectedVolumeValidated()
    {
        return Selection.GetFiltered<BIRPRendering.PostProcessVolume>(SelectionMode.TopLevel).Any();
    }

    [MenuItem("PS Converters/2 - Convert Selected Layer")]
    private static void ConvertSelectedLayer()
    {
        var layers = Selection.GetFiltered<BIRPRendering.PostProcessLayer>(SelectionMode.Deep);

        foreach (var oldLayer in layers)
            // TODO: Port this converter logic
        {
            var siblingCamera = oldLayer.GetComponent<URPRendering.UniversalAdditionalCameraData>();

            // PostProcessLayer requires a sibling Camera component, but
            // we check it here just in case something weird went happened.
            if (!siblingCamera) continue;

            // The presence of a PostProcessLayer implies the Camera should render post-processes
            siblingCamera.renderPostProcessing = true;

            siblingCamera.volumeLayerMask = oldLayer.volumeLayer;
            siblingCamera.volumeTrigger = oldLayer.volumeTrigger;
            siblingCamera.stopNaN = oldLayer.stopNaNPropagation;

            siblingCamera.antialiasingQuality = (URPRendering.AntialiasingQuality)oldLayer.subpixelMorphologicalAntialiasing.quality;

            switch (oldLayer.antialiasingMode)
            {
                case BIRPRendering.PostProcessLayer.Antialiasing.None:
                    siblingCamera.antialiasing = URPRendering.AntialiasingMode.None;
                    break;
                case BIRPRendering.PostProcessLayer.Antialiasing.FastApproximateAntialiasing:
                    siblingCamera.antialiasing = URPRendering.AntialiasingMode.FastApproximateAntialiasing;
                    break;
                case BIRPRendering.PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing:
                    siblingCamera.antialiasing = URPRendering.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    break;
                default:
                    // Default to the the most performant mode, since "None" is an explicit option.
                    siblingCamera.antialiasing = URPRendering.AntialiasingMode.FastApproximateAntialiasing;
                    break;
            }
        }

        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();
    }

    [MenuItem("PS Converters/2 - Convert Selected Layer", isValidateFunction: true)]
    private static bool ConvertSelectedLayerValidated()
    {
        return Selection.GetFiltered<BIRPRendering.PostProcessVolume>(SelectionMode.TopLevel).Any();
    }

    private static void ConvertVolumeComponent(
        BIRPRendering.PostProcessVolume oldVolume,
        IEnumerable<PostProcessEffectSettingsConverter> converters)
    {
        var gameObject = oldVolume.gameObject;

        // TODO: Should this just get the component and attempt a profile conversion to it anyway?
        //       Not sure how best to handle this from a UX perspective.
        if (gameObject.GetComponent<Volume>()) return;

        var newVolume = gameObject.AddComponent<Volume>();

        newVolume.priority = oldVolume.priority;
        newVolume.weight = oldVolume.weight;
        newVolume.blendDistance = oldVolume.blendDistance;
        newVolume.isGlobal = oldVolume.isGlobal;
        newVolume.enabled = oldVolume.enabled;

        newVolume.sharedProfile = ConvertVolumeProfileAsset(oldVolume.sharedProfile, converters);

        // TODO: Verify: This should only be true at runtime, so just ignore it, yeah?
        //       - Do we care about projects that serialized profile instances in Scene data?
        //       - Is that even possible?
        // if (oldVolume.HasInstantiatedProfile())
        // {
        //     newVolume.profile = ConvertVolumeProfileAsset(oldVolume.profile);
        // }

        EditorUtility.SetDirty(gameObject);
    }

    private static VolumeProfile ConvertVolumeProfileAsset(
        BIRPRendering.PostProcessProfile oldProfile,
        IEnumerable<PostProcessEffectSettingsConverter> converters)
    {
        if (!oldProfile) return null;

        var oldPath = AssetDatabase.GetAssetPath(oldProfile);
        var oldDirectory = Path.GetDirectoryName(oldPath);
        var oldName = Path.GetFileNameWithoutExtension(oldPath);
        var newPath = Path.Combine(oldDirectory, $"{oldName}(URP).asset");
        if (File.Exists(newPath))
        {
            // TODO: Maybe a dialogue box asking about replacement?
            Debug.Log("Converted profile already exists, yo!");
            return AssetDatabase.LoadAssetAtPath<VolumeProfile>(newPath);
        }

        // TODO: Create a "Parent" asset type which has references to RP-appropriate profiles,
        //       which should be set/created as children; this would allow users to keep per-RP
        //       profiles around with unique settings for each.
        //       - The converter should create the parent, and the new profile as a referenced child asset.
        //       - The parent asset should have the ability to delete its children (upon de-reference?)

        var newProfile = CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(newProfile, newPath);

        foreach (var oldSettings in oldProfile.settings)
        {
            // TODO: loop through converters and check if this has a profile for each implementation of "T"
            foreach (var converter in converters)
            {
                converter.AddConvertedProfileSettingsToProfile(oldSettings, newProfile);
            }
        }

        EditorUtility.SetDirty(newProfile);

        return newProfile;
    }

    public static IEnumerable<PostProcessEffectSettingsConverter> GetAllBIRPConverters()
    {
        var baseType = typeof(PostProcessEffectSettingsConverter);

        var assembly = Assembly.GetAssembly(baseType);
        var derivedTypes = assembly
            .GetTypes()
            .Where(t =>
                t.BaseType != null &&
                t.BaseType == baseType)
            .Select(t => CreateInstance(t) as PostProcessEffectSettingsConverter);

        // TODO: deleteme:
        Debug.Log(string.Join("\n", derivedTypes.Select(t => t.ToString())));

        return derivedTypes;
    }
}
#endif
