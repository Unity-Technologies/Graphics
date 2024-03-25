using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.VFX;
using Object = System.Object;

namespace UnityEditor.VFX.UI
{
    class VFXUIDebug
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
            VFXStep,
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
                    m_Mesh.vertices = new Vector3[] { new Vector3(xPos, -1, 0), new Vector3(xPos, 1, 0) };
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
                        m_Points[i] = new Vector3(i * step, -99999999, 0);
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
                    foreach (var point in m_Points)
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
                //Button m_MaxAlive;

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

            Material m_CurveMat;
            Material m_BarMat;
            VFXUIDebug m_DebugUI;
            int m_ClippingMatrixId;

            List<SwitchableCurve> m_VFXCurves;
            VerticalBar m_VerticalBar;
            List<float> m_TimeBarsOffsets;

            int m_MaxPoints;
            float m_TimeBetweenDraw;
            bool m_Pause;
            bool m_Stopped;
            bool m_Step;
            bool m_ShouldDrawTimeBars = true;
            static readonly float s_TimeBarsInterval = 1;

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

            private static readonly Func<Box, Rect> k_BoxWorldclip = GetWorldClipRect();

            public CurveContent(VFXUIDebug debugUI, int maxPoints, long timeBetweenDraw = 33)
            {
                m_DebugUI = debugUI;
                m_CurveMat = new Material(Shader.Find("Hidden/VFX/SystemInfo"));
                m_BarMat = new Material(Shader.Find("Hidden/VFX/TimeBar"));
                m_ClippingMatrixId = Shader.PropertyToID("_ClipMatrix");
                m_MaxPoints = maxPoints;
                m_VFXCurves = new List<SwitchableCurve>();

                m_VerticalBar = new VerticalBar(0);
                m_TimeBarsOffsets = new List<float>();
                m_LastTimeBarDrawTime = -2 * s_TimeBarsInterval;

                SetSamplingRate((long)timeBetweenDraw);
            }

            public void SetSamplingRate(long rate)
            {
                //schedule.Execute(MarkDirtyRepaint).Every(rate);
                m_TimeBetweenDraw = rate / 1000.0f;
            }

