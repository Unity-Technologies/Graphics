using System;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.Inspector
{
    public class SurfaceMasterNodeEditorView : AbstractNodeEditorView
    {
        VisualElement m_EditorTitle;

        new SurfaceMasterNodeEditorPresenter presenter
        {
            get { return (SurfaceMasterNodeEditorPresenter) base.presenter; }
        }

        public SurfaceMasterNodeEditorView()
        {
            AddToClassList("nodeEditor");

            var headerContainer = new VisualElement();
            headerContainer.AddToClassList("header");
            {
                m_EditorTitle = new VisualElement() {text = ""};
                m_EditorTitle.AddToClassList("title");
                headerContainer.Add(m_EditorTitle);

                var headerType = new VisualElement { text = "(node)" };
                headerType.AddToClassList("type");
                headerContainer.Add(headerType);
            }
            Add(headerContainer);

            var optionsSection = new VisualElement() {name = "surfaceOptions"};
            optionsSection.AddToClassList("section");
            {
                optionsSection.Add(new IMGUIContainer(OnGUIHandler));
            }
            Add(optionsSection);
        }

        void OnGUIHandler()
        {
            if (presenter == null)
                return;

            var options = presenter.node.options;

            EditorGUI.BeginChangeCheck();
            options.srcBlend = (SurfaceMaterialOptions.BlendMode)EditorGUILayout.EnumPopup("Src Blend", options.srcBlend);
            options.dstBlend = (SurfaceMaterialOptions.BlendMode)EditorGUILayout.EnumPopup("Dst Blend", options.dstBlend);
            options.cullMode = (SurfaceMaterialOptions.CullMode)EditorGUILayout.EnumPopup("Cull Mode", options.cullMode);
            options.zTest = (SurfaceMaterialOptions.ZTest)EditorGUILayout.EnumPopup("Z Test", options.zTest);
            options.zWrite = (SurfaceMaterialOptions.ZWrite)EditorGUILayout.EnumPopup("Z Write", options.zWrite);
            options.renderQueue = (SurfaceMaterialOptions.RenderQueue)EditorGUILayout.EnumPopup("Render Queue", options.renderQueue);
            options.renderType = (SurfaceMaterialOptions.RenderType)EditorGUILayout.EnumPopup("Render Type", options.renderType);
            if (EditorGUI.EndChangeCheck())
                presenter.node.onModified(presenter.node, ModificationScope.Graph);
        }

        public override void OnDataChanged()
        {
            if (presenter == null)
                return;

            m_EditorTitle.text = presenter.node.name;
        }
    }
}
