using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.VFX;

namespace Unity.Testing.VisualEffectGraph
{
    public class CreateAndBindGraphicsBuffer : MonoBehaviour
    {
        [VFXType]
        public struct Rectangle
        {
            public Vector2 size;
            public Vector3 color;
        }

        [VFXType(VFXTypeAttribute.Flags.GraphicsBuffer)]
        struct CustomData
        {
            public Rectangle rectangle;
            public Vector3 position;
        }

        static readonly float maxWidth = 32;
        static readonly float maxHeight = 32;

        static IEnumerable<float> PrefixSum(IEnumerable<float> entries)
        {
            float sum = 0.0f;
            foreach (var entry in entries)
            {
                sum += entry;
                yield return sum;
            }
        }

        static readonly Tuple<Vector3, float>[] s_Colors = new Tuple<Vector3, float>[]
        {
        new Tuple<Vector3, float>(new Vector3(1,1,1), 4.0f),
        new Tuple<Vector3, float>(new Vector3(0,0,0), 1.0f),
        new Tuple<Vector3, float>(new Vector3(1,0,0), 1.0f),
        new Tuple<Vector3, float>(new Vector3(0,0,1), 1.0f),
        new Tuple<Vector3, float>(new Vector3(1,1,0), 1.0f),
        };

        //TODOPAUL DO A FUNCTION :-)
        static readonly float[] s_PrefixSum = PrefixSum(s_Colors.Select(o => o.Item2)).ToArray();
        static readonly float[] s_PrefixSumNormalized = s_PrefixSum.Select(o => o / s_PrefixSum.Last()).ToArray();
        static readonly Tuple<Vector3, float>[] s_ColorsPrefixSumNormalized = s_Colors.Zip(s_PrefixSumNormalized, (a, b) => new Tuple<Vector3, float>(a.Item1, b)).ToArray();

        private static void ProcessMondrian(List<CustomData> rectangles, System.Random rand)
        {
            for (int i = rectangles.Count - 1; i >= 0; i--)
            {
                var current = rectangles[i];
                if (current.rectangle.size.x <= maxWidth * 2.0f || current.rectangle.size.y <= maxHeight * 2.0f)
                    continue;

                rectangles.RemoveAt(i);

                CustomData a = current;
                CustomData b = current;
                float randValue = (float)rand.NextDouble();
                if (current.rectangle.size.x > current.rectangle.size.y)
                {
                    float oldWidth = current.rectangle.size.x;
                    float newWidth = Mathf.Lerp(maxWidth, current.rectangle.size.x - maxWidth, randValue);
                    a.rectangle.size.x = newWidth;
                    b.rectangle.size.x = oldWidth - newWidth;

                    a.position.x -= (oldWidth - a.rectangle.size.x) / 2.0f;
                    b.position.x += (oldWidth - b.rectangle.size.x) / 2.0f;
                }
                else
                {
                    float oldHeight = current.rectangle.size.y;
                    float newHeight = Mathf.Lerp(maxHeight, current.rectangle.size.y - maxHeight, randValue);
                    a.rectangle.size.y = newHeight;
                    b.rectangle.size.y = oldHeight - newHeight;

                    a.position.y -= (oldHeight - a.rectangle.size.y) / 2.0f;
                    b.position.y += (oldHeight - b.rectangle.size.y) / 2.0f;
                }

                randValue = (float)rand.NextDouble();
                var newColor = s_ColorsPrefixSumNormalized.Last().Item1;
                foreach (var color in s_ColorsPrefixSumNormalized)
                {
                    if (randValue < color.Item2)
                    {
                        newColor = color.Item1;
                        break;
                    }
                }
                b.rectangle.color = newColor;
                rectangles.Add(a);
                rectangles.Add(b);
            }
        }

        private static readonly int s_BufferID = Shader.PropertyToID("buffer");
        private GraphicsBuffer m_buffer;

        private static readonly float s_WaitTime = 0.2f;
        private static readonly uint s_MaxIteration = 12;

        private System.Random m_Random = new System.Random(1254);
        private uint m_MaxIteration = s_MaxIteration;
        private float m_Wait = s_WaitTime;

        private List<CustomData> m_Data;

        void Start()
        {
            if (m_buffer != null)
            {
                m_buffer.Release();
            }

            m_buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 4096, Marshal.SizeOf(typeof(CustomData)));
            m_buffer.SetData(new CustomData[4096]); //Why we need to clear ?

            m_Data = new List<CustomData>()
        {
            new CustomData() { position = new Vector3(0, 0, 0), rectangle = new Rectangle() { color = new Vector3(1, 1, 1), size = new Vector2(1024, 1024) }}
        };
            m_buffer.SetData(m_Data);

            var vfx = GetComponent<VisualEffect>();
            vfx.SetGraphicsBuffer(s_BufferID, m_buffer);
        }

        void Update()
        {
            m_Wait -= Time.deltaTime;
            if (m_Wait < 0.0f)
            {
                m_Wait = s_WaitTime;
                if (m_MaxIteration != 0)
                {
                    m_MaxIteration--;
                    ProcessMondrian(m_Data, m_Random);
                    m_buffer.SetData(m_Data);

                    if (m_MaxIteration == 0)
                        m_Wait *= 5.0f;

                }
                else
                {
                    m_MaxIteration = s_MaxIteration;
                    Start();
                }
            }
        }

        public void OnDisable()
        {
            if (m_buffer != null)
            {
                m_buffer.Release();
                m_buffer = null;
            }
        }
    }
}
