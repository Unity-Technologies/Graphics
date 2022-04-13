using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Unity.Testing.VisualEffectGraph
{
    public class DirectLinkMix : MonoBehaviour
    {
        static readonly uint s_CircleCount = 16u;

        class SourceData
        {
            public float radius;
            public float speed;
            public float rate;
            public Vector3 color;

            public float time;
            public float spawnCount;
        }

        List<SourceData> m_Sources = new List<SourceData>();
        VisualEffect m_VisualEffect;
        VFXEventAttribute m_CacheEventAttribute;

        void Start()
        {
            var rand = new System.Random(158);
            for (uint i = 0; i < s_CircleCount; ++i)
            {
                var source = new SourceData();
                source.time = (float)rand.NextDouble() * Mathf.PI * 2.0f;
                source.radius = Mathf.Lerp(0.2f, 0.9f, (float)rand.NextDouble());
                source.speed = Mathf.Lerp(0.5f, 2.0f, (float)rand.NextDouble());
                source.rate = Mathf.Lerp(8.0f, 32.0f, (float)rand.NextDouble());

                var color = Color.HSVToRGB((float)rand.NextDouble(), 1.0f, 1.0f);
                source.color = new Vector3(color.r, color.g, color.b);
                source.spawnCount = 0.0f;

                m_Sources.Add(source);
            }

            m_VisualEffect = gameObject.GetComponent<VisualEffect>();
            m_CacheEventAttribute = m_VisualEffect.CreateVFXEventAttribute();
        }

        static readonly int s_SpawnCountID = Shader.PropertyToID("spawnCount");
        static readonly int s_ColorID = Shader.PropertyToID("color");
        static readonly int s_PositionID = Shader.PropertyToID("position");
        static readonly int s_VelocityID = Shader.PropertyToID("velocity");
        static readonly int s_ManualId = Shader.PropertyToID("manual");

        void Update()
        {
            var dt = Time.deltaTime;
            foreach (var source in m_Sources)
            {
                source.time += dt * source.speed;
                source.spawnCount += dt * source.rate;
                if (source.spawnCount >= 1.0f)
                {
                    float spawnCount = Mathf.Floor(source.spawnCount);
                    source.spawnCount = Mathf.Repeat(source.spawnCount, 1.0f);

                    var position = new Vector3(Mathf.Sin(source.time), Mathf.Cos(source.time), 0.0f) * source.radius;
                    var direction = new Vector3(Mathf.Cos(source.time), -Mathf.Sin(source.time), 0.0f) * 0.1f;

                    m_CacheEventAttribute.SetVector3(s_ColorID, source.color);
                    m_CacheEventAttribute.SetVector3(s_PositionID, position);
                    m_CacheEventAttribute.SetVector3(s_VelocityID, direction);
                    m_CacheEventAttribute.SetFloat(s_SpawnCountID, spawnCount);

                    m_VisualEffect.SendEvent(s_ManualId, m_CacheEventAttribute);
                }
            }
        }
    }
}
