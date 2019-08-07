using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;

using Random = System.Random;
namespace UnityEditor.VFX.UI
{

    class VFXDebugUI
    {

        public enum Modes
        {
            None,
            SystemStat
        }

        class CurveContent : ImmediateModeElement
        {
            class NormalizedCurve
            {
                Vector3[] m_Points;
                int[] m_Triangles;
                int m_MaxPoints;
                Mesh m_Mesh;

                private static readonly float s_Scale = .012f;

                public NormalizedCurve(int maxPoints)
                {
                    if (maxPoints < 2)
                        maxPoints = 2;
                    m_MaxPoints = maxPoints;
                    m_Points = new Vector3[2 * maxPoints];
                    m_Triangles = new int[6 * (maxPoints - 1)];

                    var step = 1.0f / (float)(maxPoints - 1);

                    for (int i = 0; i < maxPoints - 1; ++i)
                    {
                        m_Points[2 * i] = new Vector2(i * step, s_Scale);
                        m_Points[2 * i + 1] = new Vector2(i * step, -s_Scale);

                        int startIndex = i > 0 ? m_Triangles[6 * (i - 1) + 2] : 0;

                        m_Triangles[6 * i] = startIndex++;
                        m_Triangles[6 * i + 1] = startIndex++;
                        m_Triangles[6 * i + 2] = startIndex--;

                        m_Triangles[6 * i + 3] = startIndex++;
                        m_Triangles[6 * i + 4] = startIndex++;
                        m_Triangles[6 * i + 5] = startIndex;
                    }
                    m_Points[m_Points.Length - 2] = new Vector2((maxPoints - 1) * step, s_Scale);
                    m_Points[m_Points.Length - 1] = new Vector2((maxPoints - 1) * step, -s_Scale);


                    m_Mesh = new Mesh();
                    m_Mesh.vertices = m_Points;
                    m_Mesh.triangles = m_Triangles;
                }

                public Mesh GetMesh()
                {
                    return m_Mesh;
                }

                public void AddPoint(float value)
                {
                    m_Points = m_Mesh.vertices;

                    // shifting
                    for (int i = 1; i < m_MaxPoints; ++i)
                    {
                        m_Points[2 * (i - 1)].y = m_Points[2 * i].y;
                        m_Points[2 * (i - 1) + 1].y = m_Points[2 * i + 1].y;
                    }

                    // adding new point
                    m_Points[m_Points.Length - 2].y = value + s_Scale;
                    m_Points[m_Points.Length - 1].y = value - s_Scale;

                    m_Mesh.vertices = m_Points;
                }
            }

            Material m_Mat;
            VFXDebugUI m_DebugUI;
            int m_ClippingMatrixId;

            List<NormalizedCurve> m_VFXCurves;
            int m_MaxPoints;
            float m_TimeBetweenDraw;

            static readonly Random random = new Random();

            public CurveContent(VFXDebugUI debugUI, int maxPoints, float timeBetweenDraw = 0.033f)
            {
                m_DebugUI = debugUI;
                m_Mat = new Material(Shader.Find("Hidden/VFX/SystemStat"));
                m_ClippingMatrixId = Shader.PropertyToID("_ClipMatrix");
                m_MaxPoints = maxPoints;
                m_TimeBetweenDraw = timeBetweenDraw;

                schedule.Execute(MarkDirtyRepaint).Every((long)(m_TimeBetweenDraw * 1000.0f));

                OnVFXChange();
            }

            public void OnVFXChange()
            {
                var particleSystems = new List<string>();

                if (m_DebugUI.m_VFX != null)
                {
                    m_VFXCurves = new List<NormalizedCurve>(m_DebugUI.m_VFX.GetParticleSystemNames(particleSystems));
                    for (int i = 0; i < m_DebugUI.m_GpuSystems.Count(); ++i)
                    {
                        m_VFXCurves.Add(new NormalizedCurve(m_MaxPoints));
                    }
                }
            }

            private static Func<VisualElement, Rect> GetWorldClipRect()
            {
                var worldClipProp = typeof(VisualElement).GetMethod("get_worldClip", BindingFlags.NonPublic | BindingFlags.Instance);
                if (worldClipProp != null)
                {
                    return delegate (VisualElement elt)
                    {
                        return (Rect)worldClipProp.Invoke(elt, null);
                    };
                }

                Debug.LogError("could not retrieve get_worldClip");
                return delegate (VisualElement elt)
                {
                    return new Rect();
                };
            }

            static readonly Func<Box, Rect> k_BoxWorldclip = GetWorldClipRect();

