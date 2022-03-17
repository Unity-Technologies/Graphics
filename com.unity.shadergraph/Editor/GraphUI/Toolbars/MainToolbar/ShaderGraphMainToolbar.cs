using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.GraphUI
{
    // TODO, INTEGRATION: A good chunk of this has to be reconfigured for the overlay implementation.
    public class ShaderGraphMainToolbar : MainToolbar
    {
        enum ColorMode
        {
            None,
            Category,
            Precision,
            UserDefined
        }
        public new static readonly string ussClassName = "ge-sg-toolbar";

        protected ToolbarButton m_SaveButton;
        protected ToolbarButton m_SaveAsButton;
        protected ToolbarButton m_ShowInProjectButton;
        protected ToolbarButton m_CheckOutButton;
        protected EnumField m_ColorModeDropdown;
        protected ToolbarButton m_ToggleBlackboardButton;
        protected ToolbarButton m_ToggleInspectorButton;
        protected ToolbarButton m_ToggleMainPreviewButton;

        public static readonly string SaveButton = "saveButton";
        public static readonly string SaveAsButton = "saveAsButton";
        public static readonly string ShowInProjectButton = "showInProjectButton";
        public static readonly string CheckOutButton = "checkOutButton";
        public static readonly string Spacer = "flexibleSpacer";
        public static readonly string ColorModeDropdown = "colorModeDropdown";
        public static readonly string ToggleBlackboardButton = "toggleBlackboardButton";
        public static readonly string ToggleGraphInspectorButton = "toggleGraphInspectorButton";
        public static readonly string ToggleMainPreviewButton = "toggleMainPreviewButton";


        public ShaderGraphMainToolbar(BaseGraphTool graphTool, GraphView graphView)
            : base(graphTool, graphView)
        {
            // Get rid of all existing visual elements defined by MainToolbar and add them in order we need here
            //Clear();

            AddToClassList(ussClassName);

            this.AddStylesheet("ShaderGraphMainToolbar.uss");
            this.AddStylesheet(EditorGUIUtility.isProSkin ? "ShaderGraphMainToolbar_dark.uss" : "ShaderGraphMainToolbar_light.uss");

            var tpl = GraphElementHelper.LoadUXML("ShaderGraphMainToolbar.uxml");
            tpl.CloneTree(this);

            m_SaveButton = this.MandatoryQ<ToolbarButton>(SaveButton);
            m_SaveButton.clickable = new Clickable(OnSaveButton);
            Add(m_SaveButton);

            m_SaveAsButton = this.MandatoryQ<ToolbarButton>(SaveAsButton);
            m_SaveAsButton.clickable = new Clickable(OnSaveAsButton);
            Add(m_SaveAsButton);

            m_ShowInProjectButton = this.MandatoryQ<ToolbarButton>(ShowInProjectButton);
            m_ShowInProjectButton.clickable = new Clickable(OnShowInProjectButton);
            Add(m_ShowInProjectButton);

            //// TODO: (Sai) Uncomment when the functionality for this button exists
            ////m_CheckOutButton = this.MandatoryQ<ToolbarButton>(CheckOutButton);
            ////m_CheckOutButton.ChangeClickEvent(OnCheckOutButton);
            ////Add(m_CheckOutButton);

            //Add(new ToolbarSpacer());

            //Add(m_Breadcrumb);

            //var flexibleSpacer = this.MandatoryQ<ToolbarSpacer>(Spacer);
            //Add(flexibleSpacer);


            //// TODO: (Sai) Uncomment when the functionality for this dropdown and these buttons exist
            ////Add(new Label("Color Mode"));

            ////m_ColorModeDropdown = this.MandatoryQ<EnumField>(ColorModeDropdown);
            ////m_ColorModeDropdown.RegisterCallback<ChangeEvent<Enum>>(OnColorModeChanged);
            ////m_ColorModeDropdown.Init(ColorMode.None);
            ////Add(m_ColorModeDropdown);
            ////
            ////m_ToggleBlackboardButton = this.MandatoryQ<ToolbarButton>(ToggleBlackboardButton);
            ////m_ToggleBlackboardButton.ChangeClickEvent(OnToggleBlackboardButton);
            ////Add(m_ToggleBlackboardButton);
            ////
            ////m_ToggleInspectorButton = this.MandatoryQ<ToolbarButton>(ToggleGraphInspectorButton);
            ////m_ToggleInspectorButton.ChangeClickEvent(OnToggleInspectorButton);
            ////Add(m_ToggleInspectorButton);
            ////
            ////m_ToggleMainPreviewButton = this.MandatoryQ<ToolbarButton>(ToggleMainPreviewButton);
            ////m_ToggleMainPreviewButton.ChangeClickEvent(OnTogglePreviewButton);
            ////Add(m_ToggleMainPreviewButton);

            Add(m_OptionsButton);
        }

        void OnSaveButton()
        {
            GraphAssetUtils.SaveImplementation(GraphTool);
        }

        void OnSaveAsButton()
        {
            GraphAssetUtils.SaveAsImplementation(GraphTool);
        }

        void OnShowInProjectButton()
        {
            // If no currently opened graph, early out
            if (GraphTool.ToolState.AssetModel == null)
                return;

            var path = GraphTool.ToolState.CurrentGraph.GetGraphAssetModelPath();
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            EditorGUIUtility.PingObject(asset);
        }

        //// TODO (Sai): Add implementation for this button
        //void OnCheckOutButton()
        //{
        //    Debug.Log("Currently the Check Out button is unimplemented!");
        //}

        //// TODO (Sai): Add implementation for this dropdown
        //void OnColorModeChanged(ChangeEvent<Enum> evt)
        //{
        //    Debug.Log("Currently the Color Mode dropdown is unimplemented!");
        //}

        //// TODO (Sai): Add implementation for this button
        //void OnToggleBlackboardButton()
        //{
        //    Debug.Log("Currently the Toggle Blackboard button is unimplemented!");
        //}

        //// TODO (Sai): Add implementation for this button
        //void OnToggleInspectorButton()
        //{
        //    Debug.Log("Currently the Toggle Inspector button is unimplemented!");
        //}

        //// TODO (Sai): Add implementation for this button
        //void OnTogglePreviewButton()
        //{
        //    Debug.Log("Currently the Toggle Preview button is unimplemented!");
        //}

        //protected override void BuildOptionMenu(GenericMenu menu)
        //{
        //    base.BuildOptionMenu(menu);
        //    /**
        //     * Additional main toolbar cog-menu items can be added here
        //     * Example:
        //     *   menu.AddSeparator("");
        //     *   MenuToggle("Auto Itemize Constants", BoolPref.AutoItemizeConstants);
        //     *   MenuToggle("Auto Itemize Variables", BoolPref.AutoItemizeVariables);
        //     **/
        //}
    }
}
