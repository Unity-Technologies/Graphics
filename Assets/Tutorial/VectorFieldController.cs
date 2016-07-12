using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VectorFieldController : MonoBehaviour {

    public List<VFXComponent> m_Components;
    public List<Texture3D> m_VectorFields;

    public const float ANGLE_SPEED = 30.0f;

    private class InstanceData
    {
        public VFXComponent m_Component;
        public int m_CurrentVFIndex;
        public float m_NextSwitchTime;
        public float m_StartAngle;
    }

    private List<InstanceData> m_InstanceData;

	// Use this for initialization
	void Start () {
        m_InstanceData = new List<InstanceData>();
        foreach (var comp in m_Components)
        {
            InstanceData data = new InstanceData();
            data.m_Component = comp;
            data.m_CurrentVFIndex = -1;
            data.m_StartAngle = Random.Range(0.0f, 360.0f);
            SwitchInstanceVF(data);
            m_InstanceData.Add(data);
        }
	}
	
	// Update is called once per frame
	void Update () {
        foreach (var data in m_InstanceData)
            UpdateInstance(data);
	}

    private void UpdateInstance(InstanceData data)
    {
        if (data.m_NextSwitchTime <= Time.time)
            SwitchInstanceVF(data);

        float currentAngle = (data.m_StartAngle + Time.time * ANGLE_SPEED) % 360.0f;
        // Set VFX parameter via script
        data.m_Component.SetFloat("VFAngle", currentAngle);
    }

    private void SwitchInstanceVF(InstanceData data)
    {
        int newIndex = data.m_CurrentVFIndex;
        while (newIndex == data.m_CurrentVFIndex)
            newIndex = Random.Range(0, m_VectorFields.Count);
        
        data.m_CurrentVFIndex = newIndex;
        data.m_NextSwitchTime = Time.time + Random.Range(1.0f, 5.0f);

        // Set VFX parameter via script
        data.m_Component.SetTexture3D("VFTexture", m_VectorFields[newIndex]);
        // Send VFX event via script
        data.m_Component.SendEvent("Burst");
    }
}
