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

                private static readonly float s_Scale = .09f;

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

            class ToggleableCurve
            {
                int m_MaxPoints;
                Toggle m_Toggle;

                public NormalizedCurve curve { get; set; }
                public int id { get; set; }
                public Toggle toggle
                {
                    get { return m_Toggle; }
                    set
                    {
                        if (m_Toggle != value && m_Toggle != null)
                            m_Toggle.UnregisterValueChangedCallback(ToggleValueChanged);
                        m_Toggle = value;
                    }
                }

                public ToggleableCurve(int id, int maxPoints, Toggle toggle)
                {
                    m_MaxPoints = maxPoints;
                    curve = new NormalizedCurve(m_MaxPoints);
                    this.id = id;
                    this.toggle = toggle;
                    if (this.toggle != null)
                        toggle.RegisterValueChangedCallback(ToggleValueChanged);
                }

                void ToggleValueChanged(ChangeEvent<bool> evt)
                {
                    if (evt.newValue == false)
                        curve = new NormalizedCurve(m_MaxPoints);
                }


            }

            Material m_Mat;
            VFXDebugUI m_DebugUI;
            int m_ClippingMatrixId;

            List<ToggleableCurve> m_VFXCurves;
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
                    m_VFXCurves = new List<ToggleableCurve>(m_DebugUI.m_GpuSystems.Count());
                    for (int i = 0; i < m_DebugUI.m_GpuSystems.Count(); ++i)
                    {
                        var toggle = m_DebugUI.m_SystemStats[m_DebugUI.m_GpuSystems[i]][1] as Toggle;
                        var switchableCurve = new ToggleableCurve(m_DebugUI.m_GpuSystems[i], m_MaxPoints, toggle);
                        m_VFXCurves.Add(switchableCurve);
                    }
                }
            }




            float m_LastSampleTime;
            void DrawCurves()
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
                foreach (var switchableCurve in m_VFXCurves)
                {
                    if (switchableCurve.toggle == null || switchableCurve.toggle.value == true)
                    {
                        if (shouldSample && m_DebugUI.m_VFX.HasSystem(switchableCurve.id))
                        {
                            // updating stats
                            var alive = m_DebugUI.m_VFX.GetSystemAliveParticleCount(switchableCurve.id);
                            var capacity = m_DebugUI.m_VFX.GetSystemCapacity(switchableCurve.id);
                            float efficiency = (float)alive / (float)capacity;
                            switchableCurve.curve.AddPoint(efficiency);
                            m_DebugUI.UpdateSystemStat(switchableCurve.id, alive, capacity);
                        }

                        var color = Color.HSVToRGB((0.71405f + i * 0.37135766f) % 1.0f, 0.8f, 0.8f);
                        m_Mat.SetColor("_Color", color);

                        m_Mat.SetPass(0);
                        Graphics.DrawMeshNow(switchableCurve.curve.GetMesh(), Matrix4x4.TRS(trans, Quaternion.identity, scale));
                    }

                    ++i;
                }
            }


            protected override void ImmediateRepaint()
            {
                DrawCurves();
            }
        }

        Modes m_CurrentMode;

        VFXView m_View;
        VisualEffect m_VFX;
        List<int> m_GpuSystems;

        VFXComponentBoard m_ComponentBoard;
        VisualElement m_DebugContainer;
        Box m_DebugBox;
        CurveContent m_Curves;
        VisualElement m_SystemStatsContainer;
        // [0] container
        // [1] toggle
        // [2] system name
        // [3] alive
        // [4] capacity
        // [5] efficiency
        Dictionary<int, VisualElement[]> m_SystemStats;

        public VFXDebugUI(VFXView view)
        {
            m_View = view;
        }

        public void SetDebugMode(Modes mode, VFXComponentBoard componentBoard, bool force = false)
        {
            if (mode == m_CurrentMode && !force)
                return;

            ClearDebugMode();
            m_CurrentMode = mode;

            m_ComponentBoard = componentBoard;
            m_DebugContainer = m_ComponentBoard.Query<VisualElement>("debug-modes-container");

            switch (mode)
            {
                case Modes.SystemStat:
                    m_View.controller.RegisterNotification(m_View.controller.graph, UpdateDebugMode);
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

        void UpdateDebugMode()
        {
            switch (m_CurrentMode)
            {
                case Modes.SystemStat:
                    RegisterParticleSystems();
                    break;
                default:
                    break;
            }
        }

        void ClearDebugMode()
        {
            switch (m_CurrentMode)
            {
                case Modes.SystemStat:
                    m_View.controller.UnRegisterNotification(m_View.controller.graph, UpdateDebugMode);
                    break;
                default:
                    break;
            }
        }


        void RegisterParticleSystems()
        {
            if (m_SystemStats != null)
                foreach (var systemStat in m_SystemStats.Values)
                    m_SystemStatsContainer.Remove(systemStat[0]);

            m_SystemStats = new Dictionary<int, VisualElement[]>();
            if (m_VFX != null)
            {
                List<string> particleSystemNames = new List<string>();
                m_VFX.GetParticleSystemNames(particleSystemNames);
                m_GpuSystems = new List<int>();
                int i = 0;
                foreach (var name in particleSystemNames)
                {
                    int id = Shader.PropertyToID(name);
                    m_GpuSystems.Add(id);
                    CreateSystemStatEntry(name, id, Color.HSVToRGB((0.71405f + i * 0.37135766f) % 1.0f, 0.8f, 0.8f));

                    ++i;
                }

                m_Curves.OnVFXChange();
            }
        }

        void ToggleAll(ChangeEvent<bool> evt)
        {
            foreach (var systemStat in m_SystemStats.Values)
            {
                var toggle = systemStat[1] as Toggle;
                if (toggle != null)
                    toggle.value = evt.newValue;
            }
        }


        void SystemStat()
        {
            // axis
            var Yaxis = new VisualElement();
            Yaxis.name = "debug-box-axis-container";
            var hundredPercent = new TextElement();
            hundredPercent.text = "100%";
            hundredPercent.name = "debug-box-axis-100";
            var fiftyPercent = new TextElement();
            fiftyPercent.text = "50%";
            fiftyPercent.name = "debug-box-axis-50";
            var zeroPercent = new TextElement();
            zeroPercent.text = "0%";
            zeroPercent.name = "debug-box-axis-0";
            Yaxis.Add(hundredPercent);
            Yaxis.Add(fiftyPercent);
            Yaxis.Add(zeroPercent);

            // drawing box
            m_DebugBox = new Box();
            m_DebugBox.name = "debug-box";

            var statGraph = new VisualElement();
            statGraph.name = "debug-graph-container";

            statGraph.Add(m_DebugBox);
            statGraph.Add(Yaxis);

            m_DebugContainer.Add(statGraph);
            m_Curves = new CurveContent(this, 100, 0.016f);
            m_ComponentBoard.contentContainer.Add(m_Curves);

            // system stats title
            m_SystemStatsContainer = new ScrollView();
            m_SystemStatsContainer.name = "debug-system-stat-container";

            var toggleAll = new Toggle();
            toggleAll.value = true;
            toggleAll.RegisterValueChangedCallback(ToggleAll);

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

            var titleContainer = new VisualElement();
            titleContainer.name = "debug-system-stat-entry-container";

            titleContainer.Add(toggleAll);
            titleContainer.Add(systemStatName);
            titleContainer.Add(systemStatAlive);
            titleContainer.Add(systemStatCapacity);
            titleContainer.Add(systemStatEfficiency);

            m_DebugContainer.Add(titleContainer);
            m_DebugContainer.Add(m_SystemStatsContainer);

            // registering particle systems
            RegisterParticleSystems();

        }

        void CreateSystemStatEntry(string systemName, int id, Color color)
        {
            var statContainer = new VisualElement();
            statContainer.name = "debug-system-stat-entry-container";
            m_SystemStatsContainer.Add(statContainer);

            var toggle = new Toggle();
            toggle.value = true;

            var name = new TextElement();
            name.name = "debug-system-stat-entry-name";
            name.text = systemName;
            name.style.color = color;

            var alive = new TextElement();
            alive.name = "debug-system-stat-entry";
            alive.text = "0";

            var capacity = new TextElement();
            capacity.name = "debug-system-stat-entry";
            capacity.text = "0";

            var efficiency = new TextElement();
            efficiency.name = "debug-system-stat-entry";
            efficiency.text = "100 %";

            statContainer.Add(toggle);
            statContainer.Add(name);
            statContainer.Add(alive);
            statContainer.Add(capacity);
            statContainer.Add(efficiency);

            var stats = new VisualElement[6];
            stats[0] = statContainer;
            stats[1] = toggle;
            stats[2] = name;
            stats[3] = alive;
            stats[4] = capacity;
            stats[5] = efficiency;

            m_SystemStats[id] = stats;
        }

        void UpdateSystemStat(int systemId, int alive, int capacity)
        {
            var stat = m_SystemStats[systemId];// [0] is title bar
            if (stat[3] is TextElement Alive)
                Alive.text = alive.ToString();
            if (stat[4] is TextElement Capacity)
                Capacity.text = capacity.ToString();
            if (stat[5] is TextElement Efficiency)
                Efficiency.text = string.Format("{0} %", (int)((float)alive * 100.0f / (float)capacity));
        }

        public void Clear()
        {
            if (m_ComponentBoard != null && m_Curves != null)
                m_ComponentBoard.contentContainer.Remove(m_Curves);
            m_ComponentBoard = null;
            m_Curves = null;

            if (m_SystemStatsContainer != null)
                m_SystemStatsContainer.Clear();

            if (m_DebugContainer != null)
            {
                m_DebugContainer.Clear();
            }

            m_SystemStats = null;
            m_DebugBox = null;
            m_SystemStatsContainer = null;
            m_DebugContainer = null;

            //m_View.controller.UnRegisterNotification(m_View.controller.graph, UpdateDebugMode);
        }
    }
}
