using System;
using System.IO;
using UnityEngine;

class EditorLogWatcher : IDisposable
{
    const string editorLogFileName = "Editor.log";

    public delegate void OnLogWriteCallback(string newLines);

    OnLogWriteCallback  logWriteCallback;
    FileStream          logStream;

    public EditorLogWatcher(OnLogWriteCallback callback)
    {
        logWriteCallback = callback;

        logStream = new FileStream(GetEditorLogPath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1 * 1024 * 1024, FileOptions.RandomAccess | FileOptions.SequentialScan);
        logStream.Seek(logStream.Length, SeekOrigin.Begin);
    }

    string GetEditorLogPath()
    {
        var args = Environment.GetCommandLineArgs();

        // In case we have a -logFile argument, then we can get the file path from here
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-logFile" && i + 1 > args.Length)
                return args[i + 1];
        }

        // platform dependent editor log location
#if UNITY_EDITOR_WIN
        return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Unity\Editor\Editor.log";
#elif UNITY_EDITOR_OSX
        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)  + @"/Library/Logs/Unity/Editor.log";
#else
        return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/unity3d/Editor.log";
#endif
    }

    string GetEditorLogFolderPath() => Path.GetDirectoryName(GetEditorLogPath());

    public void Dispose()
    {
        using (var s = new StreamReader(logStream))
        {
            while (!s.EndOfStream)
                logWriteCallback?.Invoke(s.ReadLine());
        }
    }
}