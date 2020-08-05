using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEditor;

public class EditorStaticAnalysisTestPS4
{
    const int StaticAnalysisTimeout = 20 * 60 * 1000;    // 20 min for shader compilation

    static IEnumerable<EditorStaticAnalysisTests.StaticAnalysisEntry> GetStaticAnalysisEntriesPS4() => EditorStaticAnalysisTests.GetStaticAnalysisEntries(BuildTarget.PS4);

    [Test, Timeout(StaticAnalysisTimeout), Version("1"), Performance]
    public void StaticAnalysisPS4([ValueSource(nameof(GetStaticAnalysisEntriesPS4))] EditorStaticAnalysisTests.StaticAnalysisEntry entries) => EditorStaticAnalysisTests.StaticAnalysisExecute(entries);
}
