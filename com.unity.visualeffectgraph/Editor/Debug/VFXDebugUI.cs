using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using Object = System.Object;

using Random = System.Random;
namespace UnityEditor.VFX.UI
{

    class VFXDebugUI
    {
        public enum Modes
        {
            None,
            Efficiency,
            Alive
        }

        public enum Events
        {
            VFXPlayPause,
            VFXReset,
            VFXStop
        }

        class CurveContent : ImmediateModeElement
        {

            class VerticalBar
            {
                Mesh m_Mesh;
                public VerticalBar(float xPos)
                {
                    m_Mesh = new Mesh();
                    m_Mesh.vertices = new Vector3[] { new Vector3(xPos, 0, 0), new Vector3(xPos, 1, 0) };
                    m_Mesh.SetIndices(new int[] { 0, 1 }, MeshTopology.Lines, 0);
                }

                public Mesh GetMesh()
                {
                    return m_Mesh;
                }
            }

            class NormalizedCurve
            {
                int m_MaxPoints;
                Mesh m_Mesh;

                Vector3[] m_Points;

                public NormalizedCurve(int maxPoints)
                {
                    if (maxPoints < 2)
                        maxPoints = 2;

                    m_MaxPoints = maxPoints;

                    // line output
                    m_Points = new Vector3[maxPoints];
                    var linesIndices = new int[2 * (maxPoints - 1)];

                    var step = 1.0f / (float)(maxPoints - 1);

                    for (int i = 0; i < maxPoints - 1; ++i)
                    {
                        m_Points[i] = new Vector3(i * step, -1, 0);
                        linesIndices[2 * i] = i;
                        linesIndices[2 * i + 1] = i + 1;
                    }

                    m_Points[m_Points.Length - 1] = new Vector3(1, 0, 0);

                    m_Mesh = new Mesh();
                    m_Mesh.vertices = m_Points;

                    m_Mesh.SetIndices(linesIndices, MeshTopology.Lines, 0);
                }

                public Mesh GetMesh()
                {
                    return m_Mesh;
                }


                public void AddPoint(float value)
                {
                    m_Points = m_Mesh.vertices;
                    for (int i = 1; i < m_MaxPoints; ++i)
                        m_Points[i - 1].y = m_Points[i].y;

                    m_Points[m_Points.Length - 1].y = value;

                    m_Mesh.vertices = m_Points;
                }

                public float GetMax()
                {
                    float max = m_Points[0].y;
                    foreach(var point in m_Points)
                    {
                        if (max < point.y)
                            max = point.y;
                    }

                    return max;
                }
            }

            class SwitchableCurve
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

                public SwitchableCurve(int id, int maxPoints, Toggle toggle)
                {
                    m_MaxPoints = maxPoints;
                    curve = new NormalizedCurve(m_MaxPoints);
                    this.id = id;
                    this.toggle = toggle;
                    if (this.toggle != null)
                        toggle.RegisterValueChangedCallback(ToggleValueChanged);
                }

                public void ResetCurve()
                {
                    curve = new NormalizedCurve(m_MaxPoints);
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

            List<SwitchableCurve> m_VFXCurves;
            //VerticalBar m_VerticalBar;
            int m_MaxPoints;
            float m_TimeBetweenDraw;
            bool m_Pause;
            bool m_Stopped;

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

                //m_VerticalBar = new VerticalBar(1);

                schedule.Execute(MarkDirtyRepaint).Every((long)(m_TimeBetweenDraw * 1000.0f));
            }

            public void OnVFXChange()
            {
                if ((m_DebugUI.m_CurrentMode == Modes.Efficiency || m_DebugUI.m_CurrentMode == Modes.Alive) && m_DebugUI.m_VFX != null)
                {
                    m_VFXCurves = new List<SwitchableCurve>(m_DebugUI.m_GpuSystems.Count());
                    for (int i = 0; i < m_DebugUI.m_GpuSystems.Count(); ++i)
                    {
                        var toggle = m_DebugUI.m_SystemStats[m_DebugUI.m_GpuSystems[i]][1] as Toggle;
                        var switchableCurve = new SwitchableCurve(m_DebugUI.m_GpuSystems[i], m_MaxPoints, toggle);
                        m_VFXCurves.Add(switchableCurve);
                    }
                }
            }

