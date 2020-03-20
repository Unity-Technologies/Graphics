using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEditor;

public class EditorStaticAnalysisTestXboxOne
{
    static IEnumerable<EditorStaticAnalysisTests.StaticAnalysisEntry> GetStaticAnalysisEntriesXboxOne() => EditorStaticAnalysisTests.GetStaticAnalysisEntries(BuildTarget.XboxOne);

    [Test, Version("1"), Performance]
    public void StaticAnalysisXboxOne([ValueSource(nameof(GetStaticAnalysisEntriesXboxOne))] EditorStaticAnalysisTests.StaticAnalysisEntry entry) => EditorStaticAnalysisTests.StaticAnalysisExecute(entry);
}
