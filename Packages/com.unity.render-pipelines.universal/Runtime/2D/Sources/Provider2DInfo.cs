using System;
using UnityEngine;

[Serializable]
internal struct Provider2DInfo
{
    public string m_TypeName;
    public Component m_Component;
    public bool m_UsesComponent;   // if the component is deleted this is invalid info

    public Provider2DInfo(Type type, Component component)
    {
        m_TypeName = type.Name;
        m_UsesComponent = component != null;
        m_Component = component;
    }
}
