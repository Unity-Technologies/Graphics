using System;

namespace UnityEngine.Experimental.Rendering.HDPipelineTest.TestGenerator
{
    // Prototype
    /// <summary>
    ///     Describe a modification to apply to a material.
    /// </summary>
    [Serializable]
    public struct MaterialModification
    {
        // Note: these fields should probably be private, with properties / smart function to access them.

        /// <summary>
        ///     The name of the material property to update.
        /// </summary>
        public string name;

        /// <summary>
        ///     The kind of the property to update.
        /// </summary>
        public MaterialModificationKind kind;

        public int intValue;
        public bool boolValue;
        public float floatValue;
        public Texture textureValue;

        /// <summary>
        ///     Apply the modification to a material.
        /// </summary>
        /// <param name="material">The material to update.</param>
        public void ApplyTo(Material material)
        {
            switch (kind)
            {
                case MaterialModificationKind.Bool:
                    material.SetInt(name, boolValue ? 1 : 0);
                    break;
                case MaterialModificationKind.Int:
                    material.SetInt(name, intValue);
                    break;
                case MaterialModificationKind.Float:
                    material.SetFloat(name, floatValue);
                    break;
                case MaterialModificationKind.Texture:
                    material.SetTexture(name, textureValue);
                    break;
            }
        }
    }
}
