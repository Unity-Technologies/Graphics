using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;
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

        public void AddParameter(string name, VFXValueType type) {
            m_Parameters.Add(new VFXDataParam(name, type));
        }

    }


    public class VFXDataParam {
        public string m_Name;
        public VFXValueType m_type;
        public VFXDataParam(string name, VFXValueType type)
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
            intblock.AddParameter("Value", VFXValueType.kInt);
            m_DataBlocks.Add(intblock);

            VFXDataBlock floatblock = new VFXDataBlock("Float", "type_float", "Generic types");
            floatblock.AddParameter("Value", VFXValueType.kFloat);
            m_DataBlocks.Add(floatblock);

            VFXDataBlock v2block = new VFXDataBlock("Vector2", "type_vector2", "Generic types");
            v2block.AddParameter("Value", VFXValueType.kFloat2);
            m_DataBlocks.Add(v2block);

            VFXDataBlock v3block = new VFXDataBlock("Vector3", "type_vector3", "Generic types");
            v3block.AddParameter("Value", VFXValueType.kFloat3);
            m_DataBlocks.Add(v3block);

            VFXDataBlock v4block = new VFXDataBlock("Vector4", "type_vector4", "Generic types");
            v4block.AddParameter("Value", VFXValueType.kFloat4);
            m_DataBlocks.Add(v4block);

            VFXDataBlock colorblock = new VFXDataBlock("Color", "type_color", "Generic types");
            colorblock.AddParameter("Color", VFXValueType.kFloat3);
            colorblock.AddParameter("Alpha", VFXValueType.kFloat);
            colorblock.editingWidget = new VFXEdColorEditingWidget("Color", "Alpha");
            m_DataBlocks.Add(colorblock);

            VFXDataBlock gradientBlock = new VFXDataBlock("Gradient", "type_color", "Curves");
            gradientBlock.AddParameter("Gradient", VFXValueType.kTexture2D);
            gradientBlock.editingWidget = new VFXEdGradientEditingWidget("Gradient");
            m_DataBlocks.Add(gradientBlock);

            VFXDataBlock floatCurveBlock = new VFXDataBlock("Curve", "Curve", "Curves");
            floatCurveBlock.AddParameter("Curve", VFXValueType.kTexture2D);
            floatCurveBlock.editingWidget = new VFXEdCurveFloatEditingWidget("Curve");
            m_DataBlocks.Add(floatCurveBlock);

            VFXDataBlock vectorCurveBlock = new VFXDataBlock("Vector Curve", "Curve", "Curves");
            vectorCurveBlock.AddParameter("Curve", VFXValueType.kTexture2D);
            vectorCurveBlock.editingWidget = new VFXEdCurveVectorEditingWidget("Curve");
            m_DataBlocks.Add(vectorCurveBlock);

            VFXDataBlock texture2dblock = new VFXDataBlock("Texture2D", "Texture", "Generic types");
            texture2dblock.AddParameter("Value", VFXValueType.kTexture2D);
            m_DataBlocks.Add(texture2dblock);

            VFXDataBlock texture3dblock = new VFXDataBlock("Texture3D", "Volume", "Generic types");
            texture3dblock.AddParameter("Value", VFXValueType.kTexture3D);
            m_DataBlocks.Add(texture3dblock);

            VFXDataBlock sphereblock = new VFXDataBlock("Sphere", "Sphere", "Primitives");
            sphereblock.AddParameter("Center", VFXValueType.kFloat3);
            sphereblock.AddParameter("Radius", VFXValueType.kFloat);
            sphereblock.editingWidget = new VFXEdSphereEditingWidget("Center", "Radius");
            m_DataBlocks.Add(sphereblock);

            VFXDataBlock boxblock = new VFXDataBlock("Box", "Box", "Primitives");
            boxblock.AddParameter("Center", VFXValueType.kFloat3);
            boxblock.AddParameter("Size", VFXValueType.kFloat3);
            boxblock.editingWidget = new VFXEdBoxEditingWidget("Center", "Size");
            m_DataBlocks.Add(boxblock);

            VFXDataBlock planeblock = new VFXDataBlock("Plane", "Box", "Primitives");
            planeblock.AddParameter("Center", VFXValueType.kFloat3);
            planeblock.AddParameter("Normal", VFXValueType.kFloat3);
            planeblock.editingWidget = new VFXEdPlaneEditingWidget("Center", "Normal");
            m_DataBlocks.Add(planeblock);

            VFXDataBlock capsuleblock = new VFXDataBlock("Capsule", "Sphere", "Primitives");
            capsuleblock.AddParameter("Center", VFXValueType.kFloat3);
            capsuleblock.AddParameter("Direction", VFXValueType.kFloat3);
            capsuleblock.AddParameter("Radius", VFXValueType.kFloat);
            capsuleblock.AddParameter("Height", VFXValueType.kFloat);
            m_DataBlocks.Add(capsuleblock);

            VFXDataBlock vfblock = new VFXDataBlock("Vector Field", "Volume", "Forces");
            vfblock.AddParameter("VectorField", VFXValueType.kTexture3D);
            vfblock.AddParameter("Size", VFXValueType.kFloat3);
            vfblock.AddParameter("Offset", VFXValueType.kFloat3);
            m_DataBlocks.Add(vfblock);

        }
    }


}
