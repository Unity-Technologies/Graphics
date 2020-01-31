using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEditor.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public abstract class MaterialUIBlock
    {
        protected MaterialEditor        materialEditor;
        protected Material[]            materials;
        protected MaterialProperty[]    properties;

        MaterialUIBlockList             parent;

        [Flags]
        public enum Expandable : uint
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

        public void         Initialize(MaterialEditor materialEditor, MaterialProperty[] properties, MaterialUIBlockList parent)
        {
            this.materialEditor = materialEditor;
            this.parent = parent;
            materials = materialEditor.targets.Select(target => target as Material).ToArray();

            // We should always register the key used to keep collapsable state
            materialEditor.InitExpandableState();
        }

        public void         UpdateMaterialProperties(MaterialProperty[] properties)
        {
            this.properties = properties;
            LoadMaterialProperties();
        }

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

        protected T FetchUIBlockInCurrentList< T >() where T : MaterialUIBlock
        {
            return parent.FetchUIBlock< T >();
        }
        
        public abstract void LoadMaterialProperties();
        public abstract void OnGUI();
    }
}