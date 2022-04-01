using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics
{
    public class GraphicsTestSettingsWindow : EditorWindow
    {
        private Rect menuBar;
        private Rect upperPanel;
        private Rect lowerPanel;
        private Rect resizer;

        private Texture2D errorIcon;
        private Texture2D warningIcon;
        private Texture2D infoIcon;
        private Texture2D boxBgOdd;
        private Texture2D boxBgEven;
        private Texture2D boxBgSelected;
        private Texture2D icon;

        float secs;
        float t = 0f;
        private float menuBarHeight = 20f;
        private float sizeRatio = 0.7f;
        private float resizerHeight = 5f;
        private bool isResizing;

        private GUIStyle resizerStyle;
        private GUIStyle textAreaStyle;
        private GUIStyle boxStyle;

        private Vector2 upperPanelScroll;
        private Vector2 lowerPanelScroll;
        static bool m_optimized = false;
        static string m_text;
        static string m_result;

        static BaseImageOptimization m_baseImageOptimization;

        private List<Log> logs;
        private Log selectedLog;

        static BaseImageOptimization GetOptimizer
        {
            get
            {
                if (m_baseImageOptimization == null)
                {
                    m_baseImageOptimization = new BaseImageOptimization();
                }
                return m_baseImageOptimization;
            }
        }

        [MenuItem("Window/General/Graphics Test Setup")]
        private static void OpenWindow()
        {
            GraphicsTestSettingsWindow window = GetWindow<GraphicsTestSettingsWindow>();
            window.titleContent = new GUIContent("Graphics Test Setup");
        }

        private void OnEnable()
        {
            errorIcon = EditorGUIUtility.Load("icons/console.erroricon.sml.png") as Texture2D;
            warningIcon = EditorGUIUtility.Load("icons/console.warnicon.sml.png") as Texture2D;
            infoIcon = EditorGUIUtility.Load("icons/console.infoicon.sml.png") as Texture2D;

            resizerStyle = new GUIStyle();
            resizerStyle.normal.background = EditorGUIUtility.Load("icons/d_AvatarBlendBackground.png") as Texture2D;

            boxStyle = new GUIStyle();
            boxStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            boxBgOdd = EditorGUIUtility.Load("builtin skins/darkskin/images/cn entrybackodd.png") as Texture2D;
            boxBgEven = EditorGUIUtility.Load("builtin skins/darkskin/images/cnentrybackeven.png") as Texture2D;
            boxBgSelected = EditorGUIUtility.Load("builtin skins/darkskin/images/menuitemhover.png") as Texture2D;

            textAreaStyle = new GUIStyle();
            textAreaStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            textAreaStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/projectbrowsericonareabg.png") as Texture2D;

            logs = new List<Log>();
            selectedLog = null;

            m_optimized = GetOptimizer.IsOptimized;

            BaseImageOptimization.logMessageReceived += LogMessageReceived;
        }

        private void OnGUI()
        {
            DrawMenuBar();
            DrawUpperPanel();
            DrawLowerPanel();
            DrawResizer();

            ProcessEvents(Event.current);

            if (GUI.changed) Repaint();

        }
        private void DrawMenuBar()
        {
            menuBar = new Rect(0, 0, position.width, menuBarHeight);

            GUILayout.BeginArea(menuBar, EditorStyles.toolbar);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Start Optimization"), GUILayout.Width(130)))
            {
                m_result = GetOptimizer.Run();
                m_optimized = GetOptimizer.IsOptimized;
                System.Threading.Thread.Sleep(500);
                EditorUtility.ClearProgressBar();
            }

            m_text = "Optimization status : " + m_optimized;
            m_text = GUILayout.TextArea(m_text);
            GUILayout.FlexibleSpace();
            GUILayout.TextArea(m_result);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawUpperPanel()
        {
            upperPanel = new Rect(0, menuBarHeight, position.width, position.height * sizeRatio);

            GUILayout.BeginArea(upperPanel);
            upperPanelScroll = GUILayout.BeginScrollView(upperPanelScroll);

            for (int i = 0; i < logs.Count; i++)
            {
                if (DrawBox(logs[i].info, logs[i].type, i % 2 == 0, logs[i].isSelected))
                {
                    if (selectedLog != null)
                    {
                        selectedLog.isSelected = false;
                    }

                    logs[i].isSelected = true;
                    selectedLog = logs[i];
                    GUI.changed = true;
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawLowerPanel()
        {
            lowerPanel = new Rect(0, (position.height * sizeRatio) + resizerHeight, position.width, (position.height * (1 - sizeRatio)) - resizerHeight);

            GUILayout.BeginArea(lowerPanel);
            lowerPanelScroll = GUILayout.BeginScrollView(lowerPanelScroll);

            if (selectedLog != null)
            {
                GUILayout.TextArea(selectedLog.info, textAreaStyle);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private bool DrawBox(string content, LogType boxType, bool isOdd, bool isSelected)
        {
            if (isSelected)
            {
                boxStyle.normal.background = boxBgSelected;
            }
            else
            {
                if (isOdd)
                {
                    boxStyle.normal.background = boxBgOdd;
                }
                else
                {
                    boxStyle.normal.background = boxBgEven;
                }
            }

            switch (boxType)
            {
                case LogType.Error: icon = errorIcon; break;
                case LogType.Exception: icon = errorIcon; break;
                case LogType.Assert: icon = errorIcon; break;
                case LogType.Warning: icon = warningIcon; break;
                case LogType.Log: icon = infoIcon; break;
            }

            return GUILayout.Button(new GUIContent(content, icon), boxStyle, GUILayout.ExpandWidth(true), GUILayout.Height(30));
        }

        private void DrawResizer()
        {
            resizer = new Rect(0, (position.height * sizeRatio) - 5f, position.width, 10f);

            GUILayout.BeginArea(new Rect(resizer.position + (Vector2.up * 5f), new Vector2(position.width, 2)), resizerStyle);
            GUILayout.EndArea();

            EditorGUIUtility.AddCursorRect(resizer, MouseCursor.ResizeVertical);
        }

        private void OnDisable()
        {
            BaseImageOptimization.logMessageReceived -= LogMessageReceived;
        }

        private void OnDestroy()
        {
            BaseImageOptimization.logMessageReceived -= LogMessageReceived;
        }

        private void LogMessageReceived(string stackTrace, LogType type, float progress)
        {
            EditorUtility.DisplayProgressBar("Reference images optimization", stackTrace, progress);

            stackTrace = System.DateTime.Now.ToLongTimeString() + ": " + stackTrace;
            Log l = new Log(false, stackTrace, type);
            logs.Add(l);
            t += 0.5f;
        }

        private void ProcessEvents(Event e)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 && resizer.Contains(e.mousePosition))
                    {
                        isResizing = true;
                    }
                    break;

                case EventType.MouseUp:
                    isResizing = false;
                    break;
            }

            Resize(e);
        }

        private void Resize(Event e)
        {
            if (isResizing)
            {
                sizeRatio = e.mousePosition.y / position.height;
                Repaint();
            }
        }
    }
}
public class Log
{
    public bool isSelected;
    public string info;
    public LogType type;

    public Log(bool isSelected, string info, LogType type)
    {
        this.isSelected = isSelected;
        this.info = info;
        this.type = type;
    }
}
