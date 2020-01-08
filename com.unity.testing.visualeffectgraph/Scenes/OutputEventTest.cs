using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class OutputEventTest : MonoBehaviour
{
    static private int s_outputEventNameId = Shader.PropertyToID("Test_Output_Event");
    static private int s_positionNameId = Shader.PropertyToID("position");

    public GameObject m_ObjectReference;

    private VisualEffect m_vfx;
    private List<VFXEventAttribute> m_cachedVFXEventAttribute;
    private List<VFXEventAttribute> m_currentVFXEventAttribute;

    void Start()
    {
        m_vfx = GetComponent<VisualEffect>();

        if (m_vfx)
        {
            m_cachedVFXEventAttribute = new List<VFXEventAttribute>(3);
            m_currentVFXEventAttribute = new List<VFXEventAttribute>(3);
            for (int i = 0; i<3; ++i)
                m_cachedVFXEventAttribute.Add(m_vfx.CreateVFXEventAttribute());
        }
    }

    void Update()
    {
        if (m_vfx == null || m_ObjectReference == null)
            return;

        m_currentVFXEventAttribute.Clear();
        m_currentVFXEventAttribute.AddRange(m_cachedVFXEventAttribute);
        m_vfx.GetOutputEventAttribute(s_outputEventNameId, m_cachedVFXEventAttribute);

        foreach (var eventAttribute in m_currentVFXEventAttribute)
        {
            var newObject = GameObject.Instantiate(m_ObjectReference);
            newObject.GetComponent<Transform>().position = eventAttribute.GetVector3(s_positionNameId);
            newObject.SetActive(true);
        }
    }
}
