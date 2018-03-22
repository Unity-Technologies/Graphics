using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

[ExecuteInEditMode, RequireComponent(typeof(VFXParameterBinder))]
public abstract class VFXBindingBase : MonoBehaviour
{
    protected VFXParameterBinder binder;

    protected Dictionary<string, int> m_PropertyCache = new Dictionary<string, int>();

    public abstract bool IsValid(VisualEffect component);

    public int GetParameter(string name)
    {
        if (m_PropertyCache.ContainsKey(name))
            return m_PropertyCache[name];
        else
        {
            int id = Shader.PropertyToID(name);
            m_PropertyCache.Add(name, id);
            return id;
        }
    }

    private void Awake()
    {
        binder = GetComponent<VFXParameterBinder>();
    }

    protected virtual void OnEnable()
    {
        if (!binder.m_Bindings.Contains(this))
            binder.m_Bindings.Add(this);

        hideFlags = HideFlags.HideInInspector; // Comment to debug
    }

    protected virtual void OnDisable()
    {
        if (binder.m_Bindings.Contains(this))
            binder.m_Bindings.Remove(this);
    }

    public abstract void UpdateBinding(VisualEffect component);

    public override string ToString()
    {
        return GetType().ToString();
    }
}
