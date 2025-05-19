using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEditor.TestTools;

namespace UnityEngine.TestTools.Graphics.Performance.Editor.StaticAnalysis
{
    public class EditorStaticAnalysisTestPlayStation
    {
        const int StaticAnalysisTimeout = 20 * 60 * 1000; // 20 min for shader compilation

        static IEnumerable<EditorStaticAnalysisTests.StaticAnalysisEntry> GetStaticAnalysisEntriesPlayStation() =>
            EditorStaticAnalysisTests.GetStaticAnalysisEntries(BuildTarget.PS4);

        [Test, Timeout(StaticAnalysisTimeout), Version("1"), Performance]
        [RequirePlatformSupport(BuildTarget.PS4)]
        public void StaticAnalysisPlayStation(
            [ValueSource(nameof(GetStaticAnalysisEntriesPlayStation))]
                EditorStaticAnalysisTests.StaticAnalysisEntry entries
        ) => EditorStaticAnalysisTests.StaticAnalysisExecute(entries);
    }
}
