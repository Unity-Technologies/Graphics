using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace UnityEditor.Experimental
{
    public class VFXEditorStyles
    {
        public GUIStyle Empty;

        public GUIStyle Node;
        public GUIStyle NodeSelected;
        public GUIStyle NodeTitle;
        public GUIStyle NodeInfoText;

        public GUIStyle NodeBlock;
        public GUIStyle NodeBlockSelected;
        public GUIStyle NodeBlockTitle;
        public GUIStyle NodeBlockParameter;
        public GUIStyle NodeBlockDropSeparator;


        public GUIStyle ConnectorLeft;
        public GUIStyle ConnectorRight;

        public GUIStyle FlowConnectorIn;
        public GUIStyle FlowConnectorOut;

        public GUIStyle ConnectorOverlay;

        public GUIStyle CollapserOpen;
        public GUIStyle CollapserClosed;
        public GUIStyle CollapserDisabled;

        public GUIStyle Context;

        public Texture2D FlowEdgeOpacity;


        private Dictionary<string, Texture2D> m_icons;

        public Texture2D GetIcon(string name) {

            if(!m_icons.ContainsKey(name)) {
                Texture2D icon = EditorGUIUtility.Load("icons/"+name+".png") as Texture2D;
                if (icon == null)
                    throw new FileNotFoundException("Could not find file : icons/" + name + ".png");
                m_icons.Add(name, icon);
            }
            return m_icons[name];
        }


        public VFXEditorStyles()
        {
            Empty = new GUIStyle();
            Empty.border = new RectOffset();
            Empty.padding = new RectOffset();
            Empty.margin = new RectOffset();


            Node = new GUIStyle();
            Node.name = "Node";
            Node.normal.background = EditorGUIUtility.Load("NodeBase.psd") as Texture2D;
            Node.border = new RectOffset(9, 36, 41, 13);

            NodeSelected = new GUIStyle(Node);
            NodeSelected.name = "NodeSelected";
            NodeSelected.normal.background = EditorGUIUtility.Load("NodeBase_Selected.psd") as Texture2D;

            NodeTitle = new GUIStyle();
            NodeTitle.fontSize = 14;
            NodeTitle.fontStyle = FontStyle.Bold;
            NodeTitle.padding = new RectOffset(32, 32, 10, 0);
            NodeTitle.alignment = TextAnchor.MiddleCenter;
            NodeTitle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

            NodeInfoText = new GUIStyle();
            NodeInfoText.fontSize = 12;
            NodeInfoText.fontStyle = FontStyle.Italic;
            NodeInfoText.padding = new RectOffset(12, 12, 12, 12);
            NodeInfoText.alignment = TextAnchor.MiddleCenter;
            NodeInfoText.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            NodeBlock = new GUIStyle();
            NodeBlock.name = "NodeBlock";
            NodeBlock.normal.background = EditorGUIUtility.Load("NodeBlock_Flow_Unselected.psd") as Texture2D;
            NodeBlock.border = new RectOffset(8, 26, 12, 4);

            NodeBlockSelected = new GUIStyle();
            NodeBlockSelected.name = "NodeBlockSelected";
            NodeBlockSelected.normal.background = EditorGUIUtility.Load("NodeBlock_Flow_Selected.psd") as Texture2D;
            NodeBlockSelected.border = new RectOffset(8, 26, 12, 4);

            NodeBlockTitle = new GUIStyle();
            NodeBlockTitle.fontSize = 12;
            NodeBlockTitle.fontStyle = FontStyle.Bold;
            NodeBlockTitle.padding = new RectOffset(4, 4, 4, 4);
            NodeBlockTitle.alignment = TextAnchor.MiddleLeft;
            NodeBlockTitle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            NodeBlockParameter = new GUIStyle();
            NodeBlockParameter.fontSize = 11;
            NodeBlockParameter.padding = new RectOffset(4, 4, 4, 4);
            NodeBlockParameter.alignment = TextAnchor.MiddleLeft;
            NodeBlockParameter.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

            NodeBlockDropSeparator = new GUIStyle();
            NodeBlockDropSeparator.name = "NodeBlockDropSeparator";
            NodeBlockDropSeparator.normal.background = EditorGUIUtility.Load("NodeBlock_DropSeparator.psd") as Texture2D;
            NodeBlockDropSeparator.border = new RectOffset(0, 24, 0, 8);

            ConnectorLeft = new GUIStyle();
            ConnectorLeft.name = "ConnectorLeft";
            ConnectorLeft.normal.background = EditorGUIUtility.Load("Connector_Left.psd") as Texture2D;
            ConnectorLeft.border = new RectOffset(16, 0, 16, 0);

            ConnectorRight = new GUIStyle();
            ConnectorRight.name = "ConnectorRight";
            ConnectorRight.normal.background = EditorGUIUtility.Load("Connector_Right.psd") as Texture2D;
            ConnectorRight.border = new RectOffset(0, 16, 16, 0);

            FlowConnectorIn = new GUIStyle();
            FlowConnectorIn.name = "FlowConnectorIn";
            FlowConnectorIn.normal.background = EditorGUIUtility.Load("LayoutFlow_In.psd") as Texture2D;

            FlowConnectorOut = new GUIStyle();
            FlowConnectorOut.name = "FlowConnectorOut";
            FlowConnectorOut.normal.background = EditorGUIUtility.Load("LayoutFlow_Out.psd") as Texture2D;

            ConnectorOverlay = new GUIStyle();
            ConnectorOverlay.name = "ConnectorOverlay";
            ConnectorOverlay.normal.background = EditorGUIUtility.Load("ConnectorOverlay.psd") as Texture2D;
            ConnectorOverlay.overflow = new RectOffset(64, 64, 64 - 32, 64 - 16);

            CollapserOpen = new GUIStyle();
            CollapserOpen.name = "CollapserOpen";
            CollapserOpen.normal.background = EditorGUIUtility.Load("Collapser_Open.psd") as Texture2D;

            CollapserClosed = new GUIStyle();
            CollapserClosed.name = "CollapserClosed";
            CollapserClosed.normal.background = EditorGUIUtility.Load("Collapser_Closed.psd") as Texture2D;

            CollapserDisabled = new GUIStyle();
            CollapserDisabled.name = "CollapserDisabled";
            CollapserDisabled.normal.background = EditorGUIUtility.Load("Collapser_Disabled.psd") as Texture2D;

            Context = new GUIStyle();
            Context.name = "Context";
            Context.normal.background = EditorGUIUtility.Load("Context.psd") as Texture2D;
            Context.border = new RectOffset(8, 9, 9, 12);

            FlowEdgeOpacity = EditorGUIUtility.Load("FlowEdge.psd") as Texture2D;

            m_icons = new Dictionary<string, Texture2D>();
            GetIcon("Default");

        }

        public void ExportGUISkin()
        {
            GUISkin s = ScriptableObject.CreateInstance<GUISkin>();
            s.customStyles = new GUIStyle[20];
            s.customStyles[0] = Node;
            s.customStyles[1] = NodeTitle;
            s.customStyles[2] = NodeBlock;
            s.customStyles[3] = ConnectorLeft;
            s.customStyles[4] = ConnectorRight;
            s.customStyles[5] = FlowConnectorIn;
            s.customStyles[6] = FlowConnectorOut;
            s.customStyles[7] = CollapserOpen;
            s.customStyles[8] = CollapserClosed;

            AssetDatabase.CreateAsset(s, "Assets/VFXEditor/VFXEditor.guiskin");
        }


    }


}
