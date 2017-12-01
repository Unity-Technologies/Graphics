using System;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph.Drawing.Inspector
{
    /* public class SurfaceMasterNodeEditorView : AbstractNodeEditorView
     {
         NodeEditorHeaderView m_HeaderView;
         AbstractSurfaceMasterNode m_Node;

         public override INode node
         {
             get { return m_Node; }
             set
             {
                 if (value == m_Node)
                     return;
                 if (m_Node != null)
                     m_Node.onModified -= OnModified;
                 m_Node = value as AbstractSurfaceMasterNode;
                 OnModified(m_Node, ModificationScope.Node);
                 if (m_Node != null)
                     m_Node.onModified += OnModified;
             }
         }

         public override void Dispose()
         {
             if (m_Node != null)
                 m_Node.onModified -= OnModified;
         }

         public SurfaceMasterNodeEditorView()
         {
             AddToClassList("nodeEditor");

             m_HeaderView = new NodeEditorHeaderView { type = "node" };
             Add(m_HeaderView);

             var optionsSection = new VisualElement() { name = "surfaceOptions" };
             optionsSection.AddToClassList("section");
             {
                 optionsSection.Add(new IMGUIContainer(OnGUIHandler));
             }
             Add(optionsSection);
         }

         void OnGUIHandler()
         {
             if (m_Node == null)
                 return;

             var options = m_Node.options;

             EditorGUI.BeginChangeCheck();
             options.srcBlend = (SurfaceMaterialOptions.BlendMode)EditorGUILayout.EnumPopup("Src Blend", options.srcBlend);
             options.dstBlend = (SurfaceMaterialOptions.BlendMode)EditorGUILayout.EnumPopup("Dst Blend", options.dstBlend);
             options.cullMode = (SurfaceMaterialOptions.CullMode)EditorGUILayout.EnumPopup("Cull Mode", options.cullMode);
             options.zTest = (SurfaceMaterialOptions.ZTest)EditorGUILayout.EnumPopup("Z Test", options.zTest);
             options.zWrite = (SurfaceMaterialOptions.ZWrite)EditorGUILayout.EnumPopup("Z Write", options.zWrite);
             options.renderQueue = (SurfaceMaterialOptions.RenderQueue)EditorGUILayout.EnumPopup("Render Queue", options.renderQueue);
             options.renderType = (SurfaceMaterialOptions.RenderType)EditorGUILayout.EnumPopup("Render Type", options.renderType);
             if (EditorGUI.EndChangeCheck())
                 m_Node.onModified(m_Node, ModificationScope.Graph);
         }

         void OnModified(INode changedNode, ModificationScope scope)
         {
             if (m_Node == null)
                 return;

             m_HeaderView.title = m_Node.name;
             Dirty(ChangeType.Repaint);
         }
     }*/
}
