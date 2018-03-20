using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

[ExecuteInEditMode, RequireComponent(typeof(VFXParameterBinder))]
public abstract class VFXBindingBase : MonoBehaviour
{
    protected VFXParameterBinder m_Binder;

    private void Awake()
    {
        m_Binder = GetComponent<VFXParameterBinder>();
    }

    protected virtual void OnEnable()
    {
        if (!m_Binder.m_Bindings.Contains(this))
            m_Binder.m_Bindings.Add(this);

        hideFlags = HideFlags.HideInInspector; // Hide when ready and confident enough to ship
    }

    protected virtual void OnDisable()
    {
        if (m_Binder.m_Bindings.Contains(this))
            m_Binder.m_Bindings.Remove(this);
    }

    public abstract void UpdateBinding(VisualEffect component);

    public override string ToString()
    {
        return GetType().ToString();
    }
}
