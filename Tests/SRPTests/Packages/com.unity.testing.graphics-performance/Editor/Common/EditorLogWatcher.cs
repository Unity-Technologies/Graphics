using System;
using System.IO;
using UnityEngine;

public class EditorLogWatcher : IDisposable
{
    public delegate void OnLogWriteCallback(string newLines);

    OnLogWriteCallback  m_LogWriteCallback;
    FileStream          m_LogStream;

    public EditorLogWatcher(OnLogWriteCallback callback)
    {
        m_LogWriteCallback = callback ?? throw new ArgumentNullException(nameof(callback));

        m_LogStream = new FileStream(GetEditorLogPath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1 * 1024 * 1024, FileOptions.RandomAccess | FileOptions.SequentialScan);
        m_LogStream.Seek(m_LogStream.Length, SeekOrigin.Begin);
    }

    string GetEditorLogPath()
    {
        var args = Environment.GetCommandLineArgs();

        // In case we have a -logFile argument, then we can get the file path from here
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-logFile" && i + 1 < args.Length)
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

    public void Dispose()
    {
        using (var s = new StreamReader(m_LogStream))
        {
            while (!s.EndOfStream)
                m_LogWriteCallback.Invoke(s.ReadLine());
        }
    }
}