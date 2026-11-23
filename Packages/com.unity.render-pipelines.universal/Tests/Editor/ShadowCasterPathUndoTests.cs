using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.Rendering.Universal.Tests
{
    // UUM-152935: undoing a ShadowCaster2D shape edit in Play mode logged
    // "No script asset for ShadowCasterPath. Check that the definition is in a file of the same name."
    // ShadowCasterPath is a ScriptableObject, so Unity needs a MonoScript for it, which needs a file of the same name.
    class ShadowCasterPathUndoTests
    {
        // Serialized so it survives the domain reload on exiting Play mode, which runs before the assert.
        [SerializeField]
        List<string> m_MissingScriptWarnings = new List<string>();

        void OnLogMessage(string message, string stackTrace, LogType type)
        {
            if (message.Contains("No script asset for"))
                m_MissingScriptWarnings.Add(message);
        }

        static ShadowCasterPath CreateShadowCasterPath()
        {
            var path = ScriptableObject.CreateInstance<ShadowCasterPath>();
            path.hideFlags = HideFlags.HideAndDontSave;
            return path;
        }

        [Test]
        public void ShadowCasterPath_HasMonoScript()
        {
            var path = CreateShadowCasterPath();
            try
            {
                var script = MonoScript.FromScriptableObject(path);
                Assert.IsNotNull(script, "ShadowCasterPath has no MonoScript.");
                Assert.AreEqual(typeof(ShadowCasterPath), script.GetClass());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(path);
            }
        }

        [UnityTest]
        public IEnumerator UndoShadowCasterPathInPlayMode_DoesNotWarnAboutMissingScript()
        {
            return UndoShadowCasterPathInPlayMode(false);
        }

        [UnityTest]
        public IEnumerator UndoRedoShadowCasterPathInPlayMode_DoesNotWarnAboutMissingScript()
        {
            return UndoShadowCasterPathInPlayMode(true);
        }

        IEnumerator UndoShadowCasterPathInPlayMode(bool redo)
        {
            yield return new EnterPlayMode();

            var path = CreateShadowCasterPath();
            m_MissingScriptWarnings.Clear();
            Application.logMessageReceived += OnLogMessage;
            try
            {
                Undo.IncrementCurrentGroup();
                Undo.RegisterCompleteObjectUndo(path, "Edit Shadow Caster Shape");
                Undo.FlushUndoRecordObjects();
                Undo.IncrementCurrentGroup();
                Undo.PerformUndo();
                if (redo)
                    Undo.PerformRedo();
            }
            finally
            {
                Application.logMessageReceived -= OnLogMessage;
                Undo.ClearUndo(path);
                UnityEngine.Object.DestroyImmediate(path);
            }

            yield return new ExitPlayMode();

            Assert.IsEmpty(m_MissingScriptWarnings);
        }
    }
}
