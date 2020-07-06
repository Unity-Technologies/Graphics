using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Base implementation of a material GUI block to be disabled in the material inspector.
    /// </summary>
    abstract class MaterialUIBlock
    {
        /// <summary>The current material editor.</summary>
        protected MaterialEditor        materialEditor;
        /// <summary>The list of selected materials to edit.</summary>
        protected Material[]            materials;
        /// <summary>The list of available properties in the selected materials.</summary>
        protected MaterialProperty[]    properties;

        /// <summary>Parent of the UI block.</summary>
        protected MaterialUIBlockList   parent;

        [Flags]
        internal enum Expandable : uint
        {
            // Standard
            Base = 1<<0,
            Input = 1<<1,
            Tesselation = 1<<2,
            Transparency = 1<<3,
            // Free slot 4
            Detail = 1<<5,
            Emissive = 1<<6,
            Advance = 1<<7,
            Other = 1 << 8,
            ShaderGraph = 1 << 9,

            // Layered
            MainLayer = 1 << 11,
            Layer1 = 1 << 12,
            Layer2 = 1 << 13,
            Layer3 = 1 << 14,
            LayeringOptionMain = 1 << 15,
            ShowLayer1 = 1 << 16,
            ShowLayer2 = 1 << 17,
            ShowLayer3 = 1 << 18,
            MaterialReferences = 1 << 19,
            MainInput = 1 << 20,
            Layer1Input = 1 << 21,
            Layer2Input = 1 << 22,
            Layer3Input = 1 << 23,
            MainDetail = 1 << 24,
            Layer1Detail = 1 << 25,
            Layer2Detail = 1 << 26,
            Layer3Detail = 1 << 27,
            LayeringOption1 = 1 << 28,
            LayeringOption2 = 1 << 29,
            LayeringOption3 = 1 << 30
        }

        internal void         Initialize(MaterialEditor materialEditor, MaterialProperty[] properties, MaterialUIBlockList parent)
        {
            this.materialEditor = materialEditor;
            this.parent = parent;
            materials = materialEditor.targets.Select(target => target as Material).ToArray();

            // We should always register the key used to keep collapsable state
            materialEditor.InitExpandableState();
        }

        internal void         UpdateMaterialProperties(MaterialProperty[] properties)
        {
            this.properties = properties;
            LoadMaterialProperties();
        }

        /// <summary>
        /// Find a material property in the list of available properties.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="isMandatory">Specifies whether the property is mandatory for your Inspector.</param>
        /// <returns>Returns the material property if it exists. Returns null otherwise.</returns>
        protected MaterialProperty FindProperty(string propertyName, bool isMandatory = false)
        {
            // ShaderGUI.FindProperty is a protected member of ShaderGUI so we can't call it here:
            // return ShaderGUI.FindProperty(propertyName, properties, isMandatory);

            // TODO: move this to a map since this is done at every editor frame
            foreach (var prop in properties)
                if (prop.name == propertyName)
                    return prop;

            if (isMandatory)
                throw new ArgumentException("Could not find MaterialProperty: '" + propertyName + "', Num properties: " + properties.Length);
            return null;
        }

        /// <summary>
        /// Find a material property with layering option
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="layerCount">Number of layers of the shader.</param>
        /// <param name="isMandatory">Specifies whether the property is mandatory for your Inspector.</param>
        /// <returns>Returns the material property if it exists. Returns null otherwise.</returns>
        protected MaterialProperty[] FindPropertyLayered(string propertyName, int layerCount, bool isMandatory = false)
        {
            MaterialProperty[] properties = new MaterialProperty[layerCount];

            // If the layerCount is 1, then it means that the property we're fetching is not from a layered material
            // thus it doesn't have a prefix
            string[] prefixes = (layerCount > 1) ? new []{"0", "1", "2", "3"} : new []{""};

            for (int i = 0; i < layerCount; i++)
            {
                properties[i] = FindProperty(string.Format("{0}{1}", propertyName, prefixes[i]), isMandatory);
            }

            return properties;
        }

        /// <summary>
        /// Use this function to load the material properties you need in your block.
        /// </summary>
        public abstract void LoadMaterialProperties();

        /// <summary>
        /// Renders the properties in your block.
        /// </summary>
        public abstract void OnGUI();
    }
}
