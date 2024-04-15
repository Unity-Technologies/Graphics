using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    class ScriptableRendererFeatureProvider : FilterWindow.IProvider
    {
        class FeatureElement : FilterWindow.Element
        {
            public Type type;
        }

        readonly ScriptableRendererDataEditor m_Editor;
        public Vector2 position { get; set; }

        public ScriptableRendererFeatureProvider(ScriptableRendererDataEditor editor)
        {
            m_Editor = editor;
        }

        internal bool RendererFeatureSupported(Type rendererFeatureType)
        {
            Type rendererType = m_Editor.target.GetType();

            SupportedOnRendererAttribute rendererFilterAttribute = Attribute.GetCustomAttribute(rendererFeatureType, typeof(SupportedOnRendererAttribute)) as SupportedOnRendererAttribute;
            if (rendererFilterAttribute != null)
            {
                bool foundEditor = false;
                for (int i = 0; i < rendererFilterAttribute.rendererTypes.Length && !foundEditor; i++)
                    foundEditor = rendererFilterAttribute.rendererTypes[i] == rendererType;

                return foundEditor;
            }

            return true;
        }

        public void CreateComponentTree(List<FilterWindow.Element> tree)
        {
            tree.Add(new FilterWindow.GroupElement(0, "Renderer Features"));
            var types = TypeCache.GetTypesDerivedFrom<ScriptableRendererFeature>();
            var data = m_Editor.target as ScriptableRendererData;
            foreach (var type in types)
            {
                // Check to see if the current renderer feature can be used with the current renderer. If the attribute isn't found then its compatible with everything.

                if (!RendererFeatureSupported(type))
                    continue;

                if (data.DuplicateFeatureCheck(type))
                {
                    continue;
                }

                string path = GetMenuNameFromType(type);
                tree.Add(new FeatureElement
                {
                    content = new GUIContent(path),
                    level = 1,
                    type = type
                });
            }
        }

        public bool GoToChild(FilterWindow.Element element, bool addIfComponent)
        {
            if (element is FeatureElement featureElement)
            {
                m_Editor.AddComponent(featureElement.type);
                return true;
            }

            return false;
        }

        string GetMenuNameFromType(Type type)
        {
            string path;
            if (!m_Editor.GetCustomTitle(type, out path))
            {
                path = ObjectNames.NicifyVariableName(type.Name);
            }

            if (type.Namespace != null)
            {
                if (type.Namespace.Contains("Experimental"))
                    path += " (Experimental)";
            }

            return path;
        }
    }
}
