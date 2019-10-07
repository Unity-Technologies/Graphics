using System;

namespace UnityEditor.ShaderGraph.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    class JsonUpgradeAttribute : Attribute
    {
        public JsonUpgradeAttribute(string name)
        {
            this.name = name;
        }

        public JsonUpgradeAttribute(string name, Type converterType)
        {
            this.name = name;
            this.converterType = converterType;
        }

        public JsonUpgradeAttribute(string name, Type converterType, params object[] converterParams)
        {
            this.name = name;
            this.converterType = converterType;
            this.converterParams = converterParams;
        }

        public string name { get; }
        public Type converterType { get; }
        public object[] converterParams { get; }
    }
}
