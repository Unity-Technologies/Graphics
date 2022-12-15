using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEditor;

public class EditorStaticAnalysisTestXbox
{
    const int StaticAnalysisTimeout = 10 * 60 * 1000;    // 10 min for shader compilation

    static IEnumerable<EditorStaticAnalysisTests.StaticAnalysisEntry> GetStaticAnalysisEntriesXbox() => EditorStaticAnalysisTests.GetStaticAnalysisEntries(BuildTarget.XboxOne);

    [Test, Timeout(StaticAnalysisTimeout), Version("1"), Performance]
    public void StaticAnalysisXboxOne([ValueSource(nameof(GetStaticAnalysisEntriesXbox))] EditorStaticAnalysisTests.StaticAnalysisEntry entry) => EditorStaticAnalysisTests.StaticAnalysisExecute(entry);
}
