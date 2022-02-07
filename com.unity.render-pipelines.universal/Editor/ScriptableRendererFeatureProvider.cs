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

        public void CreateComponentTree(List<FilterWindow.Element> tree)
        {
            tree.Add(new FilterWindow.GroupElement(0, "Renderer Features"));
            var types = TypeCache.GetTypesDerivedFrom<ScriptableRendererFeature>();
            var data = m_Editor.target as ScriptableRendererData;
            foreach (var type in types)
            {
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
                m_Editor.AddComponent(featureElement.type.Name);
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
