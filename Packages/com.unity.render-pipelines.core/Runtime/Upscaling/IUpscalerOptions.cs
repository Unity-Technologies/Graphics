#if ENABLE_UPSCALER_FRAMEWORK
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine.Scripting;
using static UnityEngine.Rendering.DynamicResolutionHandler;

namespace UnityEngine.Rendering
{
#nullable enable

    /// <summary>
    /// Base ScriptableObject for holding configuration data for a specific upscaler.
    /// Concrete implementations (e.g., DLSSOptions, FSR2Options) should inherit from this.
    /// </summary>
    [Serializable]
    public class UpscalerOptions : ScriptableObject
    {
        /// <summary>
        /// The unique identifier or display name of the upscaler associated with these options.
        /// </summary>
        public string upscalerName
        {
            get => m_UpscalerName;
            set => m_UpscalerName = value;
        }

        /// <summary>
        /// Determines at which stage in the render pipeline the upscaling pass is injected.
        /// </summary>
        public UpsamplerScheduleType injectionPoint
        {
            get => m_InjectionPoint;
            set => m_InjectionPoint = value;
        }

        [SerializeField, HideInInspector]
        private string m_UpscalerName = "";

        [SerializeField, HideInInspector] // hide in inspector for URP, HDRP manually renders it
        private UpsamplerScheduleType m_InjectionPoint = UpsamplerScheduleType.BeforePost;

#if UNITY_EDITOR
        /// <summary>
        /// Validates and auto-populates the upscaler options list within a Render Pipeline Asset.
        /// It ensures that for every registered upscaler, a corresponding Options sub-asset exists.
        /// </summary>
        /// <param name="parentRPAsset">The Render Pipeline Asset that will contain these options as sub-assets.</param>
        /// <param name="optionsListProp">The SerializedProperty representing the list/array of UpscalerOptions.</param>
        /// <returns>True if the property was modified (cleaned up or populated), false otherwise.</returns>
        public static bool ValidateSerializedUpscalerOptionReferencesWithinRPAsset(ScriptableObject parentRPAsset, SerializedProperty optionsListProp)
        {
            if (parentRPAsset == null)
            {
                Debug.LogError("[Auto-Populate] Parent asset is null.");
                return false;
            }
            if (optionsListProp == null || !optionsListProp.isArray)
            {
                Debug.LogError($"[Auto-Populate] Provided SerializedProperty '{optionsListProp?.name ?? "null"}' is not a valid list for upscaler options.");
                return false;
            }

            bool propertyModified = false;

            // Track valid objects to identify orphans later
            HashSet<Object> validReferencedObjects = new HashSet<Object>();
            HashSet<Type> typesInList = new HashSet<Type>();

            // =================================================================================
            // PASS 1: Clean up the Serialized List (Nulls & Duplicate Types)
            // =================================================================================
            for (int i = optionsListProp.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty elementProp = optionsListProp.GetArrayElementAtIndex(i);
                Object objRef = elementProp.objectReferenceValue;

                // 1. Remove Nulls
                if (objRef == null)
                {
                    // TODO: find a way to properly remove abandoned upscaler options.
                    //       there's no good way to remove null options from the RPAsset with Delete/Destroy calls.
                    //       one alternative is to copy non-null entries into memory and rewrite the RPAsset.
                    //
                    // optionsListProp.DeleteArrayElementAtIndex(i);
                    // propertyModified = true;
                    // Debug.LogWarning($"[RP Asset] Removed null upscaler option from active list in '{parentRPAsset.name}'.");

                    continue;
                }

                // 2. Check for Duplicate Types within the active list
                // (e.g., The list somehow has two FSR2Options. We keep the first, remove the rest).
                Type objType = objRef.GetType();
                if (typesInList.Contains(objType))
                {
                    optionsListProp.DeleteArrayElementAtIndex(i);
                    propertyModified = true;
                    Debug.LogWarning($"[RP Asset] Removed duplicate active reference of type {objType.Name} from list in '{parentRPAsset.name}'.");
                    // We do NOT add to validReferencedObjects, so it will be caught in Pass 2 and destroyed.
                }
                else
                {
                    typesInList.Add(objType);
                    validReferencedObjects.Add(objRef);
                }
            }

            // =================================================================================
            // PASS 2: Ghost Hunting (Destroy orphaned Sub-Assets)
            // =================================================================================
            // Load ALL assets contained within the file
            string assetPath = AssetDatabase.GetAssetPath(parentRPAsset);
            Object[] allSubAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

            List<Object> assetsToDestroy = new List<Object>();

            foreach (Object subAsset in allSubAssets)
            {
                // We only care about UpscalerOptions. Ignore the main RPAsset or other embedded data.
                // We also check if it is HideInHierarchy/HideInInspector, which usually indicates a sub-asset.
                if (subAsset is UpscalerOptions)
                {
                    // If this asset exists in the file, but is NOT in our 'validReferencedObjects' set, it is an orphan.
                    if (!validReferencedObjects.Contains(subAsset))
                    {
                        assetsToDestroy.Add(subAsset);
                    }
                }
            }

            if (assetsToDestroy.Count > 0)
            {
                Debug.Log($"[RP Asset] Found {assetsToDestroy.Count} orphaned/duplicate sub-assets in '{parentRPAsset.name}'. Cleaning up...");

                foreach (Object orphan in assetsToDestroy)
                {
                    Debug.Log($"[RP Asset] Destroying orphan: {orphan.name} ({orphan.GetType().Name})");

                    // Crucial: Object.DestroyImmediate with 'true' allows destroying assets in the Editor.
                    // This removes the YAML block entirely.
                    Object.DestroyImmediate(orphan, true);
                }

                propertyModified = true;
            }

            // =================================================================================
            // PASS 3: Populate Missing Required Options
            // =================================================================================
            foreach (var kvp in UpscalerRegistry.s_RegisteredUpscalers)
            {
                Type upscalerType = kvp.Key;
                Type? optionsType = kvp.Value.OptionsType;

                if (optionsType == null)
                    continue;

                string upscalerName = kvp.Value.ID;

                // Check if we already have this type in our tracked types from Pass 1
                bool foundExisting = typesInList.Contains(optionsType);

                if (!foundExisting)
                {
                    UpscalerOptions newOption = (UpscalerOptions)ScriptableObject.CreateInstance(optionsType);
                    newOption.hideFlags = HideFlags.HideInHierarchy; // Standard for embedded sub-assets
                    newOption.name = optionsType.Name; // Give it a clean name in the file
                    newOption.upscalerName = upscalerName;

                    // Add the physical object to the main asset file
                    AssetDatabase.AddObjectToAsset(newOption, parentRPAsset);

                    // Add the reference to the list
                    optionsListProp.arraySize++;
                    optionsListProp.GetArrayElementAtIndex(optionsListProp.arraySize - 1).objectReferenceValue = newOption;

                    propertyModified = true;
                    Debug.Log($"[RP Asset] Created missing upscaler option on asset '{parentRPAsset.name}': {optionsType.Name} for ID: {upscalerName}");
                }
            }

            return propertyModified;
        }
#endif
    }

#nullable disable
}
#endif // ENABLE_UPSCALER_FRAMEWORK
