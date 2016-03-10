using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
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

    [CustomEditor(typeof(VFXEdProcessingNodeBlockTarget))]
    internal class VFXEdProcessingNodeBlockTargetEditor : Editor
    {

        public VFXEdProcessingNodeBlockTarget safeTarget { get { return target as VFXEdProcessingNodeBlockTarget; } }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical();
            GUILayout.Label(safeTarget.targetNodeBlock.name, VFXEditor.styles.InspectorHeader);


            
            EditorGUILayout.Space();
            int i = 0;
            foreach(VFXParamValue p in safeTarget.targetNodeBlock.ParamValues)
            {

                switch(p.ValueType)
                {
                    case VFXParam.Type.kTypeFloat: p.SetValue<float>(EditorGUILayout.FloatField(safeTarget.targetNodeBlock.Params[i].m_Name, p.GetValue<float>())); break;
                    case VFXParam.Type.kTypeFloat2: p.SetValue<Vector2>(EditorGUILayout.Vector2Field(safeTarget.targetNodeBlock.Params[i].m_Name,p.GetValue<Vector2>())); break;
                    case VFXParam.Type.kTypeFloat3: p.SetValue<Vector3>(EditorGUILayout.Vector3Field(safeTarget.targetNodeBlock.Params[i].m_Name,p.GetValue<Vector3>())); break;
                    case VFXParam.Type.kTypeFloat4: p.SetValue<Vector4>(EditorGUILayout.Vector4Field(safeTarget.targetNodeBlock.Params[i].m_Name,p.GetValue<Vector4>())); break;
                    case VFXParam.Type.kTypeInt: p.SetValue<int>(EditorGUILayout.IntSlider(p.GetValue<int>(),-1000,1000)); break;
                    case VFXParam.Type.kTypeTexture2D: p.SetValue<Texture2D>((Texture2D)EditorGUILayout.ObjectField(safeTarget.targetNodeBlock.Params[i].m_Name,p.GetValue<Texture2D>(),typeof(Texture2D)));  break;
                    case VFXParam.Type.kTypeTexture3D: p.SetValue<Texture3D>((Texture3D)EditorGUILayout.ObjectField(safeTarget.targetNodeBlock.Params[i].m_Name,p.GetValue<Texture3D>(),typeof(Texture3D)));  break;
                    case VFXParam.Type.kTypeUint: p.SetValue<uint>((uint)EditorGUILayout.IntSlider(safeTarget.targetNodeBlock.Params[i].m_Name,(int)p.GetValue<uint>(),0,1000)); break;
                    case VFXParam.Type.kTypeUnknown: break;
                    default: break;
                }
                ++i;
            }
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
            safeTarget.targetNodeBlock.Invalidate();
            safeTarget.targetNodeBlock.ParentCanvas().Repaint();
        }
    }

    [CustomEditor(typeof(VFXEdDataNodeBlockTarget))]
    internal class VFXEdDataNodeBlockTargetEditor : Editor
    {

        public VFXEdDataNodeBlockTarget safeTarget { get { return target as VFXEdDataNodeBlockTarget; } }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical();
            GUILayout.Label(safeTarget.targetNodeBlock.name, VFXEditor.styles.InspectorHeader);


            safeTarget.targetNodeBlock.m_exposedName = EditorGUILayout.TextField("Exposed Name",safeTarget.targetNodeBlock.m_exposedName);
            EditorGUILayout.Space();

            if(safeTarget.targetNodeBlock.editingWidget != null)
            {
                safeTarget.targetNodeBlock.editingWidget.OnInspectorGUI();
            }
            else
            {
                int i = 0;
                foreach(VFXParamValue p in safeTarget.targetNodeBlock.ParamValues)
                {

                    switch(p.ValueType)
                    {
                        case VFXParam.Type.kTypeFloat: p.SetValue<float>(EditorGUILayout.FloatField(safeTarget.targetNodeBlock.Params[i].m_Name, p.GetValue<float>())); break;
                        case VFXParam.Type.kTypeFloat2: p.SetValue<Vector2>(EditorGUILayout.Vector2Field(safeTarget.targetNodeBlock.Params[i].m_Name,p.GetValue<Vector2>())); break;
                        case VFXParam.Type.kTypeFloat3: p.SetValue<Vector3>(EditorGUILayout.Vector3Field(safeTarget.targetNodeBlock.Params[i].m_Name,p.GetValue<Vector3>())); break;
                        case VFXParam.Type.kTypeFloat4: p.SetValue<Vector4>(EditorGUILayout.Vector4Field(safeTarget.targetNodeBlock.Params[i].m_Name,p.GetValue<Vector4>())); break;
                        case VFXParam.Type.kTypeInt: p.SetValue<int>(EditorGUILayout.IntSlider(p.GetValue<int>(),-1000,1000)); break;
                        case VFXParam.Type.kTypeTexture2D: p.SetValue<Texture2D>((Texture2D)EditorGUILayout.ObjectField(safeTarget.targetNodeBlock.Params[i].m_Name,p.GetValue<Texture2D>(),typeof(Texture2D)));  break;
                        case VFXParam.Type.kTypeTexture3D: p.SetValue<Texture3D>((Texture3D)EditorGUILayout.ObjectField(safeTarget.targetNodeBlock.Params[i].m_Name,p.GetValue<Texture3D>(),typeof(Texture3D)));  break;
                        case VFXParam.Type.kTypeUint: p.SetValue<uint>((uint)EditorGUILayout.IntSlider(safeTarget.targetNodeBlock.Params[i].m_Name,(int)p.GetValue<uint>(),0,1000)); break;
                        case VFXParam.Type.kTypeUnknown: break;
                        default: break;
                    }
                    ++i;
                }
            }
            
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
            safeTarget.targetNodeBlock.Invalidate();
            safeTarget.targetNodeBlock.ParentCanvas().Repaint();
        }

        void OnEnable()
        {
            if(safeTarget.targetNodeBlock.editingWidget != null)
            {
                safeTarget.targetNodeBlock.editingWidget.CreateBinding(safeTarget.targetNodeBlock);
                SceneView.onSceneGUIDelegate += safeTarget.targetNodeBlock.editingWidget.OnSceneGUI;
            }
                
        }
 
        void OnDisable()
        {
            if (safeTarget.targetNodeBlock.editingWidget != null)
                SceneView.onSceneGUIDelegate -= safeTarget.targetNodeBlock.editingWidget.OnSceneGUI;
        }

    }

    [CustomEditor(typeof(VFXEdContextNodeTarget))]
    internal class VFXEdContextNodeTargetEditor : Editor
    {
        // TODO : remove here and stor inside VFXSystemModel
        Bounds bounds = new Bounds(Vector3.zero, new Vector3(50, 50, 50));


        bool bDebugVisible = true;
        
        public VFXContextModel model { get { return (target as VFXEdContextNodeTarget).targetNode.Model; } }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            Color c = GUI.color;
            EditorGUILayout.BeginVertical();

            GUILayout.Label(new GUIContent("Solver General Parameters"), VFXEditor.styles.InspectorHeader);
            EditorGUI.indentLevel++;
            EditorGUILayout.BoundsField( new GUIContent("Bounding Box"),bounds);
            EditorGUILayout.Space();
            model.GetOwner().MaxNb = (uint)EditorGUILayout.DelayedIntField("Max Particles", (int)model.GetOwner().MaxNb);
            model.GetOwner().SpawnRate = EditorGUILayout.FloatField("Spawn Rate", model.GetOwner().SpawnRate);
            EditorGUILayout.Space();
            EditorGUI.indentLevel--;


            GUILayout.Label(new GUIContent(model.Desc.Name + " : Context Parameters"), VFXEditor.styles.InspectorHeader);

            for(int i = 0; i < model.GetNbParamValues(); i++)
            {
                VFXParamValue value = model.GetParamValue(i);
                VFXParam parm = model.Desc.m_Params[i];

                switch(value.ValueType)
                {
                    case VFXParam.Type.kTypeFloat: value.SetValue<float>(EditorGUILayout.FloatField(parm.m_Name, value.GetValue<float>())); break;
                    case VFXParam.Type.kTypeFloat2: value.SetValue<Vector2>(EditorGUILayout.Vector2Field(parm.m_Name,value.GetValue<Vector2>())); break;
                    case VFXParam.Type.kTypeFloat3: value.SetValue<Vector3>(EditorGUILayout.Vector3Field(parm.m_Name,value.GetValue<Vector3>())); break;
                    case VFXParam.Type.kTypeFloat4: value.SetValue<Vector4>(EditorGUILayout.Vector4Field(parm.m_Name,value.GetValue<Vector4>())); break;
                    case VFXParam.Type.kTypeInt: value.SetValue<int>(EditorGUILayout.IntSlider(parm.m_Name,value.GetValue<int>(),-1000,1000)); break;
                    case VFXParam.Type.kTypeTexture2D: value.SetValue<Texture2D>((Texture2D)EditorGUILayout.ObjectField(parm.m_Name,value.GetValue<Texture2D>(),typeof(Texture2D)));  break;
                    case VFXParam.Type.kTypeTexture3D: value.SetValue<Texture3D>((Texture3D)EditorGUILayout.ObjectField(parm.m_Name,value.GetValue<Texture3D>(),typeof(Texture3D)));  break;
                    case VFXParam.Type.kTypeUint: value.SetValue<uint>((uint)EditorGUILayout.IntSlider(parm.m_Name,(int)value.GetValue<uint>(),0,1000)); break;
                    case VFXParam.Type.kTypeUnknown: break;
                    default: break;
                }
            }


            bDebugVisible = GUILayout.Toggle(bDebugVisible, new GUIContent("Debug Information"), VFXEditor.styles.InspectorHeader);

            if(bDebugVisible)
            {

                EditorGUILayout.Space();

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
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();

        }
    }


    /*[CustomEditor(typeof(VFXEdOutputNodeTarget))]
    internal class VFXEdOutputNodeTargetEditor : Editor
    {

        bool bDebugVisible = true;
        
        public VFXContextModel model { get { return (target as VFXEdContextNodeTarget).targetNode.Model; } }

        static readonly string[] AxisDefinitions = { "Default", "Camera Up" , "Velocity" , "Attribute" , "Custom Vector" };
        static readonly int[] AxisDefinitionValues = { 0 , 1 , 2 , 3 , 4 };

  
        int UAxisDefinition = 0;
        int VAxisDefinition = 1;

        string UAxisAttributeName = "direction";
        string VAxisAttributeName = "direction";
        Vector3 UAxisCustomVector = new Vector3();
        Vector3 VAxisCustomVector = new Vector3();


        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            Color c = GUI.color;
            EditorGUILayout.BeginVertical();

            GUILayout.Label(new GUIContent("Output Parameters"), VFXEditor.styles.InspectorHeader);
            
            UAxisDefinition = EditorGUILayout.IntPopup("U Axis Orientation : ", UAxisDefinition, AxisDefinitions, AxisDefinitionValues);
            VAxisDefinition = EditorGUILayout.IntPopup("V Axis Orientation : ",VAxisDefinition, AxisDefinitions, AxisDefinitionValues);
            
            // OPT Settings
            switch(UAxisDefinition)
            {
                case 3: UAxisAttributeName = EditorGUILayout.TextField("U Axis Orientation Attribute", UAxisAttributeName); break;
                case 4: UAxisCustomVector = EditorGUILayout.Vector3Field("U Axis Orientation Vector", UAxisCustomVector); break;
                default: break;
            }

            switch(VAxisDefinition)
            {
                case 3: UAxisAttributeName = EditorGUILayout.TextField("U Axis Orientation Attribute", UAxisAttributeName); break;
                case 4: UAxisCustomVector = EditorGUILayout.Vector3Field("U Axis Orientation Vector", UAxisCustomVector); break;
                default: break;
            }
            EditorGUI.indentLevel--;


            bDebugVisible = GUILayout.Toggle(bDebugVisible, new GUIContent("Debug Information"), VFXEditor.styles.InspectorHeader);

            if(bDebugVisible)
            {

                EditorGUILayout.Space();

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
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();

        }
    }*/

}
