using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;


namespace Unity.Testing.VisualEffectGraph
{
    public class DirectLinkRLE : MonoBehaviour
    {
        public Texture2D m_SourceTexture;

        struct SpawnEvent
        {
            public Vector3 color;
            public float count;
        }
        SpawnEvent[] m_SpawnEvents;

        void Start()
        {
            var pixels = m_SourceTexture.GetPixels();
            var spawnEvents = new Stack<SpawnEvent>();
            foreach (var pixel in pixels)
            {
                var currentColor = new Vector3(pixel.r, pixel.g, pixel.b);
                if (spawnEvents.Count == 0 || currentColor != spawnEvents.Peek().color)
                {
                    spawnEvents.Push(new SpawnEvent()
                    {
                        color = currentColor,
                        count = 0
                    });
                }

                var entry = spawnEvents.Pop();
                entry.count += 1.0f;
                spawnEvents.Push(entry);
            }

            m_SpawnEvents = spawnEvents.ToArray();
        }

        static readonly float s_WaitTime = 0.5f;
        float m_WaitingTime = s_WaitTime;
        uint m_ReadCursor;

        static readonly int s_SpawnCountID = Shader.PropertyToID("spawnCount");
        static readonly int s_ColorID = Shader.PropertyToID("color");
        static readonly int s_FireID = Shader.PropertyToID("fire");

        void Update()
        {
            m_WaitingTime -= Time.deltaTime;
            if (m_WaitingTime < 0)
            {
                m_WaitingTime = s_WaitTime;

                var vfx = gameObject.GetComponent<VisualEffect>();
                var vfxEventAttribute = vfx.CreateVFXEventAttribute();
                int spawnEventsToSend = m_SpawnEvents.Length / 20;
                for (int i = 0; i < spawnEventsToSend; i++)
                {
                    var spawnEvent = m_SpawnEvents[m_ReadCursor];

                    vfxEventAttribute.SetFloat(s_SpawnCountID, spawnEvent.count);
                    vfxEventAttribute.SetVector3(s_ColorID, spawnEvent.color);
                    vfx.SendEvent(s_FireID, vfxEventAttribute);

                    m_ReadCursor++;
                    if (m_ReadCursor >= m_SpawnEvents.Length)
                        m_ReadCursor = 0u;
                }
            }
        }
    }
}
