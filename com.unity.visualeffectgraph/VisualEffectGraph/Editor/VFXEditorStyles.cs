using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor;
using UnityEditor.Experimental;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace UnityEditor.Experimental
{
    internal class VFXEditorStyles
    {
        public Font ImpactFont;
        public Font TooltipFont;

        public GUIStyle Empty;

        public GUIStyle Node;
        public GUIStyle NodeSelected;
        public GUIStyle NodeTitle;

        public GUIStyle NodeData;
        public GUIStyle NodeParameters;
        public GUIStyle NodeParametersTitle;

        public GUIStyle NodeInfoText;

        public GUIStyle NodeOption;


        public GUIStyle NodeBlock;
        public GUIStyle NodeBlockSelected;
        public GUIStyle DataNodeBlock;
        public GUIStyle DataNodeBlockSelected;
        public GUIStyle NodeBlockTitle;
        public GUIStyle NodeBlockParameter;
        public GUIStyle NodeBlockDropSeparator;

        public GUIStyle ForbidDrop;

        public GUIStyle Tooltip;
        public GUIStyle TooltipText;

        public GUIStyle ConnectorLeft;
        public GUIStyle ConnectorRight;

        public GUIStyle FlowConnectorIn;
        public GUIStyle FlowConnectorOut;

        public GUIStyle ConnectorOverlay;

        public GUIStyle CollapserOpen;
        public GUIStyle CollapserClosed;
        public GUIStyle CollapserDisabled;

        public GUIStyle Context;

        public GUIStyle EventNode;
        public GUIStyle EventNodeText;

        public GUIStyle InspectorHeader;

        public Texture2D FlowEdgeOpacity;
        public Texture2D FlowEdgeOpacitySelected;
        public Color FlowEdgeTint = HexColor("#AFAFAFAF");
        public Color FlowEdgeSelectedTint = HexColor("#FFFFFFFF");


        public Texture2D DataEdgeOpacity;
        public Texture2D DataEdgeOpacitySelected;

        public Texture2D ToolbarPlay;
        public Texture2D ToolbarRestart;
        public Texture2D ToolbarStop;
        public Texture2D ToolbarPause;
        public Texture2D ToolbarFrameAdvance;

        public Color DataEdgeTint = HexColor("#AFAFAF80");
        public Color DataEdgeSelectedTint = HexColor("#FFFFFFFF");

        public Texture2D DefaultBlockIcon = EditorGUIUtility.Load("icons/default.png") as Texture2D;


        private Dictionary<string, Texture2D> m_icons;
        private Dictionary<VFXEdContext, Color> m_ContextColors;

        private Dictionary<VFXValueType, Color> m_TypeColors;

        internal Texture2D GetIcon(string name) {

            if(!m_icons.ContainsKey(name)) {
                Texture2D icon = EditorGUIUtility.Load("icons/"+name+".png") as Texture2D;
                if (icon == null)
                {
                    Debug.LogError("ERROR: BlockLibrary requested icon " + name + ".png, which was not found. Using default Icon");
                    return VFXEditor.styles.DefaultBlockIcon;
                }
                m_icons.Add(name, icon);
                
            }
            return m_icons[name];
            
        }

        internal Color GetContextColor(VFXEdContext c)
        {
            if (m_ContextColors.ContainsKey(c))
            {
                return m_ContextColors[c];
            }
            else
                return Color.magenta;
        }

        internal Color GetTypeColor(VFXValueType t)
        {
            if (m_TypeColors.ContainsKey(t))
            {
                return m_TypeColors[t];
            }
            else
                return Color.magenta;
        }


        internal VFXEditorStyles()
        {
            ImpactFont = EditorGUIUtility.Load("Font/BebasNeue.otf") as Font;
            TooltipFont = EditorGUIUtility.Load("Font/gohufont-uni-14.ttf") as Font;

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
            NodeTitle.padding = new RectOffset(0, 0, 10, 0);
            NodeTitle.alignment = TextAnchor.MiddleCenter;
            NodeTitle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

            NodeInfoText = new GUIStyle();
            NodeInfoText.name = "NodeInfoText";
            NodeInfoText.fontSize = 12;
            NodeInfoText.fontStyle = FontStyle.Normal;
            NodeInfoText.padding = new RectOffset(12, 12, 12, 12);
            NodeInfoText.alignment = TextAnchor.MiddleCenter;
            NodeInfoText.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            NodeOption = new GUIStyle();
            NodeOption.name = "NodeOption";
            NodeOption.padding = new RectOffset(4, 4, 2, 6);
            NodeOption.normal.background = EditorGUIUtility.Load("NodeOption_Background.psd") as Texture2D;

            NodeData = new GUIStyle(Node);
            NodeData.name = "NodeData";
            NodeData.normal.background = EditorGUIUtility.Load("NodeBase_Data.psd") as Texture2D;

            NodeParameters = new GUIStyle(Node);
            NodeParameters.name = "NodeParameters";
            NodeParameters.normal.background = EditorGUIUtility.Load("NodeBase_Parameters.psd") as Texture2D;

            NodeParametersTitle = new GUIStyle();
            NodeParametersTitle.fontSize = 14;
            NodeParametersTitle.normal.textColor = new Color(0.25f, 0.25f, 0.25f);
            NodeParametersTitle.fontStyle = FontStyle.Bold;
            NodeParametersTitle.padding = new RectOffset(0, 0, 10, 0);
            NodeParametersTitle.alignment = TextAnchor.MiddleCenter;


            NodeBlock = new GUIStyle();
            NodeBlock.name = "NodeBlock";
            NodeBlock.normal.background = EditorGUIUtility.Load("NodeBlock_Flow_Unselected.psd") as Texture2D;
            NodeBlock.border = new RectOffset(8, 26, 12, 4);

            NodeBlockSelected = new GUIStyle();
            NodeBlockSelected.name = "NodeBlockSelected";
            NodeBlockSelected.normal.background = EditorGUIUtility.Load("NodeBlock_Flow_Selected.psd") as Texture2D;
            NodeBlockSelected.border = new RectOffset(8, 26, 12, 4);

            DataNodeBlock = new GUIStyle();
            DataNodeBlock.name = "DataNodeBlock";
            DataNodeBlock.normal.background = EditorGUIUtility.Load("NodeBlock_Unselected.psd") as Texture2D;
            DataNodeBlock.border = new RectOffset(8, 8, 4, 4);

            DataNodeBlockSelected = new GUIStyle();
            DataNodeBlockSelected.name = "DataNodeBlockSelected";
            DataNodeBlockSelected.normal.background = EditorGUIUtility.Load("NodeBlock_Selected.psd") as Texture2D;
            DataNodeBlockSelected.border = new RectOffset(8, 8, 4, 4);

            NodeBlockTitle = new GUIStyle();
            NodeBlockTitle.fontSize = 12;
            NodeBlockTitle.fontStyle = FontStyle.Bold;
            NodeBlockTitle.padding = new RectOffset(4, 4, 4, 4);
            NodeBlockTitle.alignment = TextAnchor.MiddleLeft;
            NodeBlockTitle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            NodeBlockParameter = new GUIStyle();
            NodeBlockParameter.fontSize = 12;
            NodeBlockParameter.padding = new RectOffset(4, 4, 4, 4);
            NodeBlockParameter.alignment = TextAnchor.MiddleLeft;
            NodeBlockParameter.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

            NodeBlockDropSeparator = new GUIStyle();
            NodeBlockDropSeparator.name = "NodeBlockDropSeparator";
            NodeBlockDropSeparator.normal.background = EditorGUIUtility.Load("NodeBlock_DropSeparator.psd") as Texture2D;
            NodeBlockDropSeparator.border = new RectOffset(0, 24, 0, 8);

            Tooltip = new GUIStyle();
            Tooltip.name = "Tooltip";
            Tooltip.normal.background = EditorGUIUtility.Load("Tooltip.psd") as Texture2D;
            Tooltip.border = new RectOffset(10, 10, 10, 12);

            TooltipText = new GUIStyle();
            TooltipText.name = "TooltipText";
            TooltipText.font = TooltipFont;
            TooltipText.fontSize = 14;
            TooltipText.alignment = TextAnchor.UpperLeft;
            TooltipText.normal.textColor = new Color(0.75f, 0.75f, 0.75f);

            ForbidDrop = new GUIStyle();
            ForbidDrop.name = "ForbidDrop";
            ForbidDrop.normal.background = EditorGUIUtility.Load("Forbidden.psd") as Texture2D;

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

            EventNode = new GUIStyle();
            EventNode.name = "EventNode";
            EventNode.normal.background = EditorGUIUtility.Load("BlockBase.psd") as Texture2D;
            EventNode.border = new RectOffset(9, 9, 9, 12);

            EventNodeText = new GUIStyle();
            EventNodeText.name = "EventNodeText";
            EventNodeText.font = ImpactFont;
            EventNodeText.fontSize = 64;
            EventNodeText.alignment = TextAnchor.MiddleCenter;
            EventNodeText.normal.textColor = new Color(0.75f, 0.75f, 0.75f);

            InspectorHeader = new GUIStyle("ShurikenModuleTitle");
            InspectorHeader.fontSize = 12;
            InspectorHeader.fontStyle = FontStyle.Bold;
            InspectorHeader.border = new RectOffset(4, 4, 4, 4);
            InspectorHeader.overflow = new RectOffset(4, 4, 4, 4);
            InspectorHeader.margin = new RectOffset(4, 4, 4, 8);

            FlowEdgeOpacity = EditorGUIUtility.Load("FlowEdge.psd") as Texture2D;
            FlowEdgeOpacitySelected = EditorGUIUtility.Load("FlowEdge_Selected.psd") as Texture2D;

            DataEdgeOpacity = EditorGUIUtility.Load("DataEdge.psd") as Texture2D;
            DataEdgeOpacitySelected = EditorGUIUtility.Load("DataEdge_Selected.psd") as Texture2D;

            ToolbarPlay = EditorGUIUtility.Load("ToolbarIcons.Play.png") as Texture2D;
            ToolbarRestart = EditorGUIUtility.Load("ToolbarIcons.Restart.png") as Texture2D;
            ToolbarPause = EditorGUIUtility.Load("ToolbarIcons.Pause.png") as Texture2D;
            ToolbarStop = EditorGUIUtility.Load("ToolbarIcons.Stop.png") as Texture2D;
            ToolbarFrameAdvance = EditorGUIUtility.Load("ToolbarIcons.FrameAdvance.png") as Texture2D;



            m_icons = new Dictionary<string, Texture2D>();
            GetIcon("Default");


            m_ContextColors = new Dictionary<VFXEdContext, Color>();
            
            m_ContextColors.Add(VFXEdContext.None,          HexColor("#FF0000FF"));
            m_ContextColors.Add(VFXEdContext.Trigger,       HexColor("#808080FF"));
            m_ContextColors.Add(VFXEdContext.Initialize,    HexColor("#665736FF")); 
            m_ContextColors.Add(VFXEdContext.Update,        HexColor("#364C66FF"));
            m_ContextColors.Add(VFXEdContext.Output,        HexColor("#5c4662FF"));

            m_TypeColors = new Dictionary<VFXValueType, Color>();
            m_TypeColors.Add(VFXValueType.kInt,        HexColor("#23a95cFF"));
            m_TypeColors.Add(VFXValueType.kFloat,      HexColor("#8ccf0cFF"));
            m_TypeColors.Add(VFXValueType.kFloat2,     HexColor("#FFDE00FF"));
            m_TypeColors.Add(VFXValueType.kFloat3,     HexColor("#ffb400FF"));
            m_TypeColors.Add(VFXValueType.kFloat4,     HexColor("#ff7300FF"));
            m_TypeColors.Add(VFXValueType.kTexture2D,  HexColor("#FF2288FF"));
            m_TypeColors.Add(VFXValueType.kTexture3D,  HexColor("#5555FFFF"));
            m_TypeColors.Add(VFXValueType.kNone,    HexColor("#1199FFFF"));

        }

        static Color HexColor(string hex) {
            Color output;
            ColorUtility.TryParseHtmlString(hex, out output);
            return output;
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
