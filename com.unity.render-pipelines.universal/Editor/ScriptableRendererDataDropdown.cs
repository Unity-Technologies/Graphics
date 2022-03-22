using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Rendering.Universal
{
    internal class ScriptableRendererDataDropdown : AdvancedDropdown
    {
        SerializedProperty renderers;
        public ScriptableRendererDataDropdown(SerializedProperty renderers)
            : base(new AdvancedDropdownState())
        {
            this.renderers = renderers;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            return new ScriptableRendererFeatureDropdownNode();
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            ScriptableRendererDataDropdownLeaf leaf = (item as ScriptableRendererDataDropdownLeaf);
            SpawnRenderer(renderers, leaf.RendererDataType);
        }

        static void SpawnRenderer(SerializedProperty renderers, Type rendererType)
        {
            renderers.serializedObject.Update();
            int index = renderers.arraySize;
            renderers.arraySize++;
            ScriptableRendererData instance = (ScriptableRendererData)Activator.CreateInstance(rendererType);
            instance.Awake();
            instance.OnEnable();
            renderers.GetArrayElementAtIndex(index).managedReferenceValue = instance;
            renderers.serializedObject.ApplyModifiedProperties();
        }
    }

    internal class ScriptableRendererDataDropdownLeaf : AdvancedDropdownItem
    {
        public Type RendererDataType { get; }
        public string Title { get; }
        public ScriptableRendererDataDropdownLeaf(Type rendererDataType)
            : base(rendererDataType.Name)
        {
            RendererDataType = rendererDataType;
        }
    }

    internal class ScriptableRendererFeatureDropdownNode : AdvancedDropdownItem
    {
        public ScriptableRendererFeatureDropdownNode()
            : base("Renderer Data")
        {
            foreach (var rendererType in TypeCache.GetTypesDerivedFrom(typeof(ScriptableRendererData)))
            {
                if (rendererType.GetCustomAttribute<ObsoleteAttribute>() == null)
                {
                    AddChild(new ScriptableRendererDataDropdownLeaf(rendererType));
                }
            }
        }
    }
}
