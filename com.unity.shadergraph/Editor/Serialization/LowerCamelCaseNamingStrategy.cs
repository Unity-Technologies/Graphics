using System;
using Newtonsoft.Json.Serialization;

namespace UnityEditor.ShaderGraph.SerializationDemo
{
    class LowerCamelCaseNamingStrategy : NamingStrategy
    {
        protected override string ResolvePropertyName(string name)
        {
            if (!string.IsNullOrEmpty(name) && char.IsUpper(name[0]))
            {
                return char.ToUpper(name[0]) + name.Substring(1);
            }

            return name;
        }
    }
}
