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
    public abstract class MaterialUIBlock
    {
        /// <summary>The current material editor.</summary>
        protected MaterialEditor        materialEditor;
        /// <summary>The list of selected materials to edit.</summary>
        protected Material[]            materials;
        /// <summary>The list of available properties in the selected materials.</summary>
        protected MaterialProperty[]    properties;

        /// <summary>Parent of the UI block.</summary>
        protected MaterialUIBlockList   parent;

        /// <summary>Bit index used to store a foldout state in the editor preferences.</summary>
        [Flags]
        public enum ExpandableBit : uint
        {
            // Standard
            ///<summary>Reserved Base Bit</summary>
            Base = 1 << 0,
            ///<summary>Reserved Input Bit</summary>
            Input = 1 << 1,
            ///<summary>Reserved Tessellation Bit</summary>
            Tessellation = 1 << 2,
            ///<summary>Reserved Transparency Bit</summary>
            Transparency = 1 << 3,
            // Free slot 4
            ///<summary>Reserved Detail Bit</summary>
            Detail = 1 << 5,
            ///<summary>Reserved Emissive Bit</summary>
            Emissive = 1 << 6,
            ///<summary>Reserved Advanced Bit</summary>
            Advance = 1 << 7,
            ///<summary>Reserved Other Bit</summary>
            Other = 1 << 8,
            ///<summary>Reserved ShaderGraph Bit</summary>
            ShaderGraph = 1 << 9,
            // Free slot 10
            // Layered
            ///<summary>Reserved MainLayer Bit</summary>
            MainLayer = 1 << 11,
            ///<summary>Reserved Layer1 Bit</summary>
            Layer1 = 1 << 12,
            ///<summary>Reserved Layer2 Bit</summary>
            Layer2 = 1 << 13,
            ///<summary>Reserved Layer3 Bit</summary>
            Layer3 = 1 << 14,
            ///<summary>Reserved LayeringOptionMain Bit</summary>
            LayeringOptionMain = 1 << 15,
            ///<summary>Reserved ShowLayer1 Bit</summary>
            ShowLayer1 = 1 << 16,
            ///<summary>Reserved ShowLayer2 Bit</summary>
            ShowLayer2 = 1 << 17,
            ///<summary>Reserved ShowLayer3 Bit</summary>
            ShowLayer3 = 1 << 18,
            ///<summary>Reserved MaterialReferences Bit</summary>
            MaterialReferences = 1 << 19,
            ///<summary>Reserved MainInput Bit</summary>
            MainInput = 1 << 20,
            ///<summary>Reserved Layer1Input Bit</summary>
            Layer1Input = 1 << 21,
            ///<summary>Reserved Layer2Input Bit</summary>
            Layer2Input = 1 << 22,
            ///<summary>Reserved Layer3Input Bit</summary>
            Layer3Input = 1 << 23,
            ///<summary>Reserved MainDetail Bit</summary>
            MainDetail = 1 << 24,
            ///<summary>Reserved Layer1Detail Bit</summary>
            Layer1Detail = 1 << 25,
            ///<summary>Reserved Layer2Detail Bit</summary>
            Layer2Detail = 1 << 26,
            ///<summary>Reserved Layer3Detail Bit</summary>
            Layer3Detail = 1 << 27,
            ///<summary>Reserved LayeringOption1 Bit</summary>
            LayeringOption1 = 1 << 28,
            ///<summary>Reserved LayeringOption2 Bit</summary>
            LayeringOption2 = 1 << 29,
            ///<summary>Reserved LayeringOption3 Bit</summary>
            LayeringOption3 = 1 << 30,

            // Note that we use the bit reserved for layered material UI in this enum, we can do this
            // because this enum will be used for ShaderGraph custom UI and we can't author layered
            // shader in shadergraph.
            ///<summary>User Bit 0</summary>
            User0 = 1 << 11,
            ///<summary>User Bit 1</summary>
            User1 = 1 << 12,
            ///<summary>User Bit 2</summary>
            User2 = 1 << 13,
            ///<summary>User Bit 3</summary>
            User3 = 1 << 14,
            ///<summary>User Bit 4</summary>
            User4 = 1 << 15,
            ///<summary>User Bit 5</summary>
            User5 = 1 << 16,
            ///<summary>User Bit 6</summary>
            User6 = 1 << 17,
            ///<summary>User Bit 7</summary>
            User7 = 1 << 18,
            ///<summary>User Bit 8</summary>
            User8 = 1 << 19,
            ///<summary>User Bit 9</summary>
            User9 = 1 << 20,
            ///<summary>User Bit 10</summary>
            User10 = 1 << 21,
            ///<summary>User Bit 11</summary>
            User11 = 1 << 22,
            ///<summary>User Bit 12</summary>
            User12 = 1 << 23,
            ///<summary>User Bit 13</summary>
            User13 = 1 << 24,
            ///<summary>User Bit 14</summary>
            User14 = 1 << 25,
            ///<summary>User Bit 15</summary>
            User15 = 1 << 26,
            ///<summary>User Bit 16</summary>
            User16 = 1 << 27,
            ///<summary>User Bit 17</summary>
            User17 = 1 << 28,
            ///<summary>User Bit 18</summary>
            User18 = 1 << 29,
            ///<summary>User Bit 19</summary>
            User19 = 1 << 30,
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
            string[] prefixes = (layerCount > 1) ? new[] {"0", "1", "2", "3"} : new[] {""};

            for (int i = 0; i < layerCount; i++)
            {
                properties[i] = FindProperty(string.Format("{0}{1}", propertyName, prefixes[i]), isMandatory);
            }

            return properties;
        }

        /// <summary>
        /// Loads the material properties for the block.
        /// </summary>
        public abstract void LoadMaterialProperties();

        /// <summary>
        /// Renders the properties in the block.
        /// </summary>
        public abstract void OnGUI();
    }
}
