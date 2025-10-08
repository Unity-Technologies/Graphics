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

    [Serializable]
    public class UpscalerOptions : ScriptableObject
    {
        public string UpscalerName
        {
            get => m_UpscalerName;
            set => m_UpscalerName = value;
        }

        public UpsamplerScheduleType InjectionPoint
        {
            get => m_InjectionPoint;
            set => m_InjectionPoint = value;
        }

        [SerializeField, HideInInspector]
        private string m_UpscalerName = "";

        [SerializeField, HideInInspector] // hide in inspector for URP, HDRP manually renders it
        private UpsamplerScheduleType m_InjectionPoint = UpsamplerScheduleType.BeforePost;

#if UNITY_EDITOR
        // The core method to ensure options exist and are linked.
        // It operates on a SerializedProperty representing the list.
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

            // remove null entries
            for (int i = optionsListProp.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty elementProp = optionsListProp.GetArrayElementAtIndex(i);
                if (elementProp.objectReferenceValue == null)
                {
                    optionsListProp.DeleteArrayElementAtIndex(i);
                    propertyModified = true;
                    Debug.LogWarning($"[RP Asset] Removed null upscaler option from asset '{parentRPAsset.name}'.");
                }
            }

            // default-initialize registered upscaler options if they're not found within serialized asset
            foreach (var kvp in UpscalerRegistry.s_RegisteredUpscalers)
            {
                Type upscalerType = kvp.Key;
                Type? optionsType = kvp.Value.OptionsType;

                if (optionsType == null)
                    continue;

                string upscalerName = kvp.Value.ID;

                bool foundExisting = false;
                for (int i = 0; i < optionsListProp.arraySize; i++)
                {
                    SerializedProperty elementProp = optionsListProp.GetArrayElementAtIndex(i);
                    UpscalerOptions? existingOption = elementProp.objectReferenceValue as UpscalerOptions;

                    if (existingOption != null && existingOption.GetType() == optionsType /*&& existingOption.UpscalerName == upscalerName*/)
                    {
                        foundExisting = true;
                        break;
                    }
                }

                if (!foundExisting)
                {
                    UpscalerOptions newOption = (UpscalerOptions)ScriptableObject.CreateInstance(optionsType);
                    newOption.hideFlags = HideFlags.HideInHierarchy;
                    newOption.UpscalerName = upscalerName;

                    AssetDatabase.AddObjectToAsset(newOption, parentRPAsset);

                    optionsListProp.arraySize++;
                    optionsListProp.GetArrayElementAtIndex(optionsListProp.arraySize - 1).objectReferenceValue = newOption;

                    propertyModified = true;
                    Debug.Log($"[RP Asset] Auto-populated missing upscaler option on asset '{parentRPAsset.name}': {optionsType.Name} for ID: {upscalerName}");
                }
            }
            return propertyModified;
        }
#endif
    }

#nullable disable
}
#endif // ENABLE_UPSCALER_FRAMEWORK
