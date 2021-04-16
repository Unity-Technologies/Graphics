#if PPV2_EXISTS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using BIRPRendering = UnityEngine.Rendering.PostProcessing;
using Object = UnityEngine.Object;
using URPRenderingEditor = UnityEditor.Rendering.Universal;
using URPRendering = UnityEngine.Rendering.Universal;

namespace Editor.Converters
{
    public class PPv2Converter : RenderPipelineConverter
    {
        public override string name => "Post-Processing Stack v2 Converter";

        public override string info =>
            "Converts PPv2 Volumes, Profiles, and Layers to URP Volumes, Profiles, and Cameras.";

        public override Type conversion => typeof(URPRenderingEditor.BuiltInToURPConversion);

        private bool _startingSceneHasBeenClosed;
        private IEnumerable<PostProcessEffectSettingsConverter> effectConverters = null;

        public override void OnInitialize(InitializeConverterContext context)
        {
            // Converters should already be set to null on domain reload,
            // but we're doing it here just in case anything somehow lingers.
            effectConverters = null;

            // We are using separate searchContexts here and Adding them in this order:
            //      - Components from Prefabs & Scenes (Volumes & Layers)
            //      - ScriptableObjects (Profiles)
            //
            // This allows the old objects to be both re-referenced and deleted safely as they are converted in OnRun.
            // The process of converting Volumes will convert Profiles as-needed, and then the explicit followup Profile
            // conversion step will convert any non-referenced assets and delete all old Profiles.

            // Components First
            using var componentContext =
                SearchService.CreateContext("asset", "urp:convert-ppv2component");
            using var componentItems = SearchService.Request(componentContext);
            {
                AddSearchItemsAsConverterAssetEntries(componentItems, context);
            }

            // Then ScriptableObjects
            using var scriptableObjectContext =
                SearchService.CreateContext("asset", "urp:convert-ppv2scriptableobject");
            using var scriptableObjectItems = SearchService.Request(scriptableObjectContext);
            {
                AddSearchItemsAsConverterAssetEntries(scriptableObjectItems, context);
            }
        }

        public override void OnRun(RunConverterContext ctx)
        {
            var items = ctx.items.ToList();

            foreach (var (index, obj) in EnumerateObjects(items, ctx).Where(item => item != null))
            {
                if (!obj)
                {
                    ctx.MarkFailed(index, "Could not be converted because the target object was lost.");
                    continue;
                }

                BIRPRendering.PostProcessVolume[] oldVolumes = null;
                BIRPRendering.PostProcessLayer[] oldLayers = null;

                // TODO: Upcoming changes to GlobalObjectIdentifierToObjectSlow will allow this to be inverted, and the else to be deleted.
#if false
                if (obj is GameObject go)
                {
                    oldVolumes = go.GetComponents<BIRPRendering.PostProcessVolume>();
                    oldLayers = go.GetComponents<BIRPRendering.PostProcessLayer>();
                }
                else if (obj is MonoBehaviour mb)
                {
                    oldVolumes = mb.GetComponents<BIRPRendering.PostProcessVolume>();
                    oldLayers = mb.GetComponents<BIRPRendering.PostProcessLayer>();
                }
#else
                if (obj is GameObject go)
                {
                    oldVolumes = go.GetComponentsInChildren<BIRPRendering.PostProcessVolume>();
                    oldLayers = go.GetComponentsInChildren<BIRPRendering.PostProcessLayer>();
                }
                else if (obj is MonoBehaviour mb)
                {
                    oldVolumes = mb.GetComponentsInChildren<BIRPRendering.PostProcessVolume>();
                    oldLayers = mb.GetComponentsInChildren<BIRPRendering.PostProcessLayer>();
                }
#endif

                // Note: even if nothing needs to be converted, that should still count as success,
                //       though it shouldn't ever actually occur.
                var succeeded = true;
                var errorString = new StringBuilder();

                if (effectConverters == null ||
                    effectConverters.Count() == 0 ||
                    effectConverters.Any(converter => converter == null))
                {
                    effectConverters = GetAllBIRPConverters();
                }

                if (oldVolumes != null)
                {
                    foreach (var oldVolume in oldVolumes)
                    {
                        ConvertVolume(oldVolume, ref succeeded, errorString);
                    }
                }

                if (oldLayers != null)
                {
                    foreach (var oldLayer in oldLayers)
                    {
                        ConvertLayer(oldLayer, ref succeeded, errorString);
                    }
                }

                if (obj is BIRPRendering.PostProcessProfile oldProfile)
                {
                    ConvertProfile(oldProfile, ref succeeded, errorString);
                }

                if (succeeded)
                    ctx.MarkSuccessful(index);
                else
                    ctx.MarkFailed(index, errorString.ToString());
            }
        }

