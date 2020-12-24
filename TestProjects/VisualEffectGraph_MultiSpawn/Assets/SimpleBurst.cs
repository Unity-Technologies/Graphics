using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class SimpleBurst : MonoBehaviour
{
    static readonly int s_DebugPositionID = Shader.PropertyToID("debugPosition");
    static readonly int s_DebugRadiusID = Shader.PropertyToID("debugRadius");
    static readonly int s_DebugColorID = Shader.PropertyToID("debugColor");

    static readonly int s_PositionID = Shader.PropertyToID("position");
    static readonly int s_ColorID = Shader.PropertyToID("color");

    public int m_Seed;
    public float m_Period = 1.0f;

    System.Random m_Random;
    float m_Wait;
    VisualEffect m_VisualEffect;
    VFXEventAttribute m_CachedAttribute;
    public uint m_currentSpawnIndex;

    void Start()
    {
        m_VisualEffect = gameObject.GetComponent<VisualEffect>();
        m_CachedAttribute = m_VisualEffect.CreateVFXEventAttribute();
        m_Random = new System.Random(m_Seed);
        m_Wait = 0.0f;
    }

    void Update()
    {
        m_Wait -= Time.deltaTime;
        if (m_Wait < 0.0f)
        {
            m_Wait = m_Period;
            float radius = Mathf.Lerp(0.25f, 0.4f, (float)m_Random.NextDouble());
            float x = Mathf.Lerp(-1.0f, 1.0f, (float)m_Random.NextDouble());
            float y = Mathf.Lerp(-1.0f, 1.0f, (float)m_Random.NextDouble());
            var center = new Vector3(x, y, 0.0f);
            float hue = (float)m_Random.NextDouble();
            var colorRGB = Color.HSVToRGB(hue, 1.0f, 1.0f);
            var color = new Vector3(colorRGB.r, colorRGB.g, colorRGB.b);
            m_VisualEffect.SetVector3(s_DebugColorID, color);
            m_VisualEffect.SetVector3(s_DebugPositionID, center);
            m_VisualEffect.SetFloat(s_DebugRadiusID, radius);

            m_CachedAttribute.SetVector3(s_ColorID, color);

            int count = m_Random.Next(24, 32);
            for (int i = 0; i < count; ++i)
            {
                var eventName = "fire";
                if (m_currentSpawnIndex != 0)
                    eventName = string.Format("{0}_{1}", eventName, m_currentSpawnIndex);

                float r = radius * 0.5f * Mathf.Sqrt((float)m_Random.NextDouble());
                float theta = (float)m_Random.NextDouble() * Mathf.PI * 2.0f;

                var randPosition = center;
                randPosition.x += r * Mathf.Cos(theta);
                randPosition.y += r * Mathf.Sin(theta);

                m_CachedAttribute.SetVector3(s_PositionID, randPosition);
                m_VisualEffect.SendEvent(eventName, m_CachedAttribute);

                m_currentSpawnIndex++;
                if (m_currentSpawnIndex > 32u)
                    m_currentSpawnIndex = 0u;
            }
        }
    }
}
