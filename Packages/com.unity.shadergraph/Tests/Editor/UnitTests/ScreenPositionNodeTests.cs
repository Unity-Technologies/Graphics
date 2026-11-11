using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    class ScreenPositionNodeTests
    {
        // Other fixtures in this assembly replace Debug.unityLogger.logHandler without restoring it, which hides logs from LogAssert
        sealed class LogCapture : ILogHandler, IDisposable
        {
            readonly ILogHandler m_PreviousHandler;

            public readonly List<(LogType type, string message)> logs = new List<(LogType, string)>();

            public LogCapture()
            {
                m_PreviousHandler = Debug.unityLogger.logHandler;
                Debug.unityLogger.logHandler = this;
            }

            public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
            {
                logs.Add((logType, string.Format(format, args)));
            }

            public void LogException(Exception exception, UnityEngine.Object context)
            {
                logs.Add((LogType.Exception, exception.ToString()));
            }

            public void Dispose()
            {
                Debug.unityLogger.logHandler = m_PreviousHandler;
            }
        }

        static ScreenPositionNode Load(ScreenSpaceType savedType, out List<(LogType type, string message)> logs)
        {
            // The Mode setter can no longer produce Clip, so write the stored field directly to mimic a saved graph.
            var source = new ScreenPositionNode();
            var field = typeof(ScreenPositionNode).GetField("m_ScreenSpaceType", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "Expected the private field m_ScreenSpaceType on ScreenPositionNode.");
            field.SetValue(source, savedType);
            var json = MultiJson.Serialize(source);

            var node = new ScreenPositionNode();
            using (var capture = new LogCapture())
            {
                MultiJson.Deserialize(node, json);
                logs = capture.logs;
            }
            return node;
        }

        [Test]
        public void Deserialize_SavedClip_BecomesDefaultWithWarning()
        {
            var node = Load(ScreenSpaceType.Clip, out var logs);

            CollectionAssert.AreEqual(new[] { (LogType.Warning, ScreenPositionNode.k_ClipNotSupportedWarning) }, logs);
            Assert.AreEqual(ScreenSpaceType.Default, node.screenSpaceType);
            Assert.AreEqual(ScreenPositionNode.Mode.Default, node.mode);
        }

        [TestCase(ScreenSpaceType.Default, ScreenPositionNode.Mode.Default)]
        [TestCase(ScreenSpaceType.Raw, ScreenPositionNode.Mode.Raw)]
        [TestCase(ScreenSpaceType.Center, ScreenPositionNode.Mode.Center)]
        [TestCase(ScreenSpaceType.Tiled, ScreenPositionNode.Mode.Tiled)]
        [TestCase(ScreenSpaceType.Pixel, ScreenPositionNode.Mode.Pixel)]
        public void Deserialize_SavedMode_IsUnchanged(ScreenSpaceType savedType, ScreenPositionNode.Mode expected)
        {
            var node = Load(savedType, out var logs);

            CollectionAssert.IsEmpty(logs);
            Assert.AreEqual(savedType, node.screenSpaceType);
            Assert.AreEqual(expected, node.mode);
        }
    }
}