        private void AddSearchItemsAsConverterAssetEntries(ISearchList searchItems, InitializeConverterContext context)
        {
            foreach (var searchItem in searchItems)
            {
                if (searchItem == null || !GlobalObjectId.TryParse(searchItem.id, out var globalId))
                {
                    continue;
                }

                var label = searchItem.provider.fetchLabel(searchItem, searchItem.context);
                var description = searchItem.provider.fetchDescription(searchItem, searchItem.context);

                var item = new ConverterItemDescriptor()
                {
                    name = $"{label} : {description}",
                    info = globalId.ToString(),
                    warningMessage = string.Empty,
                    helpLink = "// TODO",
                };

                context.AddAssetToConvert(item);
            }
        }

        private IEnumerable<Tuple<int, Object>> EnumerateObjects(IReadOnlyList<ConverterItemInfo> items,
            RunConverterContext ctx)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];

                if (GlobalObjectId.TryParse(item.descriptor.info, out var globalId))
                {
                    // Try loading the object
                    // TODO: Upcoming changes to GlobalObjectIdentifierToObjectSlow will allow it
                    //       to return direct references to prefabs and their children.
                    //       Once that change happens there are several items which should be adjusted,
                    //       and are commented with "// TODO: (prefab reference fix)"
                    var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId);

                    // If the object was not loaded, it is probably part of an unopened scene;
                    // if so, then the solution is to first load the scene here.
                    var objIsInSceneOrPrefab = globalId.identifierType == 2; // 2 is IdentifierType.kSceneObject
                    if (!obj &&
                        objIsInSceneOrPrefab)
                    {
                        // Before we open a new scene, we need to save our changes;
                        // however, we should discard any existing changes to the
                        // scene that was open when conversion began.
                        if (_startingSceneHasBeenClosed)
                        {
                            var currentScene = SceneManager.GetActiveScene();
                            EditorSceneManager.SaveScene(currentScene);
                        }

                        _startingSceneHasBeenClosed = true;

                        // Open the Containing Scene Asset in the Hierarchy so the Object can be manipulated
                        var mainAssetPath = AssetDatabase.GUIDToAssetPath(globalId.assetGUID);
                        if (mainAssetPath.EndsWith(".unity", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var mainAsset = AssetDatabase.LoadAssetAtPath<Object>(mainAssetPath);
                            AssetDatabase.OpenAsset(mainAsset);

                            yield return null;

                            // Load the scene, as it cannot be operated on while closed
                            var scene = SceneManager.GetActiveScene();
                            while (!scene.isLoaded)
                            {
                                yield return null;
                            }

                            obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId);
                        }
                        // TODO: (prefab reference fix) This block should be removed once GlobalObjectIdentifierToObjectSlow
                        //       is updated to get proper direct references to prefabs and their child assets.
                        else
                        {
                            var mainAsset = AssetDatabase.LoadAssetAtPath<Object>(mainAssetPath);
                            AssetDatabase.OpenAsset(mainAsset);

                            yield return null;

                            // If a prefab stage was opened, then mainAsset is the root of the
                            // prefab that contains the target object, so reference that for now,
                            // until GlobalObjectIdentifierToObjectSlow is updated
                            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                            {
                                obj = mainAsset;
                            }
                        }
                    }

