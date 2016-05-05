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
    internal class VFXEdProcessingNodeBlock : VFXEdNodeBlockDraggable
    {
        public VFXBlockModel Model { get { return m_Model; } }
        private VFXProperty[] Properties { get { return Model.Properties; } }

        private VFXBlockModel m_Model;

        public VFXEdProcessingNodeBlock(VFXBlockDesc block, VFXEdDataSource dataSource) : base(dataSource)
        {
            m_Model = new VFXBlockModel(block);
            
            // For selection
            target = ScriptableObject.CreateInstance<VFXEdProcessingNodeBlockTarget>();
            (target as VFXEdProcessingNodeBlockTarget).targetNodeBlock = this;

            if (Properties != null && Properties.Length > 0)
            {
                int nbProperties = Properties.Length;
                m_Fields = new VFXUIPropertySlotField[nbProperties];
                for (int i = 0; i < nbProperties; ++i)
                {
                    m_Fields[i] = new VFXUIPropertySlotField(dataSource, Model.GetSlot(i));
                    AddChild(m_Fields[i]);
                }
            }
            else
                m_Fields = new VFXUIPropertySlotField[0];

            m_LibraryName = block.Name;

            AddChild(new VFXEdNodeBlockHeader(m_LibraryName, VFXEditor.styles.GetIcon(block.Icon == "" ? "Default" : block.Icon), block.Properties.Length > 0));
            AddManipulator(new TooltipManipulator(GetTooltipText));
            Layout();
        }

        public override void OnRemoved()
        {
            base.OnRemoved();
            Model.Detach();
        }

        public string[] GetTooltipText()
        {
            List<string> lines = new List<string>();

            //Block
            lines.Add("Node Block : " + Model.Desc.Name);
            lines.Add("Flags:" + Model.Desc.Flags);
            if(Model.Desc.Attributes != null)
            {
                lines.Add("");
                lines.Add("Attributes (" + Model.Desc.Attributes.Length + "):");

                for(int i = 0; i< Model.Desc.Attributes.Length; i++)
                {
                    lines.Add("* ("+ (Model.Desc.Attributes[i].m_Writable ? "rw":"r" ) + ") " + Model.Desc.Attributes[i].m_Name + " : " + Model.Desc.Attributes[i].m_Type);
                }
            }
            if(Model.Desc.Properties != null)
            {
                lines.Add("");
                lines.Add("Parameters (" + Model.Desc.Properties.Length + "):");
                
                for(int i = 0; i< Model.Desc.Properties.Length; i++)
                {
                    lines.Add("* " + Model.Desc.Properties[i].m_Name + " : " + Model.Desc.Properties[i].m_Type + " (" + Model.Desc.Properties[i].m_Type.ValueType + ")");
                }
            }
            lines.Add("");
            lines.Add("Source : ");
            string[] source =  Model.Desc.Source.Split(new string[] { "\t", "  " }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < source.Length; i++)
            {
                lines.Add(source[i]);
            }
            lines.Add("");
            
            // Context
            lines.Add("---");
            VFXContextModel contextmodel = Model.GetOwner();
            lines.Add("Context :" + contextmodel.GetContextType().ToString());
            lines.Add("Desc: " + Model.Desc.ToString());

            // System
            lines.Add("---");
            VFXSystemModel sysmodel = contextmodel.GetOwner();
            lines.Add("System : #" + sysmodel.Id);
            lines.Add("");
            lines.Add("Allocation Count : " + sysmodel.MaxNb);
            lines.Add("Render Priority : " + sysmodel.OrderPriority);
            lines.Add("Blend mode :" + sysmodel.BlendingMode);


            return lines.ToArray();
        }

        public override VFXPropertySlot GetSlot(string name)
        {
            for (int i = 0; i < Model.GetNbSlots(); ++i)
                if (Model.GetSlot(i).Name.Equals(name))
                    return Model.GetSlot(i);
            return null;
        }

        public override void SetSlotValue(string name, VFXValue value)
        {
            for (int i = 0; i < Model.GetNbSlots(); ++i)
                if (Model.GetSlot(i).Name.Equals(name))
                {
                    Model.GetSlot(i).Value = value;
                    break;
                }
        }

        protected override GUIStyle GetNodeBlockStyle()
        {
            return VFXEditor.styles.NodeBlock;
        }

        protected override GUIStyle GetNodeBlockSelectedStyle()
        {
            return VFXEditor.styles.NodeBlockSelected;
        }

    }
}
