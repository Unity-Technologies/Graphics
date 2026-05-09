using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.Rendering
{
#if UNITY_ENABLE_CHECKS
    internal static class VolumeDebugData
    {
        [NoAutoStaticsCleanup] // Cache for reflection data, no need to reset
        static Lazy<Dictionary<int, string>> debugIds = new(() => new Dictionary<int, string>());

        internal static string GetVolumeParameterDebugId(VolumeParameter parameter)
        {
            return debugIds.Value.TryGetValue(parameter.fieldHash, out var debugId) ? debugId : string.Empty;
        }

        internal static void AddVolumeParameterDebugId(VolumeParameter parameter, FieldInfo field)
        {
            var fieldHash = field.GetHashCode();
            parameter.fieldHash = fieldHash;
            if (debugIds.Value.ContainsKey(fieldHash))
                return;

            var displayInfo = field.GetCustomAttribute<DisplayInfoAttribute>(true);
            var debugId = displayInfo != null ? displayInfo.name : field.Name;
#if UNITY_EDITOR
            debugId = UnityEditor.ObjectNames.NicifyVariableName(debugId); // In the editor, make the name more readable
#endif
            debugIds.Value.Add(fieldHash, debugId);
        }
    }
#endif
}
