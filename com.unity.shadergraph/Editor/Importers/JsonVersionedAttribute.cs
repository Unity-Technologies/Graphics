using System;

namespace UnityEditor.Importers
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    class JsonVersionedAttribute : Attribute
    {
        readonly Type m_PreviousVersionType;

        public JsonVersionedAttribute(Type previousVersionType = null)
        {
            m_PreviousVersionType = previousVersionType;
        }

        public Type previousVersionType
        {
            get { return m_PreviousVersionType; }
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    class JsonVersionAttribute : Attribute { }

    interface IUpgradableTo<TTo>
    {
        TTo Upgrade();
    }
}
