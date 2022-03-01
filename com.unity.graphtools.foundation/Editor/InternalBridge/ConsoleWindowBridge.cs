using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Bridge
{
    static class ConsoleWindowBridge
    {
        static Action<int> s_RemoveLogEntriesByIdentifierDelegate;
        static readonly int k_GTFLogIdentifier = "GraphToolsFoundation".GetHashCode();

        // TODO: This is taken directly from Runtime\Logging\LogAssert.h.  There is no C# equivalent in the editor
        // so when the native enum changes, this should be updated as well.  Unfortunately such changes are very difficult to
        // intercept without modifying the editor as well.  Pray for no/minimal little changes of the native API.
        // Note that some values are intentionally unused but still here for clarity.  Value names are left unchanged
        // from editor, hence the following SuppressMessage attributes.
        [Flags]
        enum LogMessageFlags
        {
            NoLogMessageFlags = 0,
            Error = 1 << 0, // Message describes an error.
            Assert = 1 << 1, // Message describes an assertion failure.
            Log = 1 << 2, // Message is a general log message.
            Fatal = 1 << 4, // Message describes a fatal error, and that the program should now exit.
            AssetImportError = 1 << 6, // Message describes an error generated during asset importing.
            AssetImportWarning = 1 << 7, // Message describes a warning generated during asset importing.
            ScriptingError = 1 << 8, // Message describes an error produced by script code.
            ScriptingWarning = 1 << 9, // Message describes a warning produced by script code.
            ScriptingLog = 1 << 10, // Message describes a general log message produced by script code.
            ScriptCompileError = 1 << 11, // Message describes an error produced by the script compiler.
            ScriptCompileWarning = 1 << 12, // Message describes a warning produced by the script compiler.
            StickyLog = 1 << 13, // Message is 'sticky' and should not be removed when the user manually clears the console window.
            MayIgnoreLineNumber = 1 << 14, // The scripting runtime should skip annotating the log callstack with file and line information.
            ReportBug = 1 << 15, // When used with kFatal, indicates that the log system should launch the bug reporter.
            DisplayPreviousErrorInStatusBar = 1 << 16, // The message before this one should be displayed at the bottom of Unity's main window, unless there are no messages before this one.
            ScriptingException = 1 << 17, // Message describes an exception produced by script code.
            DontExtractStacktrace = 1 << 18, // Stacktrace extraction should be skipped for this message.
            ScriptingAssertion = 1 << 21, // The message describes an assertion failure in script code.
            StacktraceIsPostprocessed = 1 << 22, // The stacktrace has already been postprocessed and does not need further processing
        };

        static int LogTypeOptionsToLogMessageFlags(LogType logType, LogOption logOptions)
        {
            LogMessageFlags logMessageFlags;

            if (logType == LogType.Log) // LogType::Log
                logMessageFlags = LogMessageFlags.ScriptingLog;
            else if (logType == LogType.Warning) // LogType::Warning
                logMessageFlags = LogMessageFlags.ScriptingWarning;
            else if (logType == LogType.Error) // LogType::Error
                logMessageFlags = LogMessageFlags.ScriptingError;
            else if (logType == LogType.Exception) // LogType::Exception
                logMessageFlags = LogMessageFlags.ScriptingException;
            else
                logMessageFlags = LogMessageFlags.ScriptingAssertion;

            if (logOptions == LogOption.NoStacktrace)
                logMessageFlags |= LogMessageFlags.DontExtractStacktrace;

            return (int)logMessageFlags;
        }

        public static void SetEntryDoubleClickedDelegate(Action<string, int> doubleClickedCallback)
        {
            var types = typeof(EditorWindow).Assembly.GetTypes();
            var consoleWindowType = types.FirstOrDefault(ty => ty.Name == "ConsoleWindow");
            var fields = consoleWindowType?.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
            FieldInfo entryWithManagedCallbackDoubleClicked = fields?.FirstOrDefault(f => f.Name == "entryWithManagedCallbackDoubleClicked");
            if (entryWithManagedCallbackDoubleClicked == null)
                return;

            entryWithManagedCallbackDoubleClicked.SetValue(entryWithManagedCallbackDoubleClicked,
                doubleClickedCallback == null
                ? null
                : (ConsoleWindow.EntryDoubleClickedDelegate)CallEntryDoubleClickedCallback);

            void CallEntryDoubleClickedCallback(LogEntry logEntry) => doubleClickedCallback(logEntry.file, logEntry.instanceID);
        }

        public static void LogSticky(string message, string file, LogType logType, LogOption logOptions, int instanceId)
        {
            int mode = LogTypeOptionsToLogMessageFlags(logType, logOptions) | (int)LogMessageFlags.StickyLog;

            LogEntries.AddMessageWithDoubleClickCallback(new LogEntry
            {
                message = message,
                file = file,
                mode = mode,
                identifier = k_GTFLogIdentifier,
                instanceID = instanceId,
            });
        }

        public static void RemoveLogEntries()
        {
            if (s_RemoveLogEntriesByIdentifierDelegate == null)
            {
                MethodInfo m = typeof(Debug).GetMethod("RemoveLogEntriesByIdentifier", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.IsNotNull(m);
                s_RemoveLogEntriesByIdentifierDelegate = (Action<int>)m.CreateDelegate(typeof(Action<int>));
                Assert.IsNotNull(s_RemoveLogEntriesByIdentifierDelegate);
            }

            s_RemoveLogEntriesByIdentifierDelegate(k_GTFLogIdentifier);
        }

        public static T FindBoundGraphViewToolWindow<T>(VisualElement gv) where T : GraphViewToolWindowBridge
        {
            var guiViews = new List<GUIView>();
            GUIViewDebuggerHelper.GetViews(guiViews);

            FieldInfo fieldInfo = typeof(T).GetField("m_SelectedGraphView", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fieldInfo);

            using (var it = UIElementsUtility.GetPanelsIterator())
            {
                while (it.MoveNext())
                {
                    var dockArea = guiViews.FirstOrDefault(v => v.GetInstanceID() == it.Current.Key) as DockArea;
                    if (dockArea == null)
                        continue;

                    foreach (var graphViewTool in dockArea.m_Panes.OfType<T>())
                    {
                        var usedGv = (VisualElement)fieldInfo.GetValue(graphViewTool);
                        if (usedGv == gv)
                            return graphViewTool;
                    }
                }
            }

            return null;
        }

        public static T SpawnAttachedViewToolWindow<T>(EditorWindow window, VisualElement gv) where T : GraphViewToolWindowBridge
        {
            const int newToolWidth = 200;

            if (!(window.m_Parent is DockArea gvDockArea)) // Should never happen
                return null;

            var container = gvDockArea.parent;
            var originalSize = gvDockArea.position.size;
            var originalWindowPos = gvDockArea.window.position;

            int insertIdx = container.IndexOfChild(gvDockArea);
            container.RemoveChild(gvDockArea);

            var splitView = ScriptableObject.CreateInstance<SplitView>();
            var toolDockArea = ScriptableObject.CreateInstance<DockArea>();
            var toolWindow = ScriptableObject.CreateInstance<T>();

            toolWindow.SelectGraphViewFromWindow(window, gv);

            splitView.AddChild(toolDockArea);
            splitView.AddChild(gvDockArea);

            container.AddChild(splitView, insertIdx);

            toolDockArea.position = new Rect(Vector2.zero, new Vector2(newToolWidth, originalSize.y));
            gvDockArea.position = new Rect(Vector2.zero, new Vector2(originalSize.x - newToolWidth, originalSize.y));
            splitView.position = new Rect(Vector2.zero, originalSize);
            container.window.position = originalWindowPos;

            toolDockArea.AddTab(toolWindow);

            container.Reflow();

            return toolWindow;
        }
    }
}
