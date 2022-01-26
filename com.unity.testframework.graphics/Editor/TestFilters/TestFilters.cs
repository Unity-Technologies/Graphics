#if UNITY_EDITOR
using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using UnityEngine;

[System.Serializable]

[CreateAssetMenu(fileName = "TestCaseFilters", menuName = "Testing/Test Filter ScriptableObject", order = 100)]
public class TestFilters : ScriptableObject
{
    public TestFilterConfig[] filters;

    public void SortBySceneName()
    {
        for (int i = 0; i < filters.Length; i++)
        {
            Array.Sort(filters[i].FilteredScenes,
                (a, b) => a == null ? 1 : b == null ? -1 : a.name.CompareTo(b.name));
        }

        var sortedFilters = filters.OrderBy(s => s.FilteredScenes.FirstOrDefault() == null ? string.Empty : s.FilteredScenes.FirstOrDefault().name)
            .ThenBy(s => s.ColorSpace)
            .ThenBy(s => s.BuildPlatform)
            .ThenBy(s => s.GraphicsDevice).ToArray();
        Array.Copy(sortedFilters, filters, filters.Length);
    }
}
#endif
