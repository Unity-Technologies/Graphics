using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEditor;

public class EditorStaticAnalysisTestXboxOne
{
    const int StaticAnalysisTimeout = 10 * 60 * 1000;    // 10 min for shader compilation

    static IEnumerable<EditorStaticAnalysisTests.StaticAnalysisEntry> GetStaticAnalysisEntriesXboxOne() => EditorStaticAnalysisTests.GetStaticAnalysisEntries(BuildTarget.XboxOne);

    [Test, Timeout(StaticAnalysisTimeout), Version("1"), Performance]
    public void StaticAnalysisXboxOne([ValueSource(nameof(GetStaticAnalysisEntriesXboxOne))] EditorStaticAnalysisTests.StaticAnalysisEntry entry) => EditorStaticAnalysisTests.StaticAnalysisExecute(entry);
}
