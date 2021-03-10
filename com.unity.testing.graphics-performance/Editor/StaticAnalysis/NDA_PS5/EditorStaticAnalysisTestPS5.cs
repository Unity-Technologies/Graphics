using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEditor;

public class EditorStaticAnalysisTestPS5
{
    const int StaticAnalysisTimeout = 20 * 60 * 1000;    // 20 min for shader compilation

    // Note: We're using the same test for PS5 and PS4 for now.
    static IEnumerable<EditorStaticAnalysisTests.StaticAnalysisEntry> GetStaticAnalysisEntriesPS5() => EditorStaticAnalysisTests.GetStaticAnalysisEntries(BuildTarget.PS4);

    [Test, Timeout(StaticAnalysisTimeout), Version("1"), Performance]
    public void StaticAnalysisPS5([ValueSource(nameof(GetStaticAnalysisEntriesPS5))] EditorStaticAnalysisTests.StaticAnalysisEntry entries) => EditorStaticAnalysisTests.StaticAnalysisExecute(entries);
}
