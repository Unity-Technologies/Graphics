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

        public List<string> GetTooltipText()
        {
            List<string> lines = new List<string>();
            lines = VFXModelDebugInfoProvider.GetInfo(lines, Model, VFXModelDebugInfoProvider.InfoFlag.kDefault);
            return lines;
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