            public void OnVFXChange()
            {
                if (m_DebugUI.m_VFX != null)
                {
                    m_Pause = m_Stopped = m_DebugUI.m_VFX.pause;
                    if (m_DebugUI.m_CurrentMode == Modes.Efficiency || m_DebugUI.m_CurrentMode == Modes.Alive)
                    {
                        m_VFXCurves.Clear();
                        m_TimeBarsOffsets.Clear();
                        for (int i = 0; i < m_DebugUI.m_GpuSystems.Count(); ++i)
                        {
                            var toggle = m_DebugUI.m_SystemInfos[m_DebugUI.m_GpuSystems[i]][1] as Toggle;
                            var switchableCurve = new SwitchableCurve(m_DebugUI.m_GpuSystems[i], m_MaxPoints, toggle);
                            m_VFXCurves.Add(switchableCurve);
                        }
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
                    case Events.VFXStep:
                        m_Step = true;
                        m_Pause = true;
                        m_Stopped = false;
                        break;
                    case Events.VFXReset:
                        m_Stopped = false;
                        m_Pause = false;
                        foreach (var curve in m_VFXCurves)
                            curve.ResetCurve();
                        m_TimeBarsOffsets.Clear();
                        break;
                    case Events.VFXStop:
                        m_Pause = true;
                        m_Stopped = true;
                        foreach (var curve in m_VFXCurves)
                            curve.ResetCurve();
                        m_TimeBarsOffsets.Clear();
                        break;
                    default:
                        break;
                }
            }

            public void SetDrawTimeBars(bool status)
            {
                m_ShouldDrawTimeBars = status;
            }

            Object GetCurvesData()
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
                        var stat = m_DebugUI.m_VFX.GetParticleSystemInfo(switchableCurve.id);
                        float efficiency = (float)stat.aliveCount / (float)stat.capacity;

                        m_CurveMat.SetFloat("_OrdinateScale", 1.0f);
                        switchableCurve.curve.AddPoint(efficiency);
                        m_DebugUI.UpdateSystemInfoEntry(switchableCurve.id, stat);
                    }
                    break;
                    case Modes.Alive:
                    {
                        var stat = m_DebugUI.m_VFX.GetParticleSystemInfo(switchableCurve.id);
                        float maxAlive = (float)data;

                        var superior2 = 1u << (int)Mathf.CeilToInt(Mathf.Log(maxAlive, 2.0f));
                        m_DebugUI.m_YaxisElts[1].text = (superior2 / 2).ToString();
                        m_DebugUI.m_YaxisElts[2].text = superior2.ToString();

                        m_CurveMat.SetFloat("_OrdinateScale", 1.0f / (float)superior2);
                        switchableCurve.curve.AddPoint(stat.aliveCount);
                        m_DebugUI.UpdateSystemInfoEntry(switchableCurve.id, stat);
                    }
                    break;
                    default:
                        break;
                }
            }

            float m_LastSampleTime;
            float m_LastTimeBarDrawTime;
            void DrawCurves()
            {
                if (m_Stopped)
                    return;

                MarkDirtyRepaint();

                // draw matrix
                var debugRect = m_DebugUI.m_DebugDrawingBox.worldBound;
                var clippedDebugRect = k_BoxWorldclip(m_DebugUI.m_DebugDrawingBox);
                var windowRect = panel.InternalGetGUIView().position;
                var trans = new Vector4(debugRect.x / windowRect.width, (windowRect.height - (debugRect.y + debugRect.height)) / windowRect.height, 0, 0);
                var scale = new Vector3(debugRect.width / windowRect.width, debugRect.height / windowRect.height, 0);

                // clip matrix
                var clippedScale = new Vector3(windowRect.width / clippedDebugRect.width, windowRect.height / clippedDebugRect.height, 0);
                var clippedTrans = new Vector3(-clippedDebugRect.x / clippedDebugRect.width, ((clippedDebugRect.y + clippedDebugRect.height) - windowRect.height) / clippedDebugRect.height);
                var baseChange = Matrix4x4.TRS(clippedTrans, Quaternion.identity, clippedScale);
                m_CurveMat.SetMatrix(m_ClippingMatrixId, baseChange);
                m_BarMat.SetMatrix(m_ClippingMatrixId, baseChange);


                // curves update
                var now = Time.time;
                bool shouldSample = (!m_Pause && (now - m_LastSampleTime > m_TimeBetweenDraw)) || (m_Pause && m_Step);
                m_Step = false;
                if (shouldSample)
                {
                    m_LastSampleTime = now;
                }

                int i = 0;
                var curveData = GetCurvesData();
                var TRS = Matrix4x4.TRS(trans, Quaternion.identity, scale);
                foreach (var vfxCurve in m_VFXCurves)
                {
                    if (vfxCurve.toggle == null || vfxCurve.toggle.value == true)
                    {
                        if (shouldSample && m_DebugUI.m_VFX.HasSystem(vfxCurve.id))
                        {
                            UpdateCurve(vfxCurve, curveData);
                        }

                        m_CurveMat.SetColor("_Color", GetColor(i));

                        m_CurveMat.SetPass(0);
                        Graphics.DrawMeshNow(vfxCurve.curve.GetMesh(), TRS);
                    }

                    ++i;
                }

                // time bars creation
                if (shouldSample && (now - m_LastTimeBarDrawTime > s_TimeBarsInterval))
                {
                    m_LastTimeBarDrawTime = now;
                    m_TimeBarsOffsets.Add(1);
                }

                // time bars update
                var xShift = 1.0f / (float)(m_MaxPoints - 1);
                Color timeBarColor = new Color(1, 0, 0, 0.5f).gamma;
                for (int j = 0; j < m_TimeBarsOffsets.Count(); ++j)
                {
                    if (m_TimeBarsOffsets[j] < 0)
                    {
                        m_TimeBarsOffsets.RemoveAt(j);
                        --j;
                        continue;
                    }

                    m_BarMat.SetFloat("_AbscissaOffset", m_TimeBarsOffsets[j]);
                    if (shouldSample)
                        m_TimeBarsOffsets[j] -= xShift;

                    if (m_ShouldDrawTimeBars)
                    {
                        m_BarMat.SetColor("_Color", timeBarColor);
                        m_BarMat.SetPass(0);
                        Graphics.DrawMeshNow(m_VerticalBar.GetMesh(), TRS);
                    }
                }

                m_BarMat.SetFloat("_AbscissaOffset", 0);
            }

            protected override void ImmediateRepaint()
            {
                DrawCurves();
            }
        }


        // graph characteristics
        VFXGraph m_Graph;
        VFXView m_View;
        VisualEffect m_VFX;
        List<int> m_GpuSystems;


        // debug components
        Modes m_CurrentMode;
        VFXComponentBoard m_ComponentBoard;
        VisualElement m_DebugContainer;
        Button m_DebugButton;
        VisualElement m_SystemInfosContainer;
        Box m_DebugDrawingBox;
        CurveContent m_Curves;

        // VisualElement[] layout:
        // [0] container
        // [1] toggle
        // [2] system name
        // [3] alive
        // [4] max alive (Button or TextElement)
        // [5] efficiency
        Dictionary<int, VisualElement[]> m_SystemInfos;

        // TextElement[] layout:
        // [0] bottom value
        // [1] mid value
        // [2] top value
        TextElement[] m_YaxisElts;

        public VFXUIDebug(VFXView view)
        {
            m_View = view;
            m_Graph = m_View.controller.graph;
            m_GpuSystems = new List<int>();
        }

        ~VFXUIDebug()
        {
            Clear();
            m_View = null;
            m_VFX = null;
            m_GpuSystems = null;
            m_CurrentMode = Modes.None;
        }

        public Modes GetDebugMode()
        {
            return m_CurrentMode;
        }

        public void SetDebugMode(Modes mode, VFXComponentBoard componentBoard, bool force = false)
        {
            if (mode == m_CurrentMode && !force)
                return;

            Clear();
            m_CurrentMode = mode;

            m_ComponentBoard = componentBoard;
            m_DebugContainer = m_ComponentBoard.Query<VisualElement>("debug-modes-container");
            m_DebugButton = m_ComponentBoard.Query<Button>("debug-modes");

            switch (m_CurrentMode)
            {
                case Modes.Efficiency:
                    m_Graph.onRuntimeDataChanged += UpdateDebugMode;
                    Efficiency();
                    break;
                case Modes.Alive:
                    m_Graph.onRuntimeDataChanged += UpdateDebugMode;
                    Alive();
                    break;
                case Modes.None:
                    None();
                    break;
            }
        }

        private void UpdateDebugMode()
        {
            switch (m_CurrentMode)
            {
                case Modes.Efficiency:
                    RegisterParticleSystems();
                    InitSystemInfoArray();
                    break;
                case Modes.Alive:
                    RegisterParticleSystems();
                    InitSystemInfoArray();
                    break;
                default:
                    break;
            }
        }

        private void UpdateDebugMode(VFXGraph graph)
        {
            //Update now...
            UpdateDebugMode();

            //.. but in some case, the onRuntimeDataChanged is called too soon, need to update twice
            //because VFXUIDebug relies on VisualEffect : See m_VFX.GetParticleSystemNames
            m_View.schedule.Execute(UpdateDebugMode).ExecuteLater(0 /* next frame */);
        }

        public void SetVisualEffect(VisualEffect vfx)
        {
            if (m_VFX != vfx)
            {
                m_VFX = vfx;
                if (m_Curves != null)
                    m_Curves.OnVFXChange();
            }
        }

        public void Notify(Events e)
        {
            switch (e)
            {
                case Events.VFXReset:
                    InitSystemInfoArray();
                    break;
                case Events.VFXStop:
                    InitSystemInfoArray();
                    break;
                default:
                    break;
            }
            if (m_Curves != null)
                m_Curves.Notify(e);
        }

        static Color GetColor(int i)
        {
            return Color.HSVToRGB((i * 0.618033988749895f) % 1.0f, 0.6f, 1.0f).gamma;
        }

        void RegisterParticleSystems()
        {
            if (m_SystemInfos != null)
            {
                foreach (var SystemInfo in m_SystemInfos.Values)
                {
                    SystemInfo[0].Clear();
                    m_SystemInfosContainer.Remove(SystemInfo[0]);
                }
                m_SystemInfos.Clear();
            }
            else
                m_SystemInfos = new Dictionary<int, VisualElement[]>();

            if (m_VFX != null)
            {
                List<string> particleSystemNames = new List<string>();
                m_VFX.GetParticleSystemNames(particleSystemNames);
                m_GpuSystems.Clear();
                int i = 0;
                foreach (var name in particleSystemNames)
                {
                    int id = Shader.PropertyToID(name);
                    m_GpuSystems.Add(id);
                    AddSystemInfoEntry(name, id, GetColor(i));

                    ++i;
                }

                m_Curves.OnVFXChange();
            }
        }

        void ToggleAll(ChangeEvent<bool> evt)
        {
            foreach (var SystemInfo in m_SystemInfos.Values)
            {
                var toggle = SystemInfo[1] as Toggle;
                if (toggle != null)
                    toggle.value = evt.newValue;
            }
        }

        void None()
        {
            if (m_DebugButton != null)
                m_DebugButton.text = "Debug modes";
        }

        void Efficiency()
        {
            // ui
            m_DebugButton.text = "Efficiency Plot";
            m_Curves = new CurveContent(this, (int)(10.0f / 0.016f), 16);
            m_ComponentBoard.contentContainer.Add(m_Curves);

            var Yaxis = SetYAxis("100%", "50%", "0%");
            m_DebugDrawingBox = SetDebugDrawingBox();
            var settingsBox = SetSettingsBox();
            var plotArea = SetPlotArea(m_DebugDrawingBox, Yaxis);
            var title = SetSystemInfosTitle();
            m_SystemInfosContainer = new VisualElement { name = "debug-system-stat-container" };

            m_DebugContainer.Add(settingsBox);
            m_DebugContainer.Add(plotArea);
            m_DebugContainer.Add(title);
            m_DebugContainer.Add(m_SystemInfosContainer);

            // recover debug data
            RegisterParticleSystems();
        }

        void Alive()
        {
            // ui
            m_DebugButton.text = "Alive Particles Count Plot";
            m_Curves = new CurveContent(this, (int)(10.0f / 0.016f), 16);
            m_ComponentBoard.contentContainer.Add(m_Curves);

            var Yaxis = SetYAxis("", "", "0");
            m_DebugDrawingBox = SetDebugDrawingBox();
            var settingsBox = SetSettingsBox();
            var plotArea = SetPlotArea(m_DebugDrawingBox, Yaxis);
            var title = SetSystemInfosTitle();
            m_SystemInfosContainer = new VisualElement { name = "debug-system-stat-container" };

            m_DebugContainer.Add(settingsBox);
            m_DebugContainer.Add(plotArea);
            m_DebugContainer.Add(title);
            m_DebugContainer.Add(m_SystemInfosContainer);

            // recover debug data
            RegisterParticleSystems();
        }

        VisualElement SetSettingsBox()
        {
            // sampling rate
            var labelSR = new Label();
            labelSR.text = "Sampling rate (ms)";
            labelSR.style.fontSize = 12;
            var fieldSR = new IntegerField();
            fieldSR.value = 16;
            fieldSR.RegisterValueChangedCallback(SetSampleRate);
            var containerSR = new VisualElement();
            containerSR.name = "debug-settings-element-container";
            containerSR.Add(labelSR);
            containerSR.Add(fieldSR);

            // time bar toggle
            var labelTB = new Label();
            labelTB.text = "Toggle time bars";
            labelTB.style.fontSize = 12;
            var toggleTB = new Toggle();
            toggleTB.RegisterValueChangedCallback(ToggleTimeBars);
            toggleTB.value = true;
            toggleTB.style.justifyContent = Justify.Center;
            var containerTB = new VisualElement();
            containerTB.name = "debug-settings-element-container";
            containerTB.Add(labelTB);
            containerTB.Add(toggleTB);

            var settingsContainer = new VisualElement();
            settingsContainer.name = "debug-settings-container";
            settingsContainer.Add(containerSR);
            settingsContainer.Add(containerTB);
            return settingsContainer;
        }

        void SetSampleRate(ChangeEvent<int> e)
        {
            var intergerField = e.currentTarget as IntegerField;
            if (intergerField != null)
            {
                if (e.newValue < 1)
                    intergerField.value = 1;

                m_Curves.SetSamplingRate(intergerField.value);
            }
        }

        void ToggleTimeBars(ChangeEvent<bool> e)
        {
            var toggle = e.currentTarget as Toggle;
            if (toggle != null)
            {
                m_Curves.SetDrawTimeBars(e.newValue);
            }
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

            m_YaxisElts = new TextElement[3];
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

        VisualElement SetSystemInfosTitle()
        {
            var toggleAll = new Toggle();
            toggleAll.value = true;
            toggleAll.RegisterValueChangedCallback(ToggleAll);

            var SystemInfoName = new TextElement();
            SystemInfoName.name = "debug-system-stat-title-name";
            SystemInfoName.text = "Particle System";
            SystemInfoName.tooltip = "Click on a name to focus the corresponding particle system.";

            var SystemInfoAlive = new TextElement();
            SystemInfoAlive.name = "debug-system-stat-title";
            SystemInfoAlive.text = "Alive";

            var SystemInfoMaxAlive = new TextElement();
            SystemInfoMaxAlive.name = "debug-system-stat-title";
            SystemInfoMaxAlive.text = "Set Capacity";
            SystemInfoMaxAlive.tooltip = "Set a particle system capacity based on the maximum number of alive particles since recording started.";

            var SystemInfoEfficiency = new TextElement();
            SystemInfoEfficiency.name = "debug-system-stat-title";
            SystemInfoEfficiency.text = "Efficiency";

            var titleContainer = new VisualElement { name = "debug-system-stat-entry-container" };
            titleContainer.Add(toggleAll);
            titleContainer.Add(SystemInfoName);
            titleContainer.Add(SystemInfoAlive);
            titleContainer.Add(SystemInfoMaxAlive);
            titleContainer.Add(SystemInfoEfficiency);

            return titleContainer;
        }

        void AddSystemInfoEntry(string systemName, int id, Color color)
        {
            var statContainer = new VisualElement { name = "debug-system-stat-entry-container" };
            statContainer.AddToClassList("row");
            m_SystemInfosContainer.Add(statContainer);

            var toggle = new Toggle();
            toggle.value = true;

            var name = new Button();
            name.name = "debug-system-stat-entry-name";
            name.text = systemName;
            name.style.color = color;
            name.style.backgroundColor = new Color(0.16f, 0.16f, 0.16f);
            name.clickable.clicked += FocusParticleSystem(systemName);

            var alive = new TextElement();
            alive.name = "debug-system-stat-entry";
            alive.text = " - ";

            bool isSystemInSubgraph;
            var setCapacity = CapacitySetter(systemName, out isSystemInSubgraph);
            VisualElement maxAlive;
            if (isSystemInSubgraph)
            {
                var maxAliveButton = new Button { name = "debug-system-stat-entry" };
                maxAliveButton.tooltip = "Set system capacity";
                maxAliveButton.text = "0";
                maxAliveButton.SetEnabled(m_Graph.visualEffectResource != null && m_Graph.visualEffectResource.IsAssetEditable());
                maxAliveButton.clickable.clickedWithEventInfo += setCapacity;
                maxAlive = maxAliveButton;
            }
            else
            {
                var maxAliveText = new TextElement();
                maxAliveText.name = "debug-system-stat-entry";
                maxAliveText.text = "0";
                maxAlive = maxAliveText;
            }

            var efficiency = new TextElement();
            efficiency.name = "debug-system-stat-entry";
            efficiency.text = " - ";

            statContainer.Add(toggle);
            statContainer.Add(name);
            statContainer.Add(alive);
            statContainer.Add(maxAlive);
            statContainer.Add(efficiency);

            var stats = new VisualElement[6];
            stats[0] = statContainer;
            stats[1] = toggle;
            stats[2] = name;
            stats[3] = alive;
            stats[4] = maxAlive;
            stats[5] = efficiency;

            m_SystemInfos[id] = stats;
        }

        Action FocusParticleSystem(string systemName)
        {
            var visibleSystems = m_View.systems;

            Func<Experimental.GraphView.GraphElement, Action> focus = (elt) =>
            {
                return () =>
                {
                    Rect rectToFit = elt.GetPosition();
                    var frameTranslation = Vector3.zero;
                    var frameScaling = Vector3.one;

                    if (rectToFit.width <= 50 || rectToFit.height <= 50)
                    {
                        return;
                    }

                    VFXView.CalculateFrameTransform(rectToFit, m_View.layout, 30, out frameTranslation, out frameScaling);
                    Matrix4x4.TRS(frameTranslation, Quaternion.identity, frameScaling);
                    m_View.UpdateViewTransform(frameTranslation, frameScaling);
                    m_View.contentViewContainer.MarkDirtyRepaint();
                };
            };

            foreach (var system in visibleSystems)
            {
                if (system.controller.title == systemName)
                {
                    return focus(system);
                }
            }

            var subgraphs = m_View.GetAllContexts().Where(c => c.controller.model.contextType == VFXContextType.Subgraph);

            var models = new HashSet<ScriptableObject>();
            foreach (var subgraph in subgraphs)
            {
                models.Clear();
                subgraph.controller.model.CollectDependencies(models, false);
                if (models.OfType<VFXData>().Any(x => m_View.controller.graph.systemNames.GetUniqueSystemName(x) == systemName))
                {
                    return focus(subgraph);
                }
            }

            return () => { };
        }

        Action<EventBase> CapacitySetter(string systemName, out bool isSystemInSubGraph)
        {
            var system = m_View.GetAllContexts()
                .Select(x => x.controller.model.GetData())
                .OfType<VFXDataParticle>()
                .FirstOrDefault(x => m_Graph.systemNames.GetUniqueSystemName(x) == systemName);

            if (system != null)
            {
                isSystemInSubGraph = true;
                return (e) =>
                {
                    if (m_Graph.visualEffectResource != null && !m_Graph.visualEffectResource.IsAssetEditable())
                        return; //The button should be disabled but state update can have a delay

                    if (e.currentTarget is Button button)
                        system.SetSettingValue("capacity", (uint)(float.Parse(button.text) * 1.01f));
                };

            }
            isSystemInSubGraph = false;
            return (e) => { };
        }

        void UpdateSystemInfoEntry(int systemId, VFXParticleSystemInfo stat)
        {
            var statUI = m_SystemInfos[systemId];// [0] is title bar
            if (statUI[3] is TextElement alive)
                alive.text = stat.sleeping ? "Sleeping" : stat.aliveCount.ToString();
            if (statUI[4] is TextElement maxAliveText)
            {
                maxAliveText.SetEnabled(m_Graph.visualEffectResource != null && m_Graph.visualEffectResource.IsAssetEditable());
                uint.TryParse(maxAliveText.text, out uint maxAlive);
                maxAliveText.text = Math.Max(maxAlive, stat.aliveCount).ToString("D");
            }
            if (statUI[5] is TextElement efficiency)
            {
                var eff = (int)((float)stat.aliveCount * 100.0f / (float)stat.capacity);
                efficiency.text = stat.sleeping ? "Sleeping" : string.Format("{0} %", eff);
                if (eff < 51)
                    efficiency.style.color = Color.red.gamma;
                else if (eff < 91)
                    efficiency.style.color = new Color(1.0f, 0.5f, 0.32f).gamma;
                else
                    efficiency.style.color = Color.green.gamma;
            }
        }

        void InitSystemInfoArray()
        {
            if (m_SystemInfos != null)
                foreach (var statUI in m_SystemInfos.Values)
                {
                    if (statUI[3] is TextElement alive)
                        alive.text = " - ";
                    if (statUI[4] is TextElement maxAlive)
                        maxAlive.text = "0";
                    if (statUI[5] is TextElement efficiency)
                        efficiency.text = " - ";
                }
        }

        public void Clear()
        {
            m_Graph.onRuntimeDataChanged -= UpdateDebugMode;

            if (m_ComponentBoard != null && m_Curves != null)
                m_ComponentBoard.contentContainer.Remove(m_Curves);
            m_ComponentBoard = null;
            m_Curves = null;

            if (m_SystemInfosContainer != null)
                m_SystemInfosContainer.Clear();

            m_YaxisElts = null;

            if (m_DebugContainer != null)
                m_DebugContainer.Clear();


            m_SystemInfos = null;
            m_DebugDrawingBox = null;
            m_SystemInfosContainer = null;
            m_DebugContainer = null;
        }
    }
}
