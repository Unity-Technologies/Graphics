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
    internal abstract class VFXEdNodeBlock : CanvasElement
    {
        public static int Token { get { return s_Token++; } }
        private static int s_Token = 0;

        public string UniqueName { get { return m_UniqueName; } }
        public string LibraryName { get { return m_LibraryName; } }

        protected string m_LibraryName;
        protected string m_UniqueName;

        // TODO Remove that
        public DataContainer editingDataContainer;
        public VFXEdEditingWidget editingWidget;

        public VFXUIPropertySlotField[] Fields { get { return m_Fields; } }
        protected VFXUIPropertySlotField[] m_Fields;

        protected VFXEdDataSource m_DataSource;

        public VFXEdNodeBlock(VFXEdDataSource dataSource)
        {
            m_DataSource = dataSource;
            translation = Vector3.zero; // zeroed by default, will be relayouted later.
            m_Caps = Capabilities.Normal;
            m_UniqueName = GetType().Name + "_" + Token;
        }

        public abstract VFXPropertySlot GetSlot(string name);
        public abstract void SetSlotValue(string name, VFXValue value);

        public VFXUIPropertySlotField GetField(string name)
        {
            for(int i = 0; i < m_Fields.Length; i++)
            {
                if (m_Fields[i].Name == name)
                    return m_Fields[i];
            }
            return null;
        }

        public bool IsConnected()
        {
            foreach (var field in m_Fields)
            {
                if (field.IsConnected())
                    return true;
            }
            return false;
        }

        // Retrieve the full height of the block
        public virtual float GetHeight()
        {
            float height = VFXEditorMetrics.NodeBlockHeaderHeight;
            if(!collapsed)
            {
                foreach (var field in m_Fields)
                {
                    height += field.scale.y + VFXEditorMetrics.NodeBlockParameterSpacingHeight;
                }
                height += VFXEditorMetrics.NodeBlockFooterHeight;
            }
            return height;
        }

        public override void Layout()
        {
            base.Layout();

            if (collapsed)
            {
                scale = new Vector2(scale.x, VFXEditorMetrics.NodeBlockHeaderHeight);

                // if collapsed, rejoin all connectors on the middle of the header
                foreach (var field in m_Fields)
                {
                    field.translation = new Vector2(0.0f, (VFXEditorMetrics.NodeBlockHeaderHeight-VFXEditorMetrics.DataAnchorSize.y)/2);
                }
            }
            else
            {
                scale = new Vector2(scale.x, GetHeight());
                float curY = VFXEditorMetrics.NodeBlockHeaderHeight;

                foreach (var field in m_Fields)
                {
                    field.translation = new Vector2(0.0f, curY);
                    curY += field.scale.y + VFXEditorMetrics.NodeBlockParameterSpacingHeight;
                }

            }
        }

        public bool IsSelectedNodeBlock(VFXEdCanvas canvas)
        {
            if (parent is VFXEdNodeBlockContainer)
            {
                return canvas.SelectedNodeBlock == this;
            }
            else
            {
                return false;
            }
        }

        protected abstract GUIStyle GetNodeBlockSelectedStyle();
        protected abstract GUIStyle GetNodeBlockStyle();
    }
}

