using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimateEmissive : MonoBehaviour
{
    public GameObject m_CurrentGameObject = null;

    public float m_Period = 3.0f;
    public Color m_Color0;
    public Color m_Color1;

    private float m_Timer = 0.0f;
    private int m_Index = 0;


    // Use this for initialization
    void Start()
    {
        m_Timer = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (m_CurrentGameObject != null)
        {
            m_Timer += Time.deltaTime;
            if (m_Timer > m_Period)
            {
                m_Timer = 0.0f;

                Renderer renderer = m_CurrentGameObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color color = m_Index == 0 ? m_Color0 : m_Color1;
                    float intensity = 1.0f;
                    if (renderer.material != null)
                    {
                        if (renderer.material.HasProperty("_EmissionColor"))
                        {
                            renderer.material.SetColor("_EmissionColor", color);
                        }
                        else
                        {
                            renderer.material.SetColor("_EmissiveColor", color);
                            intensity = renderer.material.GetFloat("_EmissiveIntensity");
                        }
                    }

                    DynamicGI.SetEmissive(renderer, color * intensity);
                    m_Index = (m_Index + 1) % 2;
                }
            }
        }
    }
}
