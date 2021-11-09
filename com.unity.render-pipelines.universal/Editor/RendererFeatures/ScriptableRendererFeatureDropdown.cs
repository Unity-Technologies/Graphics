using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal class ScriptableRendererFeatureSelectionDropdown : AdvancedDropdown
    {
        SerializedProperty rendererFeatures;
        Editor ownerEditor;
        public ScriptableRendererFeatureSelectionDropdown(SerializedProperty rendererFeatures, Editor ownerEditor)
            : base(new AdvancedDropdownState())
        {
            this.rendererFeatures = rendererFeatures;
            this.ownerEditor = ownerEditor;
            // Adjust the minimum size of the dropdown menu by changing this variable "minimumSize".
            minimumSize = new Vector2(250, 200);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            // Get existing types of renderer features to avoid unwanted duplicates.
            Type[] existingRendererFeatureTypes = new Type[rendererFeatures.arraySize];
            for (int i = 0; i < rendererFeatures.arraySize; i++)
            {
                existingRendererFeatureTypes[i] = rendererFeatures.GetArrayElementAtIndex(i).managedReferenceValue.GetType();
            }

            TypeCache.TypeCollection rendererFeatureTypes = TypeCache.GetTypesDerivedFrom<ScriptableRendererFeature>();
            int size = rendererFeatureTypes.Count;
            string[][] paths = new string[size][];
            List<int> indicies = Enumerable.Range(0, size).ToList();
            for (int i = 0; i < size; i++)
            {
                RendererFeatureInfoAttribute attribute = rendererFeatureTypes[i].GetCustomAttribute<RendererFeatureInfoAttribute>();
                if (attribute != null)
                {
                    paths[i] = attribute.Path;
                    if (DuplicateFeatureCheck(rendererFeatureTypes[i], existingRendererFeatureTypes, attribute))
                    {
                        indicies.Remove(i);
                    }
                }
                else
                {
                    paths[i] = new string[] { "Custom", rendererFeatureTypes[i].Name };
                }
            }

            return new ScriptableRendererFeatureDropdownNode("Renderer Features", indicies, paths, rendererFeatureTypes.ToArray());
        }
        bool DuplicateFeatureCheck(Type type, Type[] existingRendererFeatureTypes, RendererFeatureInfoAttribute attribute)
        {
            return attribute.DisallowMultipleRendererFeatures && existingRendererFeatureTypes.Any(t => t == type);
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            int index = rendererFeatures.arraySize++;
            SerializedProperty rendererFeature = rendererFeatures.GetArrayElementAtIndex(index);

            ScriptableRendererFeatureDropdownLeaf leaf = (item as ScriptableRendererFeatureDropdownLeaf);
            var instance = Activator.CreateInstance(leaf.RendererFeatureType);
            ((ScriptableRendererFeature)instance).name = leaf.Title;
            rendererFeature.managedReferenceValue = instance;

            ownerEditor.serializedObject.ApplyModifiedProperties();
        }
    }

    internal class ScriptableRendererFeatureDropdownLeaf : AdvancedDropdownItem
    {
        public Type RendererFeatureType { get; }
        public string Title { get; }
        public ScriptableRendererFeatureDropdownLeaf(Type rendererFeatureType, string title)
            : base(title)
        {
            RendererFeatureType = rendererFeatureType;
            Title = title;
        }
    }

    internal class ScriptableRendererFeatureDropdownNode : AdvancedDropdownItem
    {
        Dictionary<string, List<int>> pathToIndex;
        public ScriptableRendererFeatureDropdownNode(string tag, List<int> subPaths, string[][] paths, Type[] testRendererFeatureTypes, int depth = 0)
            : base(tag)
        {
            pathToIndex = new Dictionary<string, List<int>>();
            foreach (int i in subPaths)
            {
                string[] path = paths[i];
                if (path == null || path.Length - 1 <= depth)
                {
                    AddChild(new ScriptableRendererFeatureDropdownLeaf(testRendererFeatureTypes[i], path[depth]));
                }
                else
                {
                    List<int> indexList;
                    if (pathToIndex.TryGetValue(path[depth], out indexList))
                    {
                        indexList.Add(i);
                    }
                    else
                    {
                        var list = new List<int>();
                        list.Add(i);
                        pathToIndex.Add(path[depth], list);
                    }
                }
            }
            foreach (var entry in pathToIndex)
            {
                AddChild(new ScriptableRendererFeatureDropdownNode(entry.Key, entry.Value, paths, testRendererFeatureTypes, depth + 1));
            }
        }
    }
}
