using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.HighDefinition.Compositor
{
    // Holds a list of layers and layer/composition properties. This is serialized and can be shared between projects
    internal class CompositionProfile : ScriptableObject
    {
        [SerializeField] List<ShaderProperty> m_ShaderProperties = new List<ShaderProperty>();

        public void AddPropertiesFromShaderAndMaterial (CompositionManager compositor, Shader shader, Material material)
        {
            // reflect the non-texture shader properties
            List<string> propertyNames = new List<string>();
            int propCount = shader.GetPropertyCount();
            for (int i = 0; i < propCount; i++)
            {
                ShaderProperty sp = ShaderProperty.Create(shader, material, i);
                AddShaderProperty(compositor, sp);
                propertyNames.Add(sp.propertyName);
            }

            // remove any left-over properties that do not appear in the shader anymore
            for (int j = m_ShaderProperties.Count - 1; j >= 0; --j)
            {
                int indx = propertyNames.FindIndex(x => x == m_ShaderProperties[j].propertyName);
                if (indx < 0)
                {
                    m_ShaderProperties.RemoveAt(j);
                }
            }

            // Now remove any left-over  layers that do not appear in the shader anymore
            for (int j = compositor.layers.Count - 1; j >= 0; --j)
            {
                if (compositor.layers[j].outputTarget != CompositorLayer.OutputTarget.CameraStack)
                {
                    int indx = propertyNames.FindIndex(x => x == compositor.layers[j].name);
                    if (indx < 0)
                    {
                        compositor.RemoveLayerAtIndex(j);
                    }
                }
            }
        }

        public void AddShaderProperty(CompositionManager compositor, ShaderProperty sp)
        {
            Assert.IsNotNull(sp);

            // Check if property should be shown in the inspector
            bool hide = ((int)sp.flags & (int)ShaderPropertyFlags.NonModifiableTextureData) != 0
                        || ((int)sp.flags & (int)ShaderPropertyFlags.HideInInspector) != 0;

            if (!hide)
            {
                // Check if property already exists / do not add duplicates
                int indx = m_ShaderProperties.FindIndex(s => s.propertyName == sp.propertyName);
                if (indx < 0)
                {
                    m_ShaderProperties.Add(sp);
                }
            }

            // For textures, check if we already have this layer in the layer list. If not, add it.
            if (sp.propertyType == ShaderPropertyType.Texture && sp.canBeUsedAsRT)
            {
                int indx = compositor.layers.FindIndex(s => s.name == sp.propertyName);
                if (indx < 0 && !hide)
                {
                    var newLayer = CompositorLayer.CreateOutputLayer(sp.propertyName);
                    compositor.layers.Add(newLayer);
                }
                else if (indx >= 0 && hide)
                {
                    // if a layer that was in the list is now hidden, remove it
                    compositor.RemoveLayerAtIndex(indx);
                }
            }
        }

        public void CopyPropertiesToMaterial(Material material)
        {
            foreach (var prop in m_ShaderProperties)
            {
                if (prop.propertyType == ShaderPropertyType.Float)
                {
                    material.SetFloat(prop.propertyName, prop.value.x);
                }
                else if (prop.propertyType == ShaderPropertyType.Vector)
                {
                    material.SetVector(prop.propertyName, prop.value);
                }
                else if (prop.propertyType == ShaderPropertyType.Range)
                {
                    material.SetFloat(prop.propertyName, prop.value.x);
                }
                else if (prop.propertyType == ShaderPropertyType.Color)
                {
                    material.SetColor(prop.propertyName, prop.value);
                }
            }
        }
    }
}
