using System;

namespace UnityEditor.ShaderGraph
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class LegacyAttribute : Attribute
    {
        public string fullName { get; private set; }

        public LegacyAttribute(string fullName)
        {
            this.fullName = fullName;
        }
    }
}
