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
    internal static class VFXUIHelper
    {
        public static void SlotField(VFXPropertySlot slot)
        {
            string name = slot.Property.m_Name;
            switch (slot.ValueType)
            {
                case VFXValueType.kFloat:       slot.SetValue<float>(EditorGUILayout.FloatField(name, slot.GetValue<float>())); break;
                case VFXValueType.kFloat2:      slot.SetValue<Vector2>(EditorGUILayout.Vector2Field(name, slot.GetValue<Vector2>())); break;
                case VFXValueType.kFloat3:      slot.SetValue<Vector3>(EditorGUILayout.Vector3Field(name, slot.GetValue<Vector3>())); break;
                case VFXValueType.kFloat4:      slot.SetValue<Vector4>(EditorGUILayout.Vector4Field(name, slot.GetValue<Vector4>())); break;
                case VFXValueType.kInt:         slot.SetValue<int>(EditorGUILayout.IntSlider(slot.GetValue<int>(), -1000, 1000)); break;
                case VFXValueType.kTexture2D:   slot.SetValue<Texture2D>((Texture2D)EditorGUILayout.ObjectField(name, slot.GetValue<Texture2D>(), typeof(Texture2D))); break;
                case VFXValueType.kTexture3D:   slot.SetValue<Texture3D>((Texture3D)EditorGUILayout.ObjectField(name, slot.GetValue<Texture3D>(), typeof(Texture3D))); break;
                case VFXValueType.kUint:        slot.SetValue<uint>((uint)EditorGUILayout.IntSlider(name, (int)slot.GetValue<uint>(), 0, 1000)); break;
                case VFXValueType.kNone: break;
                default: break;
            }
        }
    }

    internal abstract class VFXEdEditableObject : ScriptableObject { }

    internal class VFXEdProcessingNodeBlockTarget : VFXEdEditableObject
    {
        [SerializeField]
        public VFXEdProcessingNodeBlock targetNodeBlock;
        public VFXEdProcessingNodeBlockTarget() { }
    }

    internal class VFXEdDataNodeBlockTarget : VFXEdEditableObject
    {
        [SerializeField]
        public VFXEdDataNodeBlock targetNodeBlock;
        public VFXEdDataNodeBlockTarget() { }

    }

    internal class VFXEdContextNodeTarget : VFXEdEditableObject
    {
        [SerializeField]
        public VFXEdContextNode targetNode;
        public VFXEdContextNodeTarget() { }
    }

    [CustomEditor(typeof(VFXEdProcessingNodeBlockTarget))]
    internal class VFXEdProcessingNodeBlockTargetEditor : Editor
    {
        public VFXEdProcessingNodeBlockTarget safeTarget { get { return target as VFXEdProcessingNodeBlockTarget; } }

        public override void OnInspectorGUI()
        {
            var block = safeTarget.targetNodeBlock;

            serializedObject.Update();

            EditorGUILayout.BeginVertical();
            GUILayout.Label(block.LibraryName, VFXEditor.styles.InspectorHeader);
            
            EditorGUILayout.Space();
            VFXBlockModel model = block.Model;

            for (int i = 0; i < model.GetNbSlots(); ++i)
                model.GetSlot(i).Semantics.OnInspectorGUI(model.GetSlot(i));

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
            block.Invalidate();
            block.ParentCanvas().Repaint();
        }

        void OnEnable()
        {
            VFXBlockModel model = safeTarget.targetNodeBlock.Model;
            for (int i = 0; i < model.GetNbSlots(); ++i)
            {
                VFXPropertySlot slot = model.GetSlot(i);
                if (slot.IsValueUsed())
                {
                    VFXUIWidget widget = slot.Semantics.CreateUIWidget(slot);
                    if (widget != null)
                    {
                        SceneView.onSceneGUIDelegate += widget.OnSceneGUI;
                        m_Widgets.Add(widget);
                    }
                }
            }
        }

        void OnDisable()
        {
            foreach (var widget in m_Widgets)
                SceneView.onSceneGUIDelegate -= widget.OnSceneGUI;
            m_Widgets.Clear();
        }

        private List<VFXUIWidget> m_Widgets = new List<VFXUIWidget>();
    }

    [CustomEditor(typeof(VFXEdDataNodeBlockTarget))]
    internal class VFXEdDataNodeBlockTargetEditor : Editor
    {

        public VFXEdDataNodeBlockTarget safeTarget { get { return target as VFXEdDataNodeBlockTarget; } }

        public override void OnInspectorGUI()
        {
            var block = safeTarget.targetNodeBlock;

            serializedObject.Update();

            EditorGUILayout.BeginVertical();
            GUILayout.Label(block.LibraryName, VFXEditor.styles.InspectorHeader);

            block.m_exposedName = EditorGUILayout.TextField("Exposed Name", block.m_exposedName);

            EditorGUILayout.Space();
            block.Slot.Semantics.OnInspectorGUI(block.Slot);
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
            block.Invalidate();
            block.ParentCanvas().Repaint();
        }

        void OnEnable()
        {
            var slot = safeTarget.targetNodeBlock.Slot;
            VFXUIWidget widget = slot.Semantics.CreateUIWidget(slot);
            if (widget != null)
            {
                SceneView.onSceneGUIDelegate += widget.OnSceneGUI;
                m_Widget = widget;
            }
        }

        void OnDisable()
        {
            if (m_Widget != null)
            {
                SceneView.onSceneGUIDelegate -= m_Widget.OnSceneGUI;
                m_Widget = null;
            }
        }

        private VFXUIWidget m_Widget;
    }

    [CustomEditor(typeof(VFXEdContextNodeTarget))]
    internal class VFXEdContextNodeTargetEditor : Editor
    {
        // TODO : remove here and stor inside VFXSystemModel
        Bounds bounds = new Bounds(Vector3.zero, new Vector3(50, 50, 50));


        bool bDebugVisible = true;
        
        public VFXContextModel model { get { return (target as VFXEdContextNodeTarget).targetNode.Model; } }
        public VFXEdContextNode node { get { return (target as VFXEdContextNodeTarget).targetNode; } }

        private void SetBlendMode(object blendMode)
        {
            model.GetOwner().BlendingMode = (BlendMode)blendMode;
        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            Color c = GUI.color;
            EditorGUILayout.BeginVertical();

            GUILayout.Label(new GUIContent("Solver General Parameters"), VFXEditor.styles.InspectorHeader);
            EditorGUI.indentLevel++;
            
            EditorGUILayout.BoundsField( new GUIContent("Bounding Box"),bounds);

            EditorGUILayout.Space();

            model.GetOwner().OrderPriority = EditorGUILayout.IntField("Order Priority", model.GetOwner().OrderPriority);            
            model.GetOwner().MaxNb = (uint)EditorGUILayout.DelayedIntField("Max Particles", (int)model.GetOwner().MaxNb);
            model.GetOwner().SpawnRate = EditorGUILayout.FloatField("Spawn Rate", model.GetOwner().SpawnRate);
            
            EditorGUILayout.Space();

            GUIContent[] options = new GUIContent[3] { new GUIContent("Masked"), new GUIContent("Additive"), new GUIContent("AlphaBlend") };

            BlendMode mode = model.GetOwner().BlendingMode;
            model.GetOwner().BlendingMode = (BlendMode)EditorGUILayout.Popup(new GUIContent("Blend Mode : "),(int)mode,options );

            EditorGUILayout.Space();

            EditorGUI.indentLevel--;


            GUILayout.Label(new GUIContent(model.Desc.Name + " : Context Parameters"), VFXEditor.styles.InspectorHeader);

            for (int i = 0; i < model.GetNbSlots(); ++i)
                VFXUIHelper.SlotField(model.GetSlot(i));

            bDebugVisible = GUILayout.Toggle(bDebugVisible, new GUIContent("Debug Information"), VFXEditor.styles.InspectorHeader);

            if(bDebugVisible)
            {
                // TODO Refactor : fix that
               /* EditorGUILayout.Space();

                GUI.color = Color.green;
                GUILayout.Label(model.GetOwner().ToString());
                GUI.color = c;

                EditorGUI.indentLevel++;
                for(int i = 0; i < model.GetOwner().GetNbChildren(); i++)
                {
                    VFXContextModel context = model.GetOwner().GetChild(i);
                    GUI.color = Color.yellow;
                    EditorGUILayout.LabelField(new GUIContent(context.GetContextType().ToString()));
                    GUI.color = c;
                    EditorGUI.indentLevel++;

                    for(int j = 0; j < context.GetNbChildren(); j++)
                    {
                        VFXBlockModel block = context.GetChild(j);
                    
                        EditorGUILayout.LabelField(new GUIContent(block.Desc.m_Name));
                        EditorGUI.indentLevel++;

                        for(int k = 0; k < block.Desc.m_Params.Length; k++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(new GUIContent( block.Desc.m_Params[k].m_Name + " (" +block.Desc.m_Params[k].m_Type.ToString()+ ")"));
                            EditorGUILayout.LabelField(new GUIContent( block.GetParamValue(k).ToString()));
                            EditorGUILayout.EndHorizontal();

                        }
                        EditorGUI.indentLevel--;
                    } 
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;*/
            }
            
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
            node.Invalidate();
            node.ParentCanvas().Repaint();

        }
    }
    
}
