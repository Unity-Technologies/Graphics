using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

public class ScriptableRendererFeatureTests
{
    // Custom log handler is needed because the default one misses LogErrors that happen during RP constructor due to
    // how domain reload works with the test framework. This handler will notice the errors and fail the test inside
    // the test body if any were logged (for example if RTHandles.Initialize logs an error).
    class ErrorLogHandler : ILogHandler, IDisposable
    {
        ILogHandler m_OriginalHandler;
        public string errorMessage { get; private set; }
        public bool hasError => !string.IsNullOrEmpty(errorMessage);

        public ErrorLogHandler()
        {
            m_OriginalHandler = Debug.unityLogger.logHandler;
            Debug.unityLogger.logHandler = this;
        }

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            if (logType == LogType.Error)
            {
                // Defer assertion to ensure we are not in the middle of RP constructor or something
                errorMessage = string.Format(format, args);
            }
            else
            {
                m_OriginalHandler.LogFormat(logType, context, format, args);
            }
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            m_OriginalHandler.LogException(exception, context);
        }

        public void Dispose()
        {
            Debug.unityLogger.logHandler = m_OriginalHandler;
            m_OriginalHandler = null;

            errorMessage = null;
        }
    }

    ErrorLogHandler m_ErrorLogHandler;

    [SetUp]
    public void Setup()
    {
        m_ErrorLogHandler = new ErrorLogHandler();
    }

    [TearDown]
    public void TearDown()
    {
        m_ErrorLogHandler.Dispose();
    }

    [UnityTest]
    public IEnumerator AllocRTHandleInCreate_EnterExitPlayModeWithoutErrors()
    {
        yield return new EnterPlayMode();
        yield return null;

        yield return new ExitPlayMode();
        yield return null;

        if (m_ErrorLogHandler.hasError)
            Assert.Fail(m_ErrorLogHandler.errorMessage);
    }
}
