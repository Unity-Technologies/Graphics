using System;
using UnityEditor;

namespace UnityEngine.TestTools.Graphics.Universal
{
    // Per-build-target whitelist for a project's shader keyword declaration overrides.
    //
    // Shader build settings have no per-target knob: a keyword override applies to every build target. This
    // asset adds the missing dimension. Each override (identified by its variant keyword set) carries a
    // bitmask of the targets it stays dynamic on; on targets outside the mask the keyword reverts to
    // specialized (multi_compile) variants. An override with no entry is enabled everywhere ("Everything",
    // the default). PerPlatformKeywordOverrides applies this during the player build.
    //
    // Edit it through its inspector, which lists the project's current overrides with a target multi-select.
    [CreateAssetMenu(fileName = "ShaderKeywordPlatformOverrides",
        menuName = "Testing/SRP/Shader Keyword Platform Overrides")]
    class ShaderKeywordPlatformOverrides : ScriptableObject
    {
        // Targets shown in the inspector multi-select. Entry masks are bit-indexed into this array, so the
        // order is load-bearing: append only, never reorder or remove, or existing assets shift meaning.
        public static readonly BuildTarget[] SelectableTargets =
        {
            BuildTarget.StandaloneOSX,
            BuildTarget.StandaloneWindows,
            BuildTarget.iOS,
            BuildTarget.Android,
            BuildTarget.StandaloneWindows64,
            BuildTarget.WebGL,
            BuildTarget.WSAPlayer,
            BuildTarget.StandaloneLinux64,
            BuildTarget.PS4,
            BuildTarget.XboxOne,
            BuildTarget.tvOS,
            BuildTarget.Switch,
            BuildTarget.LinuxHeadlessSimulation,
            BuildTarget.GameCoreXboxSeries,
            BuildTarget.GameCoreXboxOne,
            BuildTarget.PS5,
            BuildTarget.EmbeddedLinux,
            BuildTarget.QNX,
            BuildTarget.VisionOS,
            BuildTarget.Switch2,
            BuildTarget.Kepler,
        };

        [Serializable]
        public struct Entry
        {
            public string[] keywords;      // identifies an override by its variant set
            public int enabledTargetsMask; // bit i set => stays dynamic on SelectableTargets[i]
        }

        public Entry[] entries = Array.Empty<Entry>();

        // An override (identified by its variant keyword names) stays dynamic on 'target' unless an entry
        // clears its bit. No entry, or a target outside SelectableTargets, leaves the override enabled.
        public bool IsEnabledOnTarget(string[] keywordNames, BuildTarget target)
        {
            int entry = FindEntry(keywordNames);
            if (entry < 0)
                return true;

            int bit = IndexOfTarget(target);
            if (bit < 0)
                return true;

            return (entries[entry].enabledTargetsMask & (1 << bit)) != 0;
        }

        int FindEntry(string[] keywordNames)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (KeywordsEqual(entries[i].keywords, keywordNames))
                    return i;
            }
            return -1;
        }

        public static int IndexOfTarget(BuildTarget target)
        {
            for (int i = 0; i < SelectableTargets.Length; i++)
            {
                if (SelectableTargets[i] == target)
                    return i;
            }
            return -1;
        }

        public static bool KeywordsEqual(string[] a, string[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;
            foreach (var name in a)
            {
                bool found = false;
                foreach (var other in b)
                {
                    if (other == name)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    return false;
            }
            return true;
        }
    }
}
