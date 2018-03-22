using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

[RequireComponent(typeof(VisualEffect))]
[ExecuteInEditMode]
public class VFXParameterBinder : MonoBehaviour
{
    [SerializeField]
    protected bool m_ExecuteInEditor = true;
    public List<VFXBinderBase> m_Bindings = new List<VFXBinderBase>();
    [SerializeField]
    protected VisualEffect m_VisualEffect;

    private void OnEnable()
    {
        m_VisualEffect = GetComponent<VisualEffect>();
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        foreach (var binding in m_Bindings)
            UnityEditor.Undo.DestroyObjectImmediate(binding);
#endif
    }

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (!m_ExecuteInEditor && Application.isEditor && !Application.isPlaying) return;

        foreach (var binding in m_Bindings)
            if (binding.IsValid(m_VisualEffect)) binding.UpdateBinding(m_VisualEffect);
    }
}
