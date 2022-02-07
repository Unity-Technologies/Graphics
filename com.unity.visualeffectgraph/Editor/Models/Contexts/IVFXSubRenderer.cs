using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    interface IVFXSubRenderer
    {
        bool hasShadowCasting { get; }
        bool hasMotionVector { get; }
        // TODO Add other per output rendering settings here
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

        //The status on needs sync relies on empty condition of the propertyMap.
        //Example: In case of URP, we are always expecting QueueOffset/QueueControl in every material.
        public bool NeedsSync() => m_PropertyMap.Count == 0;

        public void SyncFromMaterial(Material material)
        {
            m_PropertyMap.Clear();

            var matProperties = ShaderUtil.GetMaterialProperties(new UnityEngine.Object[] { material });

            foreach (var p in matProperties)
            {
                if (p.type != MaterialProperty.PropType.Float || // Only float properties
                    (p.flags & (MaterialProperty.PropFlags.HideInInspector | MaterialProperty.PropFlags.PerRendererData)) != MaterialProperty.PropFlags.HideInInspector) // Only properties hidden in inspector that are not per renderer
                    continue;

                m_PropertyMap.Add(p.name, p.floatValue);
            }
        }

        public void ApplyToMaterial(Material material)
        {
            foreach (var kvp in m_PropertyMap)
                material.SetFloat(kvp.Key, kvp.Value);
        }

        // Mimic some functionality of the Material API to check properties.
        public bool HasProperty(string name) => m_PropertyMap.ContainsKey(name);

        public float GetFloat(string name)
        {
            if (!m_PropertyMap.ContainsKey(name))
                return -1;

            return m_PropertyMap[name];
        }

        // Helper to serialize the property map dictionary.
        public void OnBeforeSerialize() => StoreProperties();
        public void OnAfterDeserialize() => LoadProperties();

        [SerializeField]
        private List<string> m_PropertyNames = new List<string>();

        [SerializeField]
        private List<float> m_PropertyValues = new List<float>();

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