            public void Notify(Events e)
            {
                switch (e)
                {
                    case Events.VFXPlayPause:
                        m_Pause = !m_Pause;
                        m_Stopped = false;
                        break;
                    case Events.VFXReset:
                        foreach (var curve in m_VFXCurves)
                            curve.ResetCurve();
                        break;
                    case Events.VFXStop:
                        m_Pause = true;
                        m_Stopped = true;
                        foreach (var curve in m_VFXCurves)
                            curve.ResetCurve();
                        break;
                    default:
                        break;
                }
            }

            Object GetCurveData()
            {
                switch (m_DebugUI.m_CurrentMode)
                {
                    case Modes.Efficiency:
                        return null;
                    case Modes.Alive:
                        {
                            float max = -1;
                            foreach (var switchableCurve in m_VFXCurves)
                            {
                                max = Mathf.Max(switchableCurve.curve.GetMax(), max);
                            }
                            return max;
                        }
                    default:
                        return null;
                }
            }

            void UpdateCurve(SwitchableCurve switchableCurve, Object data)
            {
                switch (m_DebugUI.m_CurrentMode)
                {
                    case Modes.Efficiency:
                        {
                            var alive = m_DebugUI.m_VFX.GetSystemAliveParticleCount(switchableCurve.id);
                            var capacity = m_DebugUI.m_VFX.GetSystemCapacity(switchableCurve.id);
                            float efficiency = (float)alive / (float)capacity;
                            switchableCurve.curve.AddPoint(efficiency);
                            m_DebugUI.UpdateEfficiency(switchableCurve.id, alive, capacity);
                        }
                        break;
                    case Modes.Alive:
                        {
                            var alive = m_DebugUI.m_VFX.GetSystemAliveParticleCount(switchableCurve.id);
                            var capacity = m_DebugUI.m_VFX.GetSystemCapacity(switchableCurve.id);
                            Debug.Log( (int)((float)data));
                            float normalizeAlive = (float)alive / Mathf.Max((float)data, 1.0f);// <= bidon
                            switchableCurve.curve.AddPoint(normalizeAlive);
                            m_DebugUI.UpdateAlive(switchableCurve.id, alive, capacity);
                        }
                        break;
                    default:
                        break;
                }
            }

            float m_LastSampleTime;
            void DrawCurves()
            {
                if (m_Stopped)
                    return;

                if (m_Mat == null)
                {
                    m_Mat = new Material(Shader.Find("Hidden/VFX/SystemStat"));
                    m_ClippingMatrixId = Shader.PropertyToID("_ClipMatrix");
                }
                // drawing matrix
                var debugRect = m_DebugUI.m_DebugDrawingBox.worldBound;
                var clippedDebugRect = k_BoxWorldclip(m_DebugUI.m_DebugDrawingBox);
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
                bool shouldSample = !m_Pause && (now - m_LastSampleTime > m_TimeBetweenDraw);
                if (shouldSample)
                {
                    //Debug.Log(now - m_LastSampleTime);
                    m_LastSampleTime = now;
                }

                int i = 0;
                var curveData = GetCurveData();
                foreach (var curve in m_VFXCurves)
                {
                    if (curve.toggle == null || curve.toggle.value == true)
                    {
                        if (shouldSample && m_DebugUI.m_VFX.HasSystem(curve.id))
                        {
                            UpdateCurve(curve, curveData);
                        }

                        var color = Color.HSVToRGB((0.71405f + i * 0.37135766f) % 1.0f, 0.6f, 1.0f).gamma;
                        m_Mat.SetColor("_Color", color);

                        m_Mat.SetPass(0);
                        Graphics.DrawMeshNow(curve.curve.GetMesh(), Matrix4x4.TRS(trans, Quaternion.identity, scale));
                    }

                    ++i;
                }

                // time bars
                //float n = 1.0f / m_TimeBetweenDraw;
                //float d = (float)n / (float)m_MaxPoints;
                //for (float k = 0; k * d < 1.0f; ++k)
                //{
                //    float offset = k * d * (debugRect.width / windowRect.width);
                //    var offsetTrans = trans;
                //    offsetTrans.x -= offset;

                //    Color color = Color.red;
                //    color.a = 0.5f;
                //    m_Mat.SetColor("_Color", color.gamma);
                //    m_Mat.SetPass(0);
                //    Graphics.DrawMeshNow(m_VerticalBar.GetMesh(), Matrix4x4.TRS(offsetTrans, Quaternion.identity, scale));
                //}
            }


