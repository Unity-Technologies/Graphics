#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "TestCaseFilters", menuName = "Test Filter ScriptableObject", order = 1)]
public class TestFilters : ScriptableObject
{
    public TestFilterConfig[] filters;

    public TestFilters()
    {
        filters = new TestFilterConfig[1];
    }

    public void SortBySceneName()
    {
        Array.Sort(filters,
            (a, b) => a.FilteredScene == null ? 1 : b.FilteredScene == null ? -1 : a.FilteredScene.name.CompareTo(b.FilteredScene.name));
    }
}
#endif
