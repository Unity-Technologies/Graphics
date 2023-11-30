using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{

    [Serializable]
    internal abstract class AbstractShaderGraphDataExtension : JsonObject
    {
        internal virtual int paddingIdentationFactor => 15;

        internal abstract string displayName { get; }

        internal abstract void OnPropertiesGUI(VisualElement context, Action onChange, Action<string> registerUndo, GraphData owner);

        internal static List<AbstractShaderGraphDataExtension> ValidExtensions()
        {
            var result = new List<AbstractShaderGraphDataExtension>();
            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(AbstractShaderGraphDataExtension)))
            {
                if (type.IsGenericType || type == typeof(MultiJsonInternal.UnknownGraphDataExtension))
                    continue;

                var subData = (AbstractShaderGraphDataExtension)Activator.CreateInstance(type);
                result.Add(subData);
            }
            return result;
        }
    }
}
