using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public struct PreviewProperty
    {
        public string name;
        public PropertyType propType;

        public Color colorValue;
        public Texture textureValue;
        public Cubemap cubemapValue;
        public Vector4 vector4Value;
        public float floatValue;
    }
}