            float m_LastAddTime;
            void DrawMesh()
            {
                if (m_Mat == null)
                {
                    m_Mat = new Material(Shader.Find("Hidden/VFX/SystemStat"));
                    m_ClippingMatrixId = Shader.PropertyToID("_ClipMatrix");
                }
                // drawing matrix
                var debugRect = m_DebugUI.m_DebugBox.worldBound;
                var clippedDebugRect = k_BoxWorldclip(m_DebugUI.m_DebugBox);
                var windowRect = panel.InternalGetGUIView().position;
                var trans = new Vector4(debugRect.x / windowRect.width, (windowRect.height - (debugRect.y + debugRect.height)) / windowRect.height, 0, 0);
                var scale = new Vector3(debugRect.width / windowRect.width, debugRect.height / windowRect.height, 0);

                // clipping matrix
                var clippedScale = new Vector3(windowRect.width / clippedDebugRect.width, windowRect.height / clippedDebugRect.height, 0);
                var clippedTrans = new Vector3(-clippedDebugRect.x / clippedDebugRect.width, ((clippedDebugRect.y + clippedDebugRect.height) - windowRect.height) / clippedDebugRect.height);
                var baseChange = Matrix4x4.TRS(clippedTrans, Quaternion.identity, clippedScale);
                m_Mat.SetMatrix(m_ClippingMatrixId, baseChange);

                // Updating curve
                int i = 0;
                var now = Time.time;
                bool shouldSample = false;
                if (now - m_LastAddTime > m_TimeBetweenDraw)
                {
                    shouldSample = true;
                    m_LastAddTime = now;
                }
                foreach (var curve in m_VFXCurves)
                {
                    if (shouldSample)
                    {
                        float alive = m_DebugUI.m_VFX.GetSystemAliveParticleCount(m_DebugUI.m_GpuSystems[i]);
                        float capacity = m_DebugUI.m_VFX.GetSystemCapacity(m_DebugUI.m_GpuSystems[i]);
                        curve.AddPoint(alive / capacity);
                    }

                    var color = Color.HSVToRGB((0.71405f + i * 0.37135766f) % 1.0f, 0.8f, 0.8f);
                    m_Mat.SetColor("_Color", color);

                    m_Mat.SetPass(0);
                    Graphics.DrawMeshNow(curve.GetMesh(), Matrix4x4.TRS(trans, Quaternion.identity, scale));

                    ++i;
                }
            }


            protected override void ImmediateRepaint()
            {
                DrawMesh();
            }
        }

        Modes m_CurrentMode;
        VFXComponentBoard m_ComponentBoard;
        CurveContent m_Curve;
        VisualElement m_DebugContainer;
        Box m_DebugBox;
        VFXView m_View;
        VisualEffect m_VFX;
        List<int> m_GpuSystems;

        // [0] container
        // [1] system name
        // [2] alive
        // [3] capacity
        // [4] efficiency
        List<VisualElement[]> m_SystemsStats;

        public VFXDebugUI(VFXView view)
        {
            m_View = view;
        }

        public void SetDebugMode(Modes mode, VFXComponentBoard componentBoard)
        {
            m_ComponentBoard = componentBoard;
            m_DebugContainer = m_ComponentBoard.Query<VisualElement>("debug-container");

            switch (mode)
            {
                case Modes.SystemStat:
                    SystemStat();
                    break;
                case Modes.None:
                    Clear();
                    break;
                default:
                    Clear();
                    break;
            }

            m_CurrentMode = mode;
        }

        public void SetVisualEffect(VisualEffect vfx)
        {
            m_VFX = vfx;

            if (m_Curve != null)
                m_Curve.OnVFXChange();
        }

        void SystemStat()
        {
            // drawing box
            m_DebugBox = new Box();
            m_DebugBox.name = "debug-box";
            m_DebugContainer.Add(m_DebugBox);
            m_Curve = new CurveContent(this, 100, 0.016f);
            m_ComponentBoard.contentContainer.Add(m_Curve);

            // system stats title
            var systemStatContainer = new VisualElement();
            systemStatContainer.name = "debug-system-stat-container";
            m_DebugContainer.Add(systemStatContainer);

            var systemStatName = new TextElement();
            systemStatName.name = "debug-system-stat-title-name";
            systemStatName.text = "Particle System";

            var systemStatAlive = new TextElement();
            systemStatAlive.name = "debug-system-stat-title";
            systemStatAlive.text = "Alive";

            var systemStatCapacity = new TextElement();
            systemStatCapacity.name = "debug-system-stat-title";
            systemStatCapacity.text = "Capacity";

            var systemStatEfficiency = new TextElement();
            systemStatEfficiency.name = "debug-system-stat-title";
            systemStatEfficiency.text = "Efficiency";

            systemStatContainer.Add(systemStatName);
            systemStatContainer.Add(systemStatAlive);
            systemStatContainer.Add(systemStatCapacity);
            systemStatContainer.Add(systemStatEfficiency);

            var systemStatTitle = new VisualElement[1];
            systemStatTitle[0] = systemStatContainer;

            m_SystemsStats = new List<VisualElement[]>();
            m_SystemsStats.Add(systemStatTitle);

            if (m_VFX != null)
            {
                List<string> particleSystemNames = new List<string>();
                m_VFX.GetParticleSystemNames(particleSystemNames);
                m_GpuSystems = new List<int>();
                foreach (var name in particleSystemNames)
                {
                    m_GpuSystems.Add(Shader.PropertyToID(name));

                }
            }
        }

        void Clear()
        {
            if (m_ComponentBoard != null && m_Curve != null)
                m_ComponentBoard.contentContainer.Remove(m_Curve);
            m_ComponentBoard = null;
            m_Curve = null;

            if (m_DebugContainer != null && m_DebugBox != null)
                m_DebugContainer.Remove(m_DebugBox);
            m_DebugBox = null;

            if (m_SystemsStats != null)
                foreach (var systemStat in m_SystemsStats)
                    m_DebugContainer.Remove(systemStat[0]);
            m_SystemsStats = null;

            m_DebugContainer = null;
        }
    }
}
