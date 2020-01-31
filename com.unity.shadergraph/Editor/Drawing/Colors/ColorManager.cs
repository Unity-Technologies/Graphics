using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph.Drawing.Colors
{
    // Use this to set colors on your node titles.
    // There are 2 methods of setting colors - direct Color objects via code (such as data saved in the node itself),
    // or setting classes on a VisualElement, allowing the colors themselves to be defined in USS. See notes on
    // IColorProvider for how to use these different methods.
    class ColorManager
    {
        static string DefaultProvider = NoColors.Title;
    
        List<IColorProvider> m_Providers;
        
        int m_ActiveIndex = 0;
        public int activeIndex
        {
            get => m_ActiveIndex;
            private set
            {
                if (!IsValidIndex(value))
                    return;
                
                m_ActiveIndex = value;
            }
        }

        public ColorManager(string activeProvider)
        {
            m_Providers = new List<IColorProvider>();

            if (string.IsNullOrEmpty(activeProvider))
                activeProvider = DefaultProvider;

            foreach (var colorType in TypeCache.GetTypesDerivedFrom<IColorProvider>().Where(t => !t.IsAbstract))
            {
                var provider = (IColorProvider) Activator.CreateInstance(colorType);
                m_Providers.Add(provider);
            }
            
            m_Providers.Sort((p1, p2) => string.Compare(p1.GetTitle(), p2.GetTitle(), StringComparison.InvariantCulture));
            activeIndex = m_Providers.FindIndex(provider => provider.GetTitle() == activeProvider);
        }

        public void SetNodesDirty(IEnumerable<IShaderNodeView> nodeViews)
        {
            if (activeProvider.ClearOnDirty())
            {
                foreach (var view in nodeViews)
                {
                    activeProvider.ClearColor(view);
                }
            }
        }
        
        public void SetActiveProvider(int newIndex, IEnumerable<IShaderNodeView> nodeViews)
        {
            if (newIndex == activeIndex || !IsValidIndex(newIndex))
                return;
            
            var oldProvider = activeProvider;
            activeIndex = newIndex;

            foreach (var view in nodeViews)
            {
                oldProvider.ClearColor(view);
                activeProvider.ApplyColor(view);
            }
        }

        public void UpdateNodeViews(IEnumerable<IShaderNodeView> nodeViews)
        {
            foreach (var view in nodeViews)
            {
                UpdateNodeView(view);
            }
        }
        
        public void UpdateNodeView(IShaderNodeView nodeView)
        {
            activeProvider.ApplyColor(nodeView);
        }

        public IEnumerable<string> providerNames => m_Providers.Select(p => p.GetTitle());

        public string activeProviderName => activeProvider.GetTitle();

        public bool activeSupportsCustom => activeProvider.AllowCustom();

        IColorProvider activeProvider => m_Providers[activeIndex];
        
        bool IsValidIndex(int index) => index >= 0 && index < m_Providers.Count;
    }
}
