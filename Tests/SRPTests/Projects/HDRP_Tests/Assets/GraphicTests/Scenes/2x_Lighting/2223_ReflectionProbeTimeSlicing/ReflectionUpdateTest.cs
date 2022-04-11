using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class ReflectionUpdateTest : MonoBehaviour
{
    private const int MATERIAL_COUNT = 7;
    private int m_Counter = 0;

    public Material[] materials = new Material[MATERIAL_COUNT];
    public HDProbe probe;

    public void StartTest()
    {
        m_Counter = MATERIAL_COUNT - 1;
        probe.RequestRenderNextUpdate();
    }

    void Update()
    {
        m_Counter = (m_Counter + 1) % MATERIAL_COUNT;
        foreach (Transform child in transform)
        {
            child.gameObject.TryGetComponent<MeshRenderer>(out MeshRenderer meshRenderer);
            if (meshRenderer != null)
                meshRenderer.material = materials[m_Counter];
        }
    }
}
