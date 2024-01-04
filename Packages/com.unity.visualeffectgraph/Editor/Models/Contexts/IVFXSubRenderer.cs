using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.VFX
{
    interface IVFXSubRenderer
    {
        bool hasShadowCasting { get; }
        bool hasMotionVector { get; }
        // TODO Add other per output rendering settings here

        bool isRayTraced { get; }
        int vfxSystemSortPriority { get; set; }

        // Allow to setup material generated during import
        void SetupMaterial(Material material);
    }

    [Serializable]
    class VFXMaterialSerializedSettings : ISerializationCallbackReceiver
    {
        private Dictionary<string, float> m_PropertyMap = new Dictionary<string, float>();

        public static VFXMaterialSerializedSettings CreateFromMaterial(Material material)
        {
            VFXMaterialSerializedSettings settings = new VFXMaterialSerializedSettings();
            settings.SyncFromMaterial(material);
            return settings;
        }

        public void UpgradeToMaterialWorkflowVersion(Material referenceMaterial)
        {
            var serializedReferenceMaterial = new SerializedObject(referenceMaterial);
            var referenceProperties = new Dictionary<string, float>();
            var propertyBase = serializedReferenceMaterial.FindProperty("m_SavedProperties");
            propertyBase = propertyBase.FindPropertyRelative("m_Floats");
            for (int index = 0; index < propertyBase.arraySize; ++index)
            {
                var currentElement = propertyBase.GetArrayElementAtIndex(index);
                var name = currentElement.FindPropertyRelative("first").stringValue;
                var floatValue = currentElement.FindPropertyRelative("second").floatValue;
                referenceProperties.Add(name, floatValue);
            }

            var toRemove = new List<string>();
            foreach (var entry in m_PropertyMap)
            {
                //If the reference material have the same value than the serialized setting,
                //Admit the value has never been modified and shouldn't be considered as override.
                //Before MaterialVariant, every available properties were serialized
                //N.B.: if entry.Key isn't available in reference material, it can be a setting from another unavailable SRP
                if (referenceProperties.TryGetValue(entry.Key, out var referenceValue)
                    && referenceValue == entry.Value)
                {
                    toRemove.Add(entry.Key);
                }
            }
            foreach (var entry in toRemove)
                m_PropertyMap.Remove(entry);
        }

        public void SyncFromMaterial(Material material)
        {
            m_PropertyMap.Clear();

            var serializedMaterial = new SerializedObject(material);
            var propertyBase = serializedMaterial.FindProperty("m_SavedProperties");
            propertyBase = propertyBase.FindPropertyRelative("m_Floats");
            for (int index = 0; index < propertyBase.arraySize; ++index)
            {
                var currentElement = propertyBase.GetArrayElementAtIndex(index);
                var name = currentElement.FindPropertyRelative("first").stringValue;
                var floatValue = currentElement.FindPropertyRelative("second").floatValue;

                //material.IsPropertyOverriden doesn't need to be called, only serialized property are overridden in variant
                //if (!material.IsPropertyOverriden(name)) throw new InvalidOperationException();
                //Some not registered properties can be stored in material which correspond to another SRP (!= GetMaterialProperties)
                m_PropertyMap.Add(name, floatValue);
            }

            renderQueue = material.renderQueue;
        }

        public void ApplyToMaterial(Material material)
        {
            //Directly modify serialized properties
            //Using material.SetFloat would automatically skip not existing properties (e.g.: input from another SRP)
            //We are fitting to the default material behavior here
            var serializedMaterial = new SerializedObject(material);
            var propertyBase = serializedMaterial.FindProperty("m_SavedProperties");
            propertyBase = propertyBase.FindPropertyRelative("m_Floats");
            propertyBase.arraySize = m_PropertyMap.Count;

            int index = 0;
            foreach (var kvp in m_PropertyMap)
            {
                var currentEntry = propertyBase.GetArrayElementAtIndex(index++);
                currentEntry.FindPropertyRelative("first").stringValue = kvp.Key;
                currentEntry.FindPropertyRelative("second").floatValue = kvp.Value;
            }

            serializedMaterial.ApplyModifiedProperties();
            //N.B.: We can't used serialized properties here because renderQueue changes aren't automatically mark this entry as override
            material.renderQueue = renderQueue;
        }

        public bool TryGetFloat(string name, Material fallback, out float value)
        {
            if (m_PropertyMap.TryGetValue(name, out var readValue))
            {
                value = readValue;
                return true;
            }

            //With Material Variant workflow, the actual settings could be in parent material but not saved in properties
            if (fallback != null && fallback.HasFloat(name))
            {
                value = fallback.GetFloat(name);
                return true;
            }

            value = -1;
            return false;
        }

        // Helper to serialize the property map dictionary.
        public void OnBeforeSerialize() => StoreProperties();
        public void OnAfterDeserialize() => LoadProperties();

        [SerializeField]
        private List<string> m_PropertyNames = new List<string>();

        [SerializeField]
        private List<float> m_PropertyValues = new List<float>();

        [SerializeField] private int renderQueue = -1;

        void StoreProperties()
        {
            m_PropertyNames.Clear();
            m_PropertyValues.Clear();
            foreach (var kvp in m_PropertyMap)
            {
                m_PropertyNames.Add(kvp.Key);
                m_PropertyValues.Add(kvp.Value);
            }
        }

        void LoadProperties()
        {
            m_PropertyMap = new Dictionary<string, float>();
            for (int i = 0; i != Math.Min(m_PropertyNames.Count, m_PropertyValues.Count); i++)
                m_PropertyMap.Add(m_PropertyNames[i], m_PropertyValues[i]);
        }
    }
}
