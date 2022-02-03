using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Holds the registered plugins.
    /// </summary>
    public class PluginRepository : IDisposable
    {
        List<IPluginHandler> m_PluginHandlers;
        GraphViewEditorWindow m_Window;

        internal PluginRepository(GraphViewEditorWindow window)
        {
            m_PluginHandlers = new List<IPluginHandler>();
            m_Window = window;
        }

        public IEnumerable<IPluginHandler> GetPluginHandlers()
        {
            return m_PluginHandlers;
        }

        public void RegisterPlugins(IEnumerable<IPluginHandler> plugins)
        {
            var pluginList = plugins.ToList();
            UnregisterPlugins(pluginList);
            foreach (IPluginHandler handler in pluginList)
            {
                handler.Register(m_Window);
                m_PluginHandlers.Add(handler);
            }
        }

        public void UnregisterPlugins(IEnumerable<IPluginHandler> except = null)
        {
            var pluginList = except?.ToList();
            foreach (var plugin in m_PluginHandlers)
            {
                if (except == null || !pluginList.Contains(plugin))
                    plugin.Unregister();
            }
            m_PluginHandlers.Clear();
        }

        public IEnumerable<IPluginHandler> RegisteredPlugins => m_PluginHandlers;

        public void Dispose()
        {
            UnregisterPlugins();
        }
    }
}
