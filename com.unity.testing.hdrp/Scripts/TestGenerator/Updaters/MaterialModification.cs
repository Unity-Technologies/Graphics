using System;

namespace UnityEngine.Experimental.Rendering.HDPipelineTest.TestGenerator
{
    // Prototype
    [Serializable]
    public struct MaterialModification
    {
        public string name;
        public MaterialModificationKind kind;
        public int intValue;
        public bool boolValue;
        public float floatValue;
        public Texture textureValue;

        public void ApplyTo(Material material)
        {
            switch (kind)
            {
                case MaterialModificationKind.Bool: material.SetInt(name, boolValue ? 1 : 0); break;
                case MaterialModificationKind.Int: material.SetInt(name, intValue); break;
                case MaterialModificationKind.Float: material.SetFloat(name, floatValue); break;
                case MaterialModificationKind.Texture: material.SetTexture(name, textureValue); break;
            }
        }
    }
}
