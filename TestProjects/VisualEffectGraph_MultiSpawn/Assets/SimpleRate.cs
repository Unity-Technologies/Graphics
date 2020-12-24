using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class SimpleRate : MonoBehaviour
{
    static readonly int s_DebugPositionID = Shader.PropertyToID("debugPosition");
    static readonly int s_DebugRadiusID = Shader.PropertyToID("debugRadius");
    static readonly int s_DebugColorID = Shader.PropertyToID("debugColor");

    static readonly int s_PositionID = Shader.PropertyToID("position");
    static readonly int s_ColorID = Shader.PropertyToID("color");

    static readonly int s_OnID = Shader.PropertyToID("on");
    static readonly int s_OffID = Shader.PropertyToID("off");


    public int m_Seed;
    public float m_Period = 1.0f;

    System.Random m_Random;
    float m_Wait;
    VisualEffect m_VisualEffect;
    VFXEventAttribute m_CachedAttribute;

    const uint m_NumberOfReplication = 64;
    Queue<uint> m_FreeReplication = new Queue<uint>();
    struct SpawnRecord
    {
        public Vector3 position;
        public uint index;
    }
    List<SpawnRecord> m_spawnRecords = new List<SpawnRecord>();

    void Start()
    {
        m_VisualEffect = gameObject.GetComponent<VisualEffect>();
        m_CachedAttribute = m_VisualEffect.CreateVFXEventAttribute();
        m_Random = new System.Random(m_Seed);
        m_Wait = 0.0f;

        float hue = (float)m_Random.NextDouble();
        var colorRGB = Color.HSVToRGB(hue, 1.0f, 1.0f);
        var color = new Vector3(colorRGB.r, colorRGB.g, colorRGB.b);
        m_CachedAttribute.SetVector3(s_ColorID, color);
        m_VisualEffect.SetVector3(s_DebugColorID, color);
        for (uint i = 0; i < m_NumberOfReplication; ++i)
            m_FreeReplication.Enqueue(i);
    }

    Vector3 m_PreviousPosition;
    float m_PreviousRadius = 0.35f;

    Vector3 m_NextPosition;
    float m_NextRadius = 0.35f;

    void Update()
    {
        var currentCenter = Vector3.Lerp(m_NextPosition, m_PreviousPosition, m_Wait / m_Period);
        var currentRadius = Mathf.Lerp(m_NextRadius, m_PreviousRadius, m_Wait / m_Period);

        m_VisualEffect.SetVector3(s_DebugPositionID, currentCenter);
        m_VisualEffect.SetFloat(s_DebugRadiusID, currentRadius);

        //Remove out of circle position (using backward loop, simplest)
        for (var i = m_spawnRecords.Count - 1; i >= 0; i--)
        {
            var record = m_spawnRecords[i];

            var d = record.position - currentCenter;
            if (Vector3.Dot(d, d) > currentRadius * currentRadius)
            {
                m_CachedAttribute.SetVector3(s_PositionID, record.position);
                m_VisualEffect.SendReplicatedEvent(s_OffID, m_CachedAttribute, record.index);

                m_FreeReplication.Enqueue(record.index);
                m_spawnRecords.RemoveAt(i);
            }
        }

        while (m_spawnRecords.Count < 24 && m_FreeReplication.Count != 0)
        {
            float r = currentRadius * Mathf.Sqrt((float)m_Random.NextDouble());
            float theta = (float)m_Random.NextDouble() * Mathf.PI * 2.0f;

            var randPosition = currentCenter;
            randPosition.x += r * Mathf.Cos(theta);
            randPosition.y += r * Mathf.Sin(theta);
            var newRecord = new SpawnRecord()
            {
                index = m_FreeReplication.Dequeue(),
                position = randPosition
            };

            m_CachedAttribute.SetVector3(s_PositionID, newRecord.position);
            m_VisualEffect.SendReplicatedEvent(s_OnID, m_CachedAttribute, newRecord.index);

            m_spawnRecords.Add(newRecord);
        }

        m_Wait -= Time.deltaTime;
        if (m_Wait < 0.0f)
        {
            m_Wait = m_Period;
            m_PreviousPosition = m_NextPosition;
            m_PreviousRadius = m_NextRadius;

            m_NextRadius = Mathf.Lerp(0.3f, 0.4f, (float)m_Random.NextDouble());
            float x = Mathf.Lerp(-1.0f, 1.0f, (float)m_Random.NextDouble());
            float y = Mathf.Lerp(-1.0f, 1.0f, (float)m_Random.NextDouble());
            m_NextPosition = new Vector3(x, y, 0.0f);
        }
    }
}