                    if (obj)
                        yield return new Tuple<int, Object>(i, obj);
                    else
                        ctx.MarkFailed(i, $"Object {globalId.assetGUID} failed to load...");
                }
                else
                {
                    ctx.MarkFailed(i, $"Failed to parse Global ID {item.descriptor.info}...");
                }
            }
        }

#region Conversion_Entry_Points
        private void ConvertVolume(BIRPRendering.PostProcessVolume oldVolume, ref bool succeeded,
            StringBuilder errorString)
        {
            if (!succeeded)
            {
                return;
            }

            if (!oldVolume)
            {
                // TODO: unless there's good way to tell the if the object is just missing because it was already
                //       converted as part of an earlier conversion object, then these two lines should be commented
                //       out or removed.  It should still return though.
                // succeeded = false;
                // errorString.AppendLine("PPv2 PostProcessVolume failed to be converted because the original asset reference was lost during conversion.");
                return;
            }

            if (PrefabUtility.IsPartOfPrefabInstance(oldVolume) &&
                !PrefabUtility.IsAddedComponentOverride(oldVolume))
            {
                Debug.Log("------- CONVERT VOLUME INSTANCE -------");
                // This is a property override on an instance of the component,
                // so override the component instance with the modifications.
                succeeded = ConvertVolumeInstance(oldVolume, errorString);
            }
            else
            {
                Debug.Log("------- CONVERT VOLUME ROOT -------");
                // The entire component is unique, so just convert it
                succeeded = ConvertVolumeComponent(oldVolume, errorString);
            }
        }

        private void ConvertLayer(BIRPRendering.PostProcessLayer oldLayer, ref bool succeeded,
            StringBuilder errorString)
        {
            if (!succeeded)
            {
                return;
            }

            if (!oldLayer)
            {
                // TODO: unless there's good way to tell the if the object is just missing because it was already
                //       converted as part of an earlier conversion object, then these two lines should be commented
                //       out or removed.  It should still return though.
                // succeeded = false;
                // errorString.AppendLine("PPv2 PostProcessLayer failed to be converted because the original asset reference was lost during conversion.");
                return;
            }

            if (PrefabUtility.IsPartOfPrefabInstance(oldLayer) &&
                !PrefabUtility.IsAddedComponentOverride(oldLayer))
            {
                Debug.Log("------- CONVERT LAYER INSTANCE -------");
                // This is a property override on an instance of the component,
                // so override the component instance with the modifications.
                succeeded = ConvertLayerInstance(oldLayer, errorString);
            }
            else
            {
                Debug.Log("------- CONVERT LAYER ROOT -------");
                // The entire component is unique, so just convert it
                succeeded = ConvertLayerComponent(oldLayer, errorString);
            }
        }

        private void ConvertProfile(BIRPRendering.PostProcessProfile oldProfile, ref bool succeeded,
            StringBuilder errorString)
        {
            if (!succeeded)
            {
                return;
            }

            if (!oldProfile)
            {
                errorString.AppendLine("PPv2 PostProcessProfile failed to be converted because the original asset reference was lost during conversion.");
                return;
            }

            ConvertVolumeProfileAsset(oldProfile, errorString, ref succeeded);

            // TODO:
            // - Perhaps old Profiles should only be deleted if they actually no longer have references,
            // just in case some some Volume conversions are skipped and still need references for future conversion.
            // - Alternatively, leave deletion of Profiles entirely to the user. (prefer this?)
        }
