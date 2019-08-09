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
                Vector2[] m_Segments1;
                Vector2[] m_Segments2;
                int[] m_Triangles;
                int m_MaxPoints;
                Mesh m_Mesh;
                float m_LastValue = 0.0f;

                private static readonly float s_Scale = .03f;

                public NormalizedCurve(int maxPoints)
                {
                    if (maxPoints < 2)
                        maxPoints = 2;
                    m_MaxPoints = maxPoints;
                    m_Points = new Vector3[4 * (maxPoints - 1)];
                    m_Segments1 = new Vector2[4 * (maxPoints - 1)];
                    m_Segments2 = new Vector2[4 * (maxPoints - 1)];
                    m_Triangles = new int[6 * (maxPoints - 1)];

                    var step = 1.0f / (float)(maxPoints - 1);

                    for (int i = 0; i < maxPoints - 1; ++i)
                    {
                        var leftPoint = new Vector2(i * step, 0);
                        var rightPoint = new Vector2((i + 1) * step, 0);

                        var rect = ComputeDrawingRect(leftPoint, rightPoint);
                        m_Points[4 * i] = rect[0];
                        m_Points[4 * i + 1] = rect[1];
                        m_Points[4 * i + 2] = rect[2];
                        m_Points[4 * i + 3] = rect[3];

                        m_Segments1[4 * i] = leftPoint;
                        m_Segments1[4 * i + 1] = leftPoint;
                        m_Segments1[4 * i + 2] = leftPoint;
                        m_Segments1[4 * i + 3] = leftPoint;

                        m_Segments2[4 * i] = rightPoint;
                        m_Segments2[4 * i + 1] = rightPoint;
                        m_Segments2[4 * i + 2] = rightPoint;
                        m_Segments2[4 * i + 3] = rightPoint;


                        int startIndex = 4 * i;

                        m_Triangles[6 * i] = startIndex++;
                        m_Triangles[6 * i + 1] = startIndex++;
                        m_Triangles[6 * i + 2] = startIndex;

                        m_Triangles[6 * i + 3] = startIndex++;
                        m_Triangles[6 * i + 4] = startIndex - 3;
                        m_Triangles[6 * i + 5] = startIndex;
                    }

                    m_Mesh = new Mesh();
                    m_Mesh.vertices = m_Points;
                    m_Mesh.uv = m_Segments1;
                    m_Mesh.uv2 = m_Segments2;
                    m_Mesh.triangles = m_Triangles;
                }

                public Mesh GetMesh()
                {
                    return m_Mesh;
                }


                public void AddPoint(float value)
                {
                    m_Points = m_Mesh.vertices;
                    m_Segments1 = m_Mesh.uv;
                    m_Segments2 = m_Mesh.uv2;

                    // shifting drawing rectangles
                    for (int i = 1; i < m_MaxPoints - 1; ++i)
                    {
                        m_Points[4 * (i - 1)].y = m_Points[4 * i].y;
                        m_Segments1[4 * (i - 1)].y = m_Segments1[4 * i].y;
                        m_Segments2[4 * (i - 1)].y = m_Segments2[4 * i].y;

                        m_Points[4 * (i - 1) + 1].y = m_Points[4 * i + 1].y;
                        m_Segments1[4 * (i - 1) + 1].y = m_Segments1[4 * i + 1].y;
                        m_Segments2[4 * (i - 1) + 1].y = m_Segments2[4 * i + 1].y;

                        m_Points[4 * (i - 1) + 2].y = m_Points[4 * i + 2].y;
                        m_Segments1[4 * (i - 1) + 2].y = m_Segments1[4 * i + 2].y;
                        m_Segments2[4 * (i - 1) + 2].y = m_Segments2[4 * i + 2].y;

                        m_Points[4 * (i - 1) + 3].y = m_Points[4 * i + 3].y;
                        m_Segments1[4 * (i - 1) + 3].y = m_Segments1[4 * i + 3].y;
                        m_Segments2[4 * (i - 1) + 3].y = m_Segments2[4 * i + 3].y;
                    }

                    // adding new point
                    var step = 1.0f / (float)(m_MaxPoints - 1);
                    var leftPoint = new Vector2((float)(m_MaxPoints - 2) * step, m_LastValue);
                    var rightPoint = new Vector2(1.0f, value);
                    var rect = ComputeDrawingRect(leftPoint, rightPoint);

                    m_Points[4 * (m_MaxPoints - 2)].y = rect[0].y;
                    m_Segments1[4 * (m_MaxPoints - 2)].y = m_LastValue;
                    m_Segments2[4 * (m_MaxPoints - 2)].y = value;

                    m_Points[4 * (m_MaxPoints - 2) + 1].y = rect[1].y;
                    m_Segments1[4 * (m_MaxPoints - 2) + 1].y = m_LastValue;
                    m_Segments2[4 * (m_MaxPoints - 2) + 1].y = value;

                    m_Points[4 * (m_MaxPoints - 2) + 2].y = rect[2].y;
                    m_Segments1[4 * (m_MaxPoints - 2) + 2].y = m_LastValue;
                    m_Segments2[4 * (m_MaxPoints - 2) + 2].y = value;

                    m_Points[4 * (m_MaxPoints - 2) + 3].y = rect[3].y;
                    m_Segments1[4 * (m_MaxPoints - 2) + 3].y = m_LastValue;
                    m_Segments2[4 * (m_MaxPoints - 2) + 3].y = value;

                    m_Mesh.vertices = m_Points;
                    m_Mesh.uv = m_Segments1;
                    m_Mesh.uv2 = m_Segments2;

                    m_LastValue = value;
                }

                Vector3[] ComputeDrawingRect(Vector2 leftPoint, Vector2 rightPoint)
                {
                    var Ymax = Mathf.Max(leftPoint.y, rightPoint.y) + s_Scale;
                    var Ymin = Mathf.Min(leftPoint.y, rightPoint.y) - s_Scale;
                    var rect = new Vector3[4];
                    rect[0] = new Vector3(leftPoint.x, Ymax);
                    rect[1] = new Vector3(rightPoint.x, Ymax);
                    rect[2] = new Vector3(rightPoint.x, Ymin);
                    rect[3] = new Vector3(leftPoint.x, Ymin);

                    return rect;
                }
            }


            Material m_Mat;
            VFXDebugUI m_DebugUI;
            int m_ClippingMatrixId;

            List<NormalizedCurve> m_VFXCurves;
            int m_MaxPoints;
            float m_TimeBetweenDraw;

            static readonly Random random = new Random();

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

            public CurveContent(VFXDebugUI debugUI, int maxPoints, float timeBetweenDraw = 0.033f)
            {
                m_DebugUI = debugUI;
                m_Mat = new Material(Shader.Find("Hidden/VFX/SystemStat"));
                m_ClippingMatrixId = Shader.PropertyToID("_ClipMatrix");
                m_MaxPoints = maxPoints;
                m_TimeBetweenDraw = timeBetweenDraw;

                schedule.Execute(MarkDirtyRepaint).Every((long)(m_TimeBetweenDraw * 1000.0f));
            }

            public void OnVFXChange()
            {
                if (m_DebugUI.m_CurrentMode == Modes.SystemStat && m_DebugUI.m_VFX != null)
                {
                    m_VFXCurves = new List<NormalizedCurve>(m_DebugUI.m_GpuSystems.Count());
                    for (int i = 0; i < m_DebugUI.m_GpuSystems.Count(); ++i)
                    {
                        m_VFXCurves.Add(new NormalizedCurve(m_MaxPoints));
                    }
                }
            }



            float m_LastSampleTime;
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
                var now = Time.time;
                bool shouldSample = now - m_LastSampleTime > m_TimeBetweenDraw;
                if (shouldSample)
                    m_LastSampleTime = now;

                int i = 0;
                foreach (var curve in m_VFXCurves)
                {
                    if (shouldSample)
                    {
                        // updating stats
                        var alive = m_DebugUI.m_VFX.GetSystemAliveParticleCount(m_DebugUI.m_GpuSystems[i]);
                        var capacity = m_DebugUI.m_VFX.GetSystemCapacity(m_DebugUI.m_GpuSystems[i]);
                        float efficiency = (float)alive / (float)capacity;
                        curve.AddPoint(efficiency);
                        m_DebugUI.UpdateSystemStat(i, alive, capacity);
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
        CurveContent m_Curves;
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
        List<VisualElement[]> m_SystemStats;

        public VFXDebugUI(VFXView view)
        {
            m_View = view;
        }

        public void SetDebugMode(Modes mode, VFXComponentBoard componentBoard)
        {
            m_CurrentMode = mode;

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

        }

        public void SetVisualEffect(VisualEffect vfx)
        {
            m_VFX = vfx;

            if (m_Curves != null)
                m_Curves.OnVFXChange();
        }

        void SystemStat()
        {
            // drawing box
            m_DebugBox = new Box();
            m_DebugBox.name = "debug-box";
            m_DebugContainer.Add(m_DebugBox);
            m_Curves = new CurveContent(this, 100, 0.016f);
            m_ComponentBoard.contentContainer.Add(m_Curves);

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

            m_SystemStats = new List<VisualElement[]>();
            m_SystemStats.Add(systemStatTitle);

            if (m_VFX != null)
            {
                List<string> particleSystemNames = new List<string>();
                m_VFX.GetParticleSystemNames(particleSystemNames);
                m_GpuSystems = new List<int>();
                int i = 0;
                foreach (var name in particleSystemNames)
                {
                    m_GpuSystems.Add(Shader.PropertyToID(name));
                    CreateSystemStatEntry(name, Color.HSVToRGB((0.71405f + i * 0.37135766f) % 1.0f, 0.8f, 0.8f));

                    ++i;
                }

                m_Curves.OnVFXChange();
            }
        }

        void CreateSystemStatEntry(string name, Color color)
        {
            var statContainer = new VisualElement();
            statContainer.name = "debug-system-stat-container";
            m_DebugContainer.Add(statContainer);

            var Name = new TextElement();
            Name.name = "debug-system-name";
            Name.text = name;
            Name.style.color = color;

            var Alive = new TextElement();
            Alive.name = "debug-system-stat";
            Alive.text = "0";

            var Capacity = new TextElement();
            Capacity.name = "debug-system-stat";
            Capacity.text = "0";

            var Efficiency = new TextElement();
            Efficiency.name = "debug-system-stat";
            Efficiency.text = "100 %";

            statContainer.Add(Name);
            statContainer.Add(Alive);
            statContainer.Add(Capacity);
            statContainer.Add(Efficiency);

            var stats = new VisualElement[5];
            stats[0] = statContainer;
            stats[1] = Name;
            stats[2] = Alive;
            stats[3] = Capacity;
            stats[4] = Efficiency;

            m_SystemStats.Add(stats);
        }

        void UpdateSystemStat(int i, int alive, int capacity)
        {
            var stat = m_SystemStats[i + 1];// [0] is title bar
            if (stat[2] is TextElement Alive)
                Alive.text = alive.ToString();
            if (stat[3] is TextElement Capacity)
                Capacity.text = capacity.ToString();
            if (stat[4] is TextElement Efficiency)
                Efficiency.text = string.Format("{0} %", (int)((float)alive * 100.0f / (float)capacity));
        }

        public void Clear()
        {
            if (m_ComponentBoard != null && m_Curves != null)
                m_ComponentBoard.contentContainer.Remove(m_Curves);
            m_ComponentBoard = null;
            m_Curves = null;

            if (m_DebugContainer != null && m_DebugBox != null)
                m_DebugContainer.Remove(m_DebugBox);
            m_DebugBox = null;

            if (m_SystemStats != null)
                foreach (var systemStat in m_SystemStats)
                    m_DebugContainer.Remove(systemStat[0]);
            m_SystemStats = null;

            m_DebugContainer = null;
        }
    }
}
