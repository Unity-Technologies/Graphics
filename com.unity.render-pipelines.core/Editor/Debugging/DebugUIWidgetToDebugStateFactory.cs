using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.Callbacks;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class DebugUIWidgetToDebugStateFactory
    {
        private static Lazy<DebugUIWidgetToDebugStateFactory> m_Instance =
            new (() => new DebugUIWidgetToDebugStateFactory());

        public static DebugUIWidgetToDebugStateFactory instance => m_Instance.Value;

        static Dictionary<Type, Type> s_WidgetStateMap; // DebugUI.Widget type -> DebugState type

        public DebugUIWidgetToDebugStateFactory()
        {
            // Map states to widget (a single state can map to several widget types if the value to serialize is the same)
            var stateTypes = CoreUtils.GetAllTypesDerivedFrom<DebugState>()
                .Where(
                    t => t.IsDefined(typeof(DebugStateAttribute), false)
                         && !t.IsAbstract
                );

            s_WidgetStateMap = new Dictionary<Type, Type>();

            foreach (var stateType in stateTypes)
            {
                var attr = (DebugStateAttribute)stateType.GetCustomAttributes(typeof(DebugStateAttribute), false)[0];

                foreach (var t in attr.types)
                    s_WidgetStateMap.Add(t, stateType);
            }
        }

        [MustUseReturnValue]
        public bool CreateDebugState([DisallowNull] Type widgetType, [NotNullWhen(true)] out DebugState state)
        {
            state = null;

            if (s_WidgetStateMap.TryGetValue(widgetType, out Type stateType))
            {
                state = ScriptableObject.CreateInstance(stateType) as DebugState;
            }

            return state != null;
        }

        [DidReloadScripts]
        static void OnEditorReload()
        {
            m_Instance = new Lazy<DebugUIWidgetToDebugStateFactory>();
        }
    }
}
