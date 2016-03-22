using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXDataBlock
    {
        public Texture2D icon { get { return m_Icon; } }
        public string category { get { return m_Category; } }
        public string name {get { return m_Name; } }
        public string path { get { return category +"/"+m_Name; } }
        public List<VFXDataParam> Parameters { get { return m_Parameters; } }
        public VFXEdEditingWidget editingWidget;

        private Texture2D m_Icon;
        private string m_Category;
        private string m_Name;
        private List<VFXDataParam> m_Parameters;
        

        public VFXDataBlock(string name, string iconname, string category ) {
            m_Name = name;
            m_Icon = VFXEditor.styles.GetIcon(iconname);
            m_Category = category;
            m_Parameters = new List<VFXDataParam>();
        }

        public void AddParameter(string name, VFXParam.Type type) {
            m_Parameters.Add(new VFXDataParam(name, type));
        }

    }


    public class VFXDataParam {
        public string m_Name;
        public VFXParam.Type m_type;
        public VFXDataParam(string name, VFXParam.Type type)
        {
            m_Name = name;
            m_type = type;
        }
    }

    internal class VFXDataBlockLibraryCollection
    {
        public List<VFXDataBlock> blocks { get { return m_DataBlocks; } }
        private List<VFXDataBlock> m_DataBlocks;

        public VFXDataBlockLibraryCollection() {
            m_DataBlocks = new List<VFXDataBlock>();
        }

        public List<VFXDataBlock> GetBlocks() {
            return blocks;
        }

        public VFXDataBlock GetBlock(string name)
        {
            return blocks.Find(n => n.name == name);
        }

        public void Load() {
            m_DataBlocks.Clear();

            // TODO : Add Blocks Here
            VFXDataBlock intblock = new VFXDataBlock("Int", "type_integer", "Generic types");
            intblock.AddParameter("Value", VFXParam.Type.kTypeInt);
            m_DataBlocks.Add(intblock);

            VFXDataBlock floatblock = new VFXDataBlock("Float", "type_float", "Generic types");
            floatblock.AddParameter("Value", VFXParam.Type.kTypeFloat);
            m_DataBlocks.Add(floatblock);

            VFXDataBlock v2block = new VFXDataBlock("Vector2", "type_vector2", "Generic types");
            v2block.AddParameter("Value", VFXParam.Type.kTypeFloat2);
            m_DataBlocks.Add(v2block);

            VFXDataBlock v3block = new VFXDataBlock("Vector3", "type_vector3", "Generic types");
            v3block.AddParameter("Value", VFXParam.Type.kTypeFloat3);
            m_DataBlocks.Add(v3block);

            VFXDataBlock v4block = new VFXDataBlock("Vector4", "type_vector4", "Generic types");
            v4block.AddParameter("Value", VFXParam.Type.kTypeFloat4);
            m_DataBlocks.Add(v4block);

            VFXDataBlock colorblock = new VFXDataBlock("Color", "type_color", "Generic types");
            colorblock.AddParameter("Color", VFXParam.Type.kTypeFloat3);
            colorblock.AddParameter("Alpha", VFXParam.Type.kTypeFloat);
            colorblock.editingWidget = new VFXEdColorEditingWidget("Color", "Alpha");
            m_DataBlocks.Add(colorblock);

            VFXDataBlock gradientBlock = new VFXDataBlock("Gradient", "type_color", "Generic types");
            gradientBlock.AddParameter("Gradient", VFXParam.Type.kTypeTexture2D);
            gradientBlock.editingWidget = new VFXEdGradientEditingWidget("Gradient");
            m_DataBlocks.Add(gradientBlock);

            VFXDataBlock texture2dblock = new VFXDataBlock("Texture2D", "Texture", "Generic types");
            texture2dblock.AddParameter("Value", VFXParam.Type.kTypeTexture2D);
            m_DataBlocks.Add(texture2dblock);

            VFXDataBlock texture3dblock = new VFXDataBlock("Texture3D", "Volume", "Generic types");
            texture3dblock.AddParameter("Value", VFXParam.Type.kTypeTexture3D);
            m_DataBlocks.Add(texture3dblock);

            VFXDataBlock sphereblock = new VFXDataBlock("Sphere", "Sphere", "Primitives");
            sphereblock.AddParameter("Center", VFXParam.Type.kTypeFloat3);
            sphereblock.AddParameter("Radius", VFXParam.Type.kTypeFloat);
            sphereblock.editingWidget = new VFXEdSphereEditingWidget("Center", "Radius");
            m_DataBlocks.Add(sphereblock);

            VFXDataBlock boxblock = new VFXDataBlock("Box", "Box", "Primitives");
            boxblock.AddParameter("Center", VFXParam.Type.kTypeFloat3);
            boxblock.AddParameter("Size", VFXParam.Type.kTypeFloat3);
            boxblock.editingWidget = new VFXEdBoxEditingWidget("Center", "Size");
            m_DataBlocks.Add(boxblock);

            VFXDataBlock planeblock = new VFXDataBlock("Plane", "Box", "Primitives");
            planeblock.AddParameter("Center", VFXParam.Type.kTypeFloat3);
            planeblock.AddParameter("Normal", VFXParam.Type.kTypeFloat3);
            planeblock.editingWidget = new VFXEdPlaneEditingWidget("Center", "Normal");
            m_DataBlocks.Add(planeblock);

            VFXDataBlock capsuleblock = new VFXDataBlock("Capsule", "Sphere", "Primitives");
            capsuleblock.AddParameter("Center", VFXParam.Type.kTypeFloat3);
            capsuleblock.AddParameter("Direction", VFXParam.Type.kTypeFloat3);
            capsuleblock.AddParameter("Radius", VFXParam.Type.kTypeFloat);
            capsuleblock.AddParameter("Height", VFXParam.Type.kTypeFloat);
            m_DataBlocks.Add(capsuleblock);

            VFXDataBlock vfblock = new VFXDataBlock("Vector Field", "Volume", "Forces");
            vfblock.AddParameter("VectorField", VFXParam.Type.kTypeTexture3D);
            vfblock.AddParameter("Size", VFXParam.Type.kTypeFloat3);
            vfblock.AddParameter("Offset", VFXParam.Type.kTypeFloat3);
            m_DataBlocks.Add(vfblock);

        }
    }


}
