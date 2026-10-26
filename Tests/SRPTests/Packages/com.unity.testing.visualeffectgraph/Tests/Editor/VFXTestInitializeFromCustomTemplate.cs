using UnityEngine;

namespace UnityEditor.VFX.Test
{
    class VFXTestInitializeFromCustomTemplate : VFXBasicInitialize
    {
        // Serialized into the .vfx rather than held in a static: codeGeneratorTemplate is read while the effect
        // is imported, and that import can run on an asset import worker, which has none of our statics.
        [SerializeField]
        string m_TemplatePathWithoutExtension;

        public string templatePathWithoutExtension
        {
            get => m_TemplatePathWithoutExtension;
            set => m_TemplatePathWithoutExtension = value;
        }

        public override string codeGeneratorTemplate => m_TemplatePathWithoutExtension;
    }
}
