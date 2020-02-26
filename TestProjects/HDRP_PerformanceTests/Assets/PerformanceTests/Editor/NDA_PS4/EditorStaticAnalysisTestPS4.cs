using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEditor;

public class EditorStaticAnalysisTestXboxOne
{
    static IEnumerable<EditorStaticAnalysisTests.StaticAnalysisEntry> GetStaticAnalysisEntriesPS4() => EditorStaticAnalysisTests.GetStaticAnalysisEntries(BuildTarget.PS4);

    [Test, Version("1")]
    public void StaticAnalysisPS4([ValueSource(nameof(GetStaticAnalysisEntriesPS4))] EditorStaticAnalysisTests.StaticAnalysisEntry entries) => EditorStaticAnalysisTests.StaticAnalysisExecute(entries);
}
