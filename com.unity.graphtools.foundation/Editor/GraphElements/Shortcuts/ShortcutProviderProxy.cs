using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    sealed class ShortcutProviderProxy : IDiscoveryShortcutProviderProxy
    {
        static ShortcutProviderProxy s_ShortcutProviderProxy;

        public static ShortcutProviderProxy GetInstance()
        {
            if (s_ShortcutProviderProxy == null)
            {
                s_ShortcutProviderProxy = new ShortcutProviderProxy();
                ToolShortcutDiscoveryProvider.GetInstance().Proxy = s_ShortcutProviderProxy;
            }

            return s_ShortcutProviderProxy;
        }

        List<(string toolName, Type context, Func<string, bool> shortcutFilter)> m_Tools;

        ShortcutProviderProxy()
        {
            m_Tools = new List<(string toolName, Type editorWindowType, Func<string, bool> shortcutFilter)>();
        }

        public void AddTool(string toolName, Type editorWindowType, Func<string, bool> shortcutFilter, bool rebuildNow = false)
        {
            if (!m_Tools.Contains((toolName, editorWindowType, shortcutFilter)))
            {
                m_Tools.Add((toolName, editorWindowType, shortcutFilter));

                if (rebuildNow)
                {
                    ToolShortcutDiscoveryProvider.RebuildShortcuts();
                }
            }
        }

        public IEnumerable<ShortcutDefinition> GetDefinedShortcuts()
        {
            var shortcutEventTypes = TypeCache.GetTypesWithAttribute<ToolShortcutEventAttribute>()
                .Where(t => typeof(IShortcutEvent).IsAssignableFrom(t) && AssemblyCache.CachedAssemblies.Contains(t.Assembly))
                .ToList();

            foreach (var type in shortcutEventTypes)
            {
                MethodInfo methodInfo = null;
                var attributes = (ToolShortcutEventAttribute[])type.GetCustomAttributes(typeof(ToolShortcutEventAttribute), false);
                foreach (var attribute in attributes)
                {
                    if (attribute.OnlyOnPlatforms != null && !attribute.OnlyOnPlatforms.Contains(Application.platform))
                    {
                        continue;
                    }

                    if (attribute.ExcludedPlatforms != null && attribute.ExcludedPlatforms.Contains(Application.platform))
                    {
                        continue;
                    }

                    if (methodInfo == null)
                        methodInfo = type.GetMethod("SendEvent",
                            BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic);

                    Debug.Assert(methodInfo != null);

                    foreach (var(toolName, context, shortcutFilter) in m_Tools)
                    {
                        if (attribute.ToolName != null && toolName != attribute.ToolName)
                            continue;

                        if (!shortcutFilter?.Invoke(attribute.Identifier) ?? false)
                            continue;

                        yield return new ShortcutDefinition
                        {
                            ToolName = toolName,
                            ShortcutId = attribute.Identifier,
                            Context = context,
                            DefaultBinding = attribute.DefaultBinding,
                            DisplayName = attribute.DisplayName,
                            IsClutch = attribute.IsClutch,
                            MethodInfo = methodInfo
                        };
                    }
                }
            }
        }
    }
}