            protected override void ImmediateRepaint()
            {
                DrawCurves();
            }
        }

        Modes m_CurrentMode;

        // graph characteristics
        VFXView m_View;
        VisualEffect m_VFX;
        List<int> m_GpuSystems;

        // debug components
        VFXComponentBoard m_ComponentBoard;
        VisualElement m_DebugContainer;
        VisualElement m_SystemStatsContainer;
        Box m_DebugDrawingBox;
        CurveContent m_Curves;

        // [0] container
        // [1] toggle
        // [2] system name
        // [3] alive
        // [4] capacity
        // [5] efficiency
        Dictionary<int, VisualElement[]> m_SystemStats;
        // [0] bottom value
        // [1] mid value
        // [2] top value
        VisualElement[] m_YaxisElts;

        public VFXDebugUI(VFXView view)
        {
            m_View = view;
        }

        ~VFXDebugUI()
        {
            Clear();
        }

        public void SetDebugMode(Modes mode, VFXComponentBoard componentBoard, bool force = false)
        {
            if (mode == m_CurrentMode && !force)
                return;

            ClearDebugMode();
            m_CurrentMode = mode;

            m_ComponentBoard = componentBoard;
            m_DebugContainer = m_ComponentBoard.Query<VisualElement>("debug-modes-container");

            switch (m_CurrentMode)
            {
                case Modes.Efficiency:
                    m_View.controller.RegisterNotification(m_View.controller.graph, UpdateDebugMode);
                    Efficiency();
                    break;
                case Modes.Alive:
                    m_View.controller.RegisterNotification(m_View.controller.graph, UpdateDebugMode);
                    Alive();
                    break;
                case Modes.None:
                    Clear();
                    break;
                default:
                    Clear();
                    break;
            }
        }

        void UpdateDebugMode()
        {
            switch (m_CurrentMode)
            {
                case Modes.Efficiency:
                    RegisterParticleSystems();
                    break;
                case Modes.Alive:
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
                case Modes.Efficiency:
                    m_View.controller.UnRegisterNotification(m_View.controller.graph, UpdateDebugMode);
                    Clear();
                    break;
                case Modes.Alive:
                    m_View.controller.UnRegisterNotification(m_View.controller.graph, UpdateDebugMode);
                    Clear();
                    break;
                default:
                    break;
            }
        }


        public void SetVisualEffect(VisualEffect vfx)
        {
            m_VFX = vfx;

            if (m_Curves != null)
                m_Curves.OnVFXChange();
        }

