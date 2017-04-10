using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class VFXComponentDebugPanel
{
    private enum DebugStatMode
    {
        Efficiency = 0
    }

    private VFXComponent m_Component;

    private DebugStatMode m_DebugStatMode = DebugStatMode.Efficiency;
    private VFXSystemComponentStat[] m_DebugStatCurrentData;
    private List<uint>[] m_DebugStatAliveHistory;
    private uint[] m_DebugStatMaxCapacity;
    private double m_DebugStatLastUpdateTime;

    private static Color[] s_DebugStatCurveColors;


    private const uint DEBUGSTAT_CURVE_COLOR_COUNT = 25;
    private const float DEBUGSTAT_UPDATE_INTERVAL = 0.033f;
    private const uint DEBUGSTAT_NUM_SAMPLES = 100;

    private Styles m_Styles;

    public VFXComponentDebugPanel(VFXComponent component)
    {
        m_Component = component;
        m_Styles = new Styles();
    }

    public void OnSceneGUI()
    {
        var stats = m_DebugStatCurrentData;

        //Basic display BBox
        var debugColors = new Color[] { Color.magenta, Color.green, Color.cyan, Color.yellow };
        for (int iStat = 0; iStat < stats.Length; ++iStat)
        {
            var stat = stats[iStat];

            var transform = m_Component.GetComponent<Transform>();
            var bckpMatrix = Handles.matrix;
            Handles.matrix = transform.localToWorldMatrix;
            Handles.color = debugColors[iStat % debugColors.Length];
            Handles.DrawWireCube(stat.localBounds.center, stat.localBounds.extents * 2);
            Handles.matrix = bckpMatrix;
        }
    }


    public void OnWindowGUI()
    {
        Rect debugWindowRect = new Rect(10, 28, 480, 300 + m_DebugStatCurrentData.Length * 16);
        GUI.Window(667, debugWindowRect, DrawDebugControlsWindow, "VFX Debug Information");
    }


    public void InitDebug(int count, VFXSystemComponentStat[] stats)
    {
        m_DebugStatCurrentData = stats;
        m_DebugStatAliveHistory = new List<uint>[count];
        m_DebugStatMaxCapacity = new uint[count];

        if(s_DebugStatCurveColors == null)
        {
            s_DebugStatCurveColors = new Color[DEBUGSTAT_CURVE_COLOR_COUNT];
            for(int i = 0; i < DEBUGSTAT_CURVE_COLOR_COUNT; i++)
                s_DebugStatCurveColors[i] = Color.HSVToRGB( (0.71405f+i*0.37135766f) % 1.0f , 0.5f , 1 );
        }

        for(int i = 0; i < count; i++)
        {
            m_DebugStatAliveHistory[i] = new List<uint>();
            m_DebugStatMaxCapacity[i] = 0;
        }
    }

    public void UpdateDebugData()
    {
        double time = Time.time; //EditorApplication.timeSinceStartup;

        var stats = VFXComponent.GetSystemComponentsStatsFilter(m_Component);

        // First frame, or system count changed
        if (m_DebugStatCurrentData == null || m_DebugStatCurrentData.Length != stats.Length)
        {
            InitDebug(stats.Length, stats);
        }


        if(time - m_DebugStatLastUpdateTime > DEBUGSTAT_UPDATE_INTERVAL)
        {

            for(int i = 0; i < m_DebugStatMaxCapacity.Length; i++)
            {
                if (m_DebugStatAliveHistory[i].Count > DEBUGSTAT_NUM_SAMPLES)
                {
                    m_DebugStatAliveHistory[i].RemoveAt(0);
                }
                m_DebugStatAliveHistory[i].Add(m_DebugStatCurrentData[i].alive);

                m_DebugStatMaxCapacity[i] = Math.Max(m_DebugStatCurrentData[i].capacity, m_DebugStatMaxCapacity[i]);
            }

            m_DebugStatCurrentData = stats;
            m_DebugStatLastUpdateTime = time;
        }
        else
        {
            for(int i = 0; i < m_DebugStatMaxCapacity.Length; i++)
            {
                m_DebugStatCurrentData[i].alive = Math.Max(stats[i].alive, m_DebugStatCurrentData[i].alive);
                m_DebugStatCurrentData[i].spawnRequest += stats[i].spawnRequest;
            }
        }
    }

    public void DrawDebugControlsWindow(int windowID)
    {
        using (new GUILayout.VerticalScope())
        {
            if (m_DebugStatCurrentData.Length > 0)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Debug Mode", GUILayout.Width(120));
                    m_DebugStatMode = (DebugStatMode)EditorGUILayout.EnumPopup(m_DebugStatMode);
                    GUILayout.Space(32);
                }


                GUILayout.Space(16);

                Rect curveRect;

                using (new GUILayout.HorizontalScope(GUILayout.Height(200)))
                {
                    curveRect = GUILayoutUtility.GetRect(400, 200);
                    using (new GUILayout.VerticalScope(GUILayout.Width(32)))
                    {
                        GUILayout.Label("100%");
                        GUILayout.FlexibleSpace();
                        GUILayout.Label("75%");
                        GUILayout.FlexibleSpace();
                        GUILayout.Label("50%");
                        GUILayout.FlexibleSpace();
                        GUILayout.Label("25%");
                        GUILayout.FlexibleSpace();
                        GUILayout.Label("0%");
                    }
                }

                Handles.DrawSolidRectangleWithOutline(curveRect,new Color(0.1f,0.1f,0.1f,1.0f), new Color(0.2f,0.2f,0.2f,1.0f));
                Handles.color = new Color(0.2f, 0.2f, 0.2f, 1.0f);
                Handles.DrawLine(new Vector3(curveRect.xMin, curveRect.yMin + 0.25f * curveRect.height),new Vector3(curveRect.xMax, curveRect.yMin + 0.25f * curveRect.height));
                Handles.DrawLine(new Vector3(curveRect.xMin, curveRect.yMin + 0.5f * curveRect.height),new Vector3(curveRect.xMax, curveRect.yMin + 0.5f * curveRect.height));
                Handles.DrawLine(new Vector3(curveRect.xMin, curveRect.yMin + 0.75f * curveRect.height),new Vector3(curveRect.xMax, curveRect.yMin + 0.75f * curveRect.height));
                Handles.color = Color.white;

                for(int i = 0; i < m_DebugStatMaxCapacity.Length; i++)
                {
                    Vector3[] points = new Vector3[m_DebugStatAliveHistory[i].Count];
                    for (int j = 0 ; j < m_DebugStatAliveHistory[i].Count; j++)
                    {
                        points[j] = new Vector3(curveRect.position.x + curveRect.width*(float)j/DEBUGSTAT_NUM_SAMPLES, curveRect.position.y + curveRect.height * (1-(float)m_DebugStatAliveHistory[i][j]/m_DebugStatMaxCapacity[i]));
                    }
                    Handles.matrix = GUI.matrix;
                    Handles.color = s_DebugStatCurveColors[i % DEBUGSTAT_CURVE_COLOR_COUNT];
                    Handles.DrawAAPolyLine(4,points);
                    Handles.color = Color.white;
                }

                GUILayout.Space(16);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("#", m_Styles.entryHeader, GUILayout.Width(32));
                    GUILayout.Label("Alive", m_Styles.entryHeader, GUILayout.Width(80));
                    GUILayout.Label("Allocated", m_Styles.entryHeader, GUILayout.Width(80));
                    GUILayout.Label("Spawned", m_Styles.entryHeader, GUILayout.Width(80));
                    GUILayout.Label("Overflow", m_Styles.entryHeader, GUILayout.Width(80));
                    GUILayout.Box(GUIContent.none, m_Styles.entryHeader, GUILayout.ExpandWidth(true));
                    GUILayout.Label("Efficiency", m_Styles.entryHeader, GUILayout.Width(80));
                }


                var allsum = m_DebugStatCurrentData.Aggregate((a, b) => new VFXSystemComponentStat { alive = a.alive + b.alive, capacity = a.capacity + b.capacity });

                using (new GUILayout.HorizontalScope())
                {
                    GUI.contentColor = Color.white;
                    GUILayout.Label("ALL", m_Styles.entryEven, GUILayout.Width(32));
                    GUILayout.Label(allsum.alive.ToString(), m_Styles.entryEven, GUILayout.Width(80));
                    GUILayout.Label(allsum.capacity.ToString(), m_Styles.entryEven, GUILayout.Width(80));
                    GUILayout.Box(GUIContent.none, m_Styles.entryEven, GUILayout.ExpandWidth(true));

                    float percentage = (float)allsum.alive / allsum.capacity;
                    GUI.contentColor = percentage < 0.33f ? Color.red : percentage < 0.66f ? Color.yellow : Color.green;
                    GUILayout.Label((int)(percentage*100)+"%", m_Styles.entryEven, GUILayout.Width(80));
                }

                for(int i = 0; i < m_DebugStatMaxCapacity.Length; i++)
                {
                    GUIStyle s = (i % 2 == 0) ? m_Styles.entryOdd : m_Styles.entryEven ;

                    using (new GUILayout.HorizontalScope())
                    {
                        GUI.contentColor = s_DebugStatCurveColors[i % DEBUGSTAT_CURVE_COLOR_COUNT];
                        GUILayout.Label("#"+(i+1), s, GUILayout.Width(32));
                        GUILayout.Label(m_DebugStatCurrentData[i].alive.ToString(), s, GUILayout.Width(80));
                        GUILayout.Label(m_DebugStatCurrentData[i].capacity.ToString(), s, GUILayout.Width(80));
                        GUILayout.Label(m_DebugStatCurrentData[i].spawnRequest.ToString(), s, GUILayout.Width(80));

                        int overflow = Math.Max((int)m_DebugStatCurrentData[i].alive + (int) m_DebugStatCurrentData[i].spawnRequest - (int) m_DebugStatCurrentData[i].capacity,0);
                        GUILayout.Label(overflow.ToString(), s, GUILayout.Width(80));

                        GUILayout.Box(GUIContent.none, s, GUILayout.ExpandWidth(true));

                        float percentage = (float)m_DebugStatCurrentData[i].alive / m_DebugStatCurrentData[i].capacity;
                        GUI.contentColor = percentage < 0.33f ? Color.red : percentage < 0.66f ? Color.yellow : Color.green;
                        GUILayout.Label((int)(percentage*100)+"%", s, GUILayout.Width(80));
                    }
                }

            }
        }

        // Handle click in window to avoid unselecting asset
        if (Event.current.type == EventType.mouseDown)
            Event.current.Use();
    }

    private class Styles
    {
        public GUIStyle InspectorHeader;
        public GUIStyle ToggleGizmo;

        public GUILayoutOption MiniButtonWidth = GUILayout.Width(48);
        public GUILayoutOption PlayControlsHeight = GUILayout.Height(24);

        public GUIStyle entryEven;
        public GUIStyle entryOdd;
        public GUIStyle entryHeader;

        public Styles()
        {
            InspectorHeader = new GUIStyle("ShurikenModuleTitle");
            InspectorHeader.fontSize = 12;
            InspectorHeader.fontStyle = FontStyle.Bold;
            InspectorHeader.contentOffset = new Vector2(2,-2);
            InspectorHeader.border = new RectOffset(4, 4, 4, 4);
            InspectorHeader.overflow = new RectOffset(4, 4, 4, 4);
            InspectorHeader.margin = new RectOffset(4, 4, 16, 8);

            Texture2D showIcon = EditorGUIUtility.Load("VisibilityIcon.png") as Texture2D;
            Texture2D hideIcon = EditorGUIUtility.Load("VisibilityIconDisabled.png") as Texture2D;

            ToggleGizmo = new GUIStyle();
            ToggleGizmo.margin = new RectOffset(0, 0, 4, 0);
            ToggleGizmo.active.background = hideIcon;
            ToggleGizmo.onActive.background = showIcon;
            ToggleGizmo.normal.background = hideIcon;
            ToggleGizmo.onNormal.background = showIcon;
            ToggleGizmo.focused.background = hideIcon;
            ToggleGizmo.onFocused.background = showIcon;
            ToggleGizmo.hover.background = hideIcon;
            ToggleGizmo.onHover.background = showIcon;

            entryEven = new GUIStyle("OL EntryBackEven");
            entryEven.margin = new RectOffset();
            entryEven.contentOffset = new Vector2();
            entryEven.padding = new RectOffset(8,0,0,0);
            entryOdd = new GUIStyle("OL EntryBackOdd");
            entryOdd.margin = new RectOffset();
            entryOdd.contentOffset = new Vector2();
            entryOdd.padding = new RectOffset(8,0,0,0);
            entryHeader = new GUIStyle(entryOdd);
            entryHeader.fontStyle = FontStyle.Bold;

        }
    }

}
