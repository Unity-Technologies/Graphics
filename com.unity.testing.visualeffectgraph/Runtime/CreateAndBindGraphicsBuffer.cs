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

        [VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
        struct CustomData
        {
            public Rectangle rectangle;
            public Vector3 position;
        }

/*
        //Some sample of invalid declaration
        //0.
        [VFXType(VFXTypeAttribute.Flags.GraphicsBuffer)]
        public struct Type_Using_Private_And_GraphicsBuffer
        {
            public int a;
            private int b;
        }

        //1.
        public struct NotAVFXType
        {
            public int a;
        }

        //2.
        [VFXType]
        public struct Type_Using_Not_VFX_Type_SubType
        {
            public float a;
            public NotAVFXType b;
        }

        //3.
        [VFXType(VFXTypeAttribute.Flags.GraphicsBuffer)]
        public struct Type_Using_Not_Supported_Type_By_VFX
        {
            public float a;
            public byte b;
        }

        //4.
        [VFXType(VFXTypeAttribute.Flags.GraphicsBuffer)]
        public struct Type_Not_Blittable
        {
            public float a;
            public Texture2D b;
        }
*/
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

        static readonly Tuple<Vector3, float>[] s_Colors = GenerateColorPrefixSum();
        static Tuple<Vector3, float>[] GenerateColorPrefixSum()
        {
            var colors = new Tuple<Vector3, float>[]
            {
                new Tuple<Vector3, float>(new Vector3(249.0f,251.0f,249.0f), 4.0f),
                new Tuple<Vector3, float>(new Vector3(46.0f,46.0f,45.0f), 1.0f),
                new Tuple<Vector3, float>(new Vector3(221.0f, 1.0f, 0.0f), 1.0f),
                new Tuple<Vector3, float>(new Vector3(32.0f, 80.0f, 149.0f), 1.0f),
                new Tuple<Vector3, float>(new Vector3(250.0f, 201.0f, 0), 1.0f),
            };

            var prefixSum = PrefixSum(colors.Select(o => o.Item2)).ToArray();
            var prefixSumNormalized = prefixSum.Select(o => o / prefixSum.Last()).ToArray();
            return colors.Zip(prefixSumNormalized, (a, b) => new Tuple<Vector3, float>(new Vector3(Mathf.GammaToLinearSpace(a.Item1.x / 255.0f), Mathf.GammaToLinearSpace(a.Item1.y / 255.0f), Mathf.GammaToLinearSpace(a.Item1.z / 255.0f)), b)).ToArray();
        }

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
                var newColor = s_Colors.Last().Item1;
                foreach (var color in s_Colors)
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
        private GraphicsBuffer m_Buffer;

        private static readonly uint s_MaxIteration = 12;
        private static readonly float s_WaitTime = 1.0f / (float)s_MaxIteration;

        private System.Random m_Random = new System.Random(1245);
        private uint m_MaxIteration = s_MaxIteration;
        private float m_Wait = s_WaitTime;

        private List<CustomData> m_Data;

        void Reallocate(int newSize)
        {
            if (m_Buffer != null)
                m_Buffer.Release();

            m_Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, newSize, Marshal.SizeOf(typeof(CustomData)));
            m_Buffer.SetData(new CustomData[newSize]);

            var vfx = GetComponent<VisualEffect>();
            vfx.SetGraphicsBuffer(s_BufferID, m_Buffer);
        }

        void Start()
        {
            Reallocate(4);
            m_Data = new List<CustomData>()
            {
                new CustomData() { position = new Vector3(0, 0, 0), rectangle = new Rectangle() { color = new Vector3(1, 1, 1), size = new Vector2(1024, 1024) }}
            };
            m_Buffer.SetData(m_Data);
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

                    if (m_Data.Count > m_Buffer.count)
                    {
                        int newCount = m_Buffer.count;
                        while (newCount < m_Data.Count)
                            newCount *= 2;
                        Reallocate(newCount);
                    }

                    m_Buffer.SetData(m_Data);

                    if (m_MaxIteration == 0)
                        m_Wait = 1.0f;
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
            if (m_Buffer != null)
            {
                m_Buffer.Release();
                m_Buffer = null;
            }
        }
    }
}