        public void Notify(Events e)
        {
            switch (e)
            {
                case Events.VFXPlayPause:
                    m_Curves.Notify(Events.VFXPlayPause);
                    break;
                case Events.VFXReset:
                    m_Curves.Notify(Events.VFXReset);
                    break;
                case Events.VFXStop:
                    m_Curves.Notify(Events.VFXStop);
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
                    CreateSystemStatEntry(name, id, Color.HSVToRGB((0.71405f + i * 0.37135766f) % 1.0f, 0.6f, 1.0f));

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


        void Efficiency()
        {
            // ui
            m_Curves = new CurveContent(this, (int)(10.0f / 0.016f), 0.016f);
            m_ComponentBoard.contentContainer.Add(m_Curves);

            var Yaxis = SetYAxis("100%", "50%", "0%");
            m_DebugDrawingBox = SetDebugDrawingBox();
            var plotArea = SetPlotArea(m_DebugDrawingBox, Yaxis);

            var title =  SetStatsTitle();

            m_SystemStatsContainer = SetSystemStatContainer();

            m_DebugContainer.Add(plotArea);
            m_DebugContainer.Add(title);
            m_DebugContainer.Add(m_SystemStatsContainer);

            // recover debug data
            RegisterParticleSystems();
        }

        void Alive()
        {
            // ui
            m_Curves = new CurveContent(this, 300, 0.016f);
            m_ComponentBoard.contentContainer.Add(m_Curves);

            var Yaxis = SetYAxis("", "", "");
            m_DebugDrawingBox = SetDebugDrawingBox();
            var plotArea = SetPlotArea(m_DebugDrawingBox, Yaxis);

            var title = SetStatsTitle();

            m_SystemStatsContainer = SetSystemStatContainer();

            m_DebugContainer.Add(plotArea);
            m_DebugContainer.Add(title);
            m_DebugContainer.Add(m_SystemStatsContainer);

            // recover debug data
            RegisterParticleSystems();
        }

        VisualElement SetYAxis(string topValue, string midValue, string botValue)
        {
            var Yaxis = new VisualElement();
            Yaxis.name = "debug-box-axis-container";
            var top = new TextElement();
            top.text = topValue;
            top.name = "debug-box-axis-100";
            var mid = new TextElement();
            mid.text = midValue;
            mid.name = "debug-box-axis-50";
            var bot = new TextElement();
            bot.text = botValue;
            bot.name = "debug-box-axis-0";
            Yaxis.Add(top);
            Yaxis.Add(mid);
            Yaxis.Add(bot);

            m_YaxisElts = new VisualElement[3];
            m_YaxisElts[0] = bot;
            m_YaxisElts[1] = mid;
            m_YaxisElts[2] = top;

            return Yaxis;
        }

        Box SetDebugDrawingBox()
        {
            var debugBox = new Box();
            debugBox.name = "debug-box";
            return debugBox;
        }

        VisualElement SetPlotArea(Box debugDrawingBox, VisualElement Yaxis)
        {
            var plotArea = new VisualElement();
            plotArea.name = "debug-plot-area";

            plotArea.Add(debugDrawingBox);
            plotArea.Add(Yaxis);

            return plotArea;
        }

        VisualElement SetSystemStatContainer()
        {
            var scrollerContainer = new ScrollView();
            scrollerContainer.name = "debug-system-stat-container";
            return scrollerContainer;
        }

        VisualElement SetStatsTitle()
        {
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

            return titleContainer;
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
            alive.text = " - ";

            var capacity = new TextElement();
            capacity.name = "debug-system-stat-entry";
            capacity.text = " - ";

            var efficiency = new TextElement();
            efficiency.name = "debug-system-stat-entry";
            efficiency.text = " - ";

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

        void UpdateEfficiency(int systemId, int alive, int capacity)
        {
            var stat = m_SystemStats[systemId];// [0] is title bar
            if (stat[3] is TextElement Alive)
                Alive.text = alive.ToString();
            if (stat[4] is TextElement Capacity)
                Capacity.text = capacity.ToString();
            if (stat[5] is TextElement Efficiency)
                Efficiency.text = string.Format("{0} %", (int)((float)alive * 100.0f / (float)capacity));
        }

        void UpdateAlive(int systemId, int alive, int capacity)
        {
            UpdateEfficiency(systemId, alive, capacity);
        }


        public void Clear()
        {
            if (m_ComponentBoard != null && m_Curves != null)
                m_ComponentBoard.contentContainer.Remove(m_Curves);
            m_ComponentBoard = null;
            m_Curves = null;

            if (m_SystemStatsContainer != null)
                m_SystemStatsContainer.Clear();

            m_YaxisElts = null;

            if (m_DebugContainer != null)
                m_DebugContainer.Clear();
            

            m_SystemStats = null;
            m_DebugDrawingBox = null;
            m_SystemStatsContainer = null;
            m_DebugContainer = null;
        }

       

    }
}