#endregion Conversion_Entry_Points

        private bool ConvertVolumeComponent(BIRPRendering.PostProcessVolume oldVolume, StringBuilder errorString)
        {
            // Don't convert if it appears to already have been converted.
            if (oldVolume.GetComponent<Volume>()) return true;

            var gameObject = oldVolume.gameObject;
            var newVolume = gameObject.AddComponent<Volume>();

            newVolume.priority = oldVolume.priority;
            newVolume.weight = oldVolume.weight;
            newVolume.blendDistance = oldVolume.blendDistance;
            newVolume.isGlobal = oldVolume.isGlobal;
            newVolume.enabled = oldVolume.enabled;

            var success = true;
            newVolume.sharedProfile = ConvertVolumeProfileAsset(oldVolume.sharedProfile, errorString, ref success);

            // TODO:
            // Object.DestroyImmediate(oldVolume, allowDestroyingAssets: true);
            EditorUtility.SetDirty(gameObject);

            return success;
        }

        private bool ConvertVolumeInstance(BIRPRendering.PostProcessVolume oldVolume, StringBuilder errorString)
        {
            // First get a reference to the local instance of the converted component
            // which may require immediately converting it at its origin location first.
            var newVolumeInstance = oldVolume.GetComponent<Volume>();
            if (!newVolumeInstance)
            {
                var oldVolumeOrigin = PrefabUtility.GetCorrespondingObjectFromSource(oldVolume);

                if (!ConvertVolumeComponent(oldVolumeOrigin, errorString))
                {
                    return false;
                }
                // TODO: do we need to save this change immediately (and refresh the database?) to get access to the instance?
                newVolumeInstance = oldVolume.GetComponent<Volume>();

                if (!newVolumeInstance)
                {
                    errorString.AppendLine("PPv2 PostProcessVolume failed to be converted because the instance object did not inherit the converted Prefab source.");
                    return false;
                }
            }

            bool success = true;
            var oldModifications = PrefabUtility.GetPropertyModifications(oldVolume);
            foreach (var oldModification in oldModifications)
            {
                if (oldModification.target is BIRPRendering.PostProcessVolume)
                {
                    // TODO: Remove this
                    Debug.Log("--- Object Modification:" +
                              $"\n{oldModification.target}" +
                              $"\n{oldModification.value}" +
                              $"\n{oldModification.objectReference}" +
                              $"\n{oldModification.propertyPath}");

                    if (oldModification.propertyPath.EndsWith("priority", StringComparison.InvariantCultureIgnoreCase))
                        newVolumeInstance.priority = oldVolume.priority;
                    else if (oldModification.propertyPath.EndsWith("weight", StringComparison.InvariantCultureIgnoreCase))
                        newVolumeInstance.weight = oldVolume.weight;
                    else if (oldModification.propertyPath.EndsWith("blendDistance", StringComparison.InvariantCultureIgnoreCase))
                        newVolumeInstance.blendDistance = oldVolume.blendDistance;
                    else if (oldModification.propertyPath.EndsWith("isGlobal", StringComparison.InvariantCultureIgnoreCase))
                        newVolumeInstance.isGlobal = oldVolume.isGlobal;
                    else if (oldModification.propertyPath.EndsWith("enabled", StringComparison.InvariantCultureIgnoreCase))
                        newVolumeInstance.enabled = oldVolume.enabled;
                    else if (oldModification.propertyPath.EndsWith("sharedProfile", StringComparison.InvariantCultureIgnoreCase))
                        newVolumeInstance.sharedProfile = ConvertVolumeProfileAsset(oldVolume.sharedProfile, errorString, ref success);

                    EditorUtility.SetDirty(newVolumeInstance);
                }
            }

            return success;
        }

        private bool ConvertLayerComponent(BIRPRendering.PostProcessLayer oldLayer, StringBuilder errorString)
        {
            Debug.Log("--- 011 ---");
            var siblingCamera = oldLayer.GetComponent<Camera>().GetUniversalAdditionalCameraData();

            // PostProcessLayer requires a sibling Camera component, but
            // we check it here just in case something weird went happened.
            if (!siblingCamera)
            {
                Debug.Log("--- 012 ---");
                errorString.AppendLine("PPv2 PostProcessLayer failed to be converted because the instance object was missing a required sibling Camera component.");
                return false;
            }

            Debug.Log("--- 013 ---", siblingCamera);
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

            // TODO:
            // Object.DestroyImmediate(oldLayer, allowDestroyingAssets: true);
            EditorUtility.SetDirty(siblingCamera.gameObject);

            Debug.Log("--- 014 ---");
            return true;
        }

        private bool ConvertLayerInstance(BIRPRendering.PostProcessLayer oldLayer, StringBuilder errorString)
        {
            Debug.Log("--- 001 ---");
            // First get a reference to the local instance of the camera (which is required by PostProcessingLayer)
            var siblingCamera = oldLayer.GetComponent<Camera>().GetUniversalAdditionalCameraData();
            if (!siblingCamera)
            {
                Debug.Log("--- 002 ---");
                errorString.AppendLine("PPv2 PostProcessLayer failed to be converted because the instance object was missing a required sibling Camera component.");
                return false;
            }

            siblingCamera.renderPostProcessing = true;

            Debug.Log("--- 003 ---");
            var oldModifications = PrefabUtility.GetPropertyModifications(oldLayer);
            foreach (var oldModification in oldModifications)
            {
                Debug.Log("--- 004 ---");

                // TODO: Remove this
                Debug.Log("--- PostProcessLayer Modification:" +
                          $"\n{oldModification.target}" +
                          $"\n{oldModification.value}" +
                          $"\n{oldModification.objectReference}" +
                          $"\n{oldModification.propertyPath}");

                if (oldModification.target is BIRPRendering.PostProcessLayer)
                {
                    Debug.Log("--- 005 ---");
                    if (oldModification.propertyPath.EndsWith("volumeLayer", StringComparison.InvariantCultureIgnoreCase))
                        siblingCamera.volumeLayerMask = oldLayer.volumeLayer;
                    else if (oldModification.propertyPath.EndsWith("volumeTrigger", StringComparison.InvariantCultureIgnoreCase))
                        siblingCamera.volumeTrigger = oldLayer.volumeTrigger;
                    else if (oldModification.propertyPath.EndsWith("stopNaNPropagation", StringComparison.InvariantCultureIgnoreCase))
                        siblingCamera.stopNaN = oldLayer.stopNaNPropagation;
                    else if (oldModification.propertyPath.EndsWith("quality", StringComparison.InvariantCultureIgnoreCase))
                        siblingCamera.antialiasingQuality = (URPRendering.AntialiasingQuality)oldLayer.subpixelMorphologicalAntialiasing.quality;
                    else if (oldModification.propertyPath.EndsWith("antialiasingMode", StringComparison.InvariantCultureIgnoreCase))
                    {
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

                    EditorUtility.SetDirty(siblingCamera);
                }
            }

            return true;
        }

        private VolumeProfile ConvertVolumeProfileAsset(BIRPRendering.PostProcessProfile oldProfile, StringBuilder errorString, ref bool success)
        {
            // Don't convert if it appears to already have been converted.
            if (!oldProfile) return null;

            var oldPath = AssetDatabase.GetAssetPath(oldProfile);
            var oldDirectory = Path.GetDirectoryName(oldPath);
            var oldName = Path.GetFileNameWithoutExtension(oldPath);
            var newPath = Path.Combine(oldDirectory, $"{oldName}(URP).asset");
            if (File.Exists(newPath))
            {
                return AssetDatabase.LoadAssetAtPath<VolumeProfile>(newPath);
            }

            var newProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            try
            {
                AssetDatabase.CreateAsset(newProfile, newPath);
            }
            catch (Exception e)
            {
                errorString.AppendLine($"PPv2 PostProcessLayer failed to be converted with exception:\n{e}");
                success = false;

                if (!newProfile) return null;
            }

            foreach (var oldSettings in oldProfile.settings)
            {
                foreach (var effectConverter in effectConverters)
                {
                    effectConverter.AddConvertedProfileSettingsToProfile(oldSettings, newProfile);
                }
            }

            EditorUtility.SetDirty(newProfile);

            return newProfile;
        }

        public IEnumerable<PostProcessEffectSettingsConverter> GetAllBIRPConverters()
        {
            var baseType = typeof(PostProcessEffectSettingsConverter);

            var assembly = Assembly.GetAssembly(baseType);
            var derivedTypes = assembly
                .GetTypes()
                .Where(t =>
                    t.BaseType != null &&
                    t.BaseType == baseType)
                .Select(t => ScriptableObject.CreateInstance(t) as PostProcessEffectSettingsConverter);

            return derivedTypes;
        }
    }
}
#endif
