using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

using SysRegex = System.Text.RegularExpressions.Regex;

namespace UnityEditor.VFX
{
    class VFXSystemNames
    {
        static readonly string DefaultSystemName = "System";
        static readonly string IndexPattern = @" (\(([0-9])*\))$";

        readonly Dictionary<VFXData, string> m_SystemNamesCache = new();

        static readonly ProfilerMarker k_ProfilerMarkerSyncNames = new("VFXEditor.ResyncNames");

        public static string GetSystemName(VFXModel model)
        {
            switch (model)
            {
                case VFXDataSpawner data:
                    return data.owners.First().label;
                case VFXData data:
                    return data.title;
                case VFXContext { contextType: VFXContextType.Spawner } context:
                    return context.label;
                case VFXContext context when context.GetData() is {} data:
                    return data.title;
                default:
                    return null;
            }
        }

        public static void SetSystemName(VFXModel model, string name)
        {
            switch (model)
            {
                case VFXData data:
                    data.title = name;
                    break;
                case VFXContext { contextType: VFXContextType.Spawner } context:
                    context.label = name;
                    break;
                case VFXContext context when context.GetData() is {} data:
                    data.title = name;
                    break;
            }
        }

        public string GetUniqueSystemName(VFXData system)
        {
            if (m_SystemNamesCache.TryGetValue(system, out var systemName))
            {
                return systemName;
            }

            return GenerateUniqueName(system);
        }

        public void Sync(VFXGraph graph)
        {
            using var scope = k_ProfilerMarkerSyncNames.Auto();

            var models = new HashSet<ScriptableObject>();
            graph.CollectDependencies(models, false);

            var systems = models
                .OfType<VFXContext>()
                .Select(x => x.GetData())
                .Distinct()
                .Where(x => x != null);

            m_SystemNamesCache.Clear();
            foreach (var system in systems)
            {
                GenerateUniqueName(system);
            }
        }

        private static string GetSystemUnindexedName(string name)
        {
            return string.IsNullOrEmpty(name)
                ? name
                : SysRegex.Replace(name, IndexPattern, ""); // Remove any number in the system name
        }

        private string GenerateUniqueName(VFXData system)
        {
            var wishedName = GetSystemUnindexedName(GetSystemName(system));
            if (string.IsNullOrEmpty(wishedName))
            {
                wishedName = DefaultSystemName;
            }

            var index = 1;
            var systemName = wishedName;
            while (m_SystemNamesCache.Values.Contains(systemName))
            {
                systemName = $"{wishedName} ({index++})";
            }

            m_SystemNamesCache[system] = systemName;
            return systemName;
        }
    }
}
