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
        int sortPriority { get; set; }

        // Allow to setup material generated during import
        void SetupMaterial(Material material);
    }

    [Serializable]
    class VFXMaterialSerializedSettings
    {
        [Serializable]
        private struct FloatProperty
        {
            public string name;
            public float value;
        }

        [SerializeField]
        private List<FloatProperty> properties = new List<FloatProperty>();

        public static VFXMaterialSerializedSettings CreateFromMaterial(Material material)
        {
            VFXMaterialSerializedSettings settings = new VFXMaterialSerializedSettings();
            settings.SyncFromMaterial(material);
            return settings;
        }

        public void SyncFromMaterial(Material material)
        {
            properties.Clear();

            var matProperties = ShaderUtil.GetMaterialProperties(new UnityEngine.Object[] { material });

            foreach (var p in matProperties)
            {
                // TODO we probably want to also filter out potential properties coming from compilation? (No compilation output data should be serialized)
                if (p.type != MaterialProperty.PropType.Float)
                    continue;

                properties.Add(new FloatProperty()
                {
                    name = p.name,
                    value = p.floatValue
                });
            }
        }

        public void ApplyToMaterial(Material material)
        {
            foreach (var p in properties)
                material.SetFloat(p.name, p.value);
        }
    }
}
