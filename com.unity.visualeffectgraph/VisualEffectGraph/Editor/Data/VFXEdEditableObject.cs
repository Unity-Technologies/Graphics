using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using UnityEditor.Experimental.VFX;

using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
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

    internal class VFXEdCommentTarget : VFXEdEditableObject
    {
        [SerializeField]
        public VFXEdComment targetComment;
        public VFXEdCommentTarget() { }
    }

    internal class VFXEdEventNodeTarget : VFXEdEditableObject
    {
        [SerializeField]
        public VFXEdEventNode eventNode;
        public VFXEdEventNodeTarget() { }
    }

    internal abstract class VFXCustomEditor : Editor, VFXPropertySlotObserver
    {
        public virtual void OnSlotEvent(VFXPropertySlot.Event type,VFXPropertySlot slot)
        {
            // Reinit widgets if link or transform mode has changed
            if (type == VFXPropertySlot.Event.kLinkUpdated || type == VFXPropertySlot.Event.kTransformModeUpdated)
            {
                InnerDisable(false);
                InnerEnable(false);
            }
            
            Repaint();
        }

        void OnEnable()     { InnerEnable(true); }
        void OnDisable()    { InnerDisable(true); }

        protected abstract void InnerEnable(bool registerSlots);
        protected abstract void InnerDisable(bool unregisterSlots);

        protected void ObserveSlot(VFXPropertySlot slot, bool createWidget, bool registerSlot) 
        {
            if (registerSlot)
            {
                m_ObservedSlots.Add(slot);
                slot.AddObserver(this);
            }

            if (createWidget)
            {
                VFXUIWidget widget = slot.Semantics.CreateUIWidget(slot,VFXEditor.componentTransform);
                if (widget != null)
                {
                    SceneView.onSceneGUIDelegate += widget.OnSceneGUI;
                    m_Widgets.Add(widget);
                }
            }
        }

        public void StopObservingSlots(bool unregisterSlots)
        {
            if (unregisterSlots)
            {
                foreach (var slot in m_ObservedSlots)
                    slot.RemoveObserver(this);
                m_ObservedSlots.Clear();
            }

            foreach (var widget in m_Widgets)
                SceneView.onSceneGUIDelegate -= widget.OnSceneGUI;
            m_Widgets.Clear();
        }

        private List<VFXPropertySlot> m_ObservedSlots = new List<VFXPropertySlot>();
        private List<VFXUIWidget> m_Widgets = new List<VFXUIWidget>();
    }

    [CustomEditor(typeof(VFXEdProcessingNodeBlockTarget))]
    internal class VFXEdProcessingNodeBlockTargetEditor : VFXCustomEditor
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
        }

        protected override void InnerEnable(bool registerSlots)
        {
            VFXBlockModel model = safeTarget.targetNodeBlock.Model;
            for (int i = 0; i < model.GetNbSlots(); ++i)
            {
                VFXPropertySlot slot = model.GetSlot(i);
                ObserveSlot(slot, slot.IsValueUsed(), registerSlots);
            }
        }

        protected override void InnerDisable(bool unregisterSlots)
        {
            StopObservingSlots(unregisterSlots);
        }
    }

    [CustomEditor(typeof(VFXEdDataNodeBlockTarget))]
    internal class VFXEdDataNodeBlockTargetEditor : VFXCustomEditor
    {

        public VFXEdDataNodeBlockTarget safeTarget { get { return target as VFXEdDataNodeBlockTarget; } }

        public override void OnInspectorGUI()
        {
            var block = safeTarget.targetNodeBlock;

            serializedObject.Update();

            EditorGUILayout.BeginVertical();
            GUILayout.Label(block.LibraryName, VFXEditor.styles.InspectorHeader);

            block.Model.ExposedName = EditorGUILayout.TextField("Exposed Name", block.Model.ExposedName);

            EditorGUILayout.Space();
            block.Slot.Semantics.OnInspectorGUI(block.Slot);
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        protected override void InnerEnable(bool registerSlots)
        {
            var slot = safeTarget.targetNodeBlock.Slot;
            ObserveSlot(slot, true, registerSlots);
        }

        protected override void InnerDisable(bool unregisterSlots)
        {
            StopObservingSlots(unregisterSlots);
        }
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
            EditorGUILayout.BeginVertical();

            GUILayout.Label(new GUIContent("Solver General Parameters"), VFXEditor.styles.InspectorHeader);
            EditorGUI.indentLevel++;
            
            EditorGUILayout.BoundsField( new GUIContent("Bounding Box"),bounds);

            EditorGUILayout.Space();

            VFXSystemModel system = model.GetOwner();

            system.WorldSpace = EditorGUILayout.Toggle("World Space", system.WorldSpace); 
            system.OrderPriority = EditorGUILayout.IntField("Order Priority", system.OrderPriority);            
            system.MaxNb = (uint)EditorGUILayout.DelayedIntField("Max Particles", (int)system.MaxNb);
            system.SpawnRate = EditorGUILayout.FloatField("Spawn Rate", system.SpawnRate);
            
            EditorGUILayout.Space();

            GUIContent[] options = new GUIContent[3] { new GUIContent("Masked"), new GUIContent("Additive"), new GUIContent("AlphaBlend") };

            // TODO This should be in the output context
            BlendMode mode = system.BlendingMode;
            system.BlendingMode = (BlendMode)EditorGUILayout.Popup(new GUIContent("Blend Mode : "),(int)mode,options );
            system.SoftParticlesFadeDistance = EditorGUILayout.DelayedFloatField("Soft particles fade distance", system.SoftParticlesFadeDistance);

            EditorGUILayout.Space();

            EditorGUI.indentLevel--;


            GUILayout.Label(new GUIContent(model.Desc.Name + " : Context Parameters"), VFXEditor.styles.InspectorHeader);

            for (int i = 0; i < model.GetNbSlots(); ++i)
                model.GetSlot(i).Semantics.OnInspectorGUI(model.GetSlot(i));

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
        }
    }
    
    [CustomEditor(typeof(VFXEdCommentTarget))]
    internal class VFXEdCommentTargetEditor: Editor
    {
        public VFXCommentModel model { get { return (target as VFXEdCommentTarget).targetComment.Model; } }
        public VFXEdComment node { get { return (target as VFXEdCommentTarget).targetComment; } }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            using (new GUILayout.VerticalScope())
            {
                
                GUILayout.Label(new GUIContent("Comment Parameters"), VFXEditor.styles.InspectorHeader);
                EditorGUI.indentLevel++;
                model.Title = EditorGUILayout.TextField("Title", model.Title);
                model.Color = EditorGUILayout.ColorField("Color", model.Color);
                model.Body = EditorGUILayout.TextArea(model.Body, GUILayout.MinHeight(80f));
                EditorGUI.indentLevel--;
                
            }

            if(EditorGUI.EndChangeCheck())
            {
                node.Invalidate();
                node.ParentCanvas().Repaint();
            }
        }
    }

    [CustomEditor(typeof(VFXEdEventNodeTarget))]
    internal class VFXEdEventNodeTargetEditor: Editor
    {
        public VFXEventModel model { get { return (target as VFXEdEventNodeTarget).eventNode.Model; } }
        public VFXEdEventNode node { get { return (target as VFXEdEventNodeTarget).eventNode; } }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            using (new GUILayout.VerticalScope())
            {
                
                GUILayout.Label(new GUIContent("Event"), VFXEditor.styles.InspectorHeader);
                EditorGUI.indentLevel++;
                EditorGUI.BeginDisabledGroup(model.Locked);
                model.Name = EditorGUILayout.DelayedTextField(model.Name);
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
                
            }

            if(EditorGUI.EndChangeCheck())
            {
                node.Layout();
                node.Invalidate();
                node.ParentCanvas().Repaint();
            }
        }
    }

}
