using UnityEngine;
using System;

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



    [CustomEditor(typeof(VFXEdProcessingNodeBlockTarget))]
    internal class VFXEdProcessingNodeBlockTargetEditor : Editor
    {

        public VFXEdProcessingNodeBlockTarget safeTarget { get { return target as VFXEdProcessingNodeBlockTarget; } }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical();
            GUILayout.Label(safeTarget.targetNodeBlock.name);

            int i = 0;
            foreach(VFXParamValue p in safeTarget.targetNodeBlock.ParamValues)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(safeTarget.targetNodeBlock.Params[i].m_Name + " (" + p.ValueType + ")");

                switch(p.ValueType)
                {
                    case VFXParam.Type.kTypeFloat: p.SetValue<float>(EditorGUILayout.FloatField(p.GetValue<float>())); break;
                    case VFXParam.Type.kTypeFloat2: p.SetValue<Vector2>(EditorGUILayout.Vector2Field("",p.GetValue<Vector2>())); break;
                    case VFXParam.Type.kTypeFloat3: p.SetValue<Vector3>(EditorGUILayout.Vector3Field("",p.GetValue<Vector3>())); break;
                    case VFXParam.Type.kTypeFloat4: p.SetValue<Vector4>(EditorGUILayout.Vector4Field("",p.GetValue<Vector4>())); break;
                    case VFXParam.Type.kTypeInt: p.SetValue<int>(EditorGUILayout.IntSlider(p.GetValue<int>(),-1000,1000)); break;
                    case VFXParam.Type.kTypeTexture2D: GUILayout.Label("Texture2D"); break;
                    case VFXParam.Type.kTypeTexture3D: GUILayout.Label("Texture3D"); break;
                    case VFXParam.Type.kTypeUint: p.SetValue<uint>((uint)EditorGUILayout.IntSlider((int)p.GetValue<uint>(),0,1000)); break;
                    case VFXParam.Type.kTypeUnknown: break;
                    default: break;
                }
                GUILayout.EndHorizontal();
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
            GUILayout.Label(safeTarget.targetNodeBlock.name);

            int i = 0;
            foreach(VFXParamValue p in safeTarget.targetNodeBlock.ParamValues)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(safeTarget.targetNodeBlock.Params[i].m_name + " (" + p.ValueType + ")");
                switch(p.ValueType)
                {
                    case VFXParam.Type.kTypeFloat: p.SetValue<float>(EditorGUILayout.FloatField(p.GetValue<float>())); break;
                    case VFXParam.Type.kTypeFloat2: p.SetValue<Vector2>(EditorGUILayout.Vector2Field("",p.GetValue<Vector2>())); break;
                    case VFXParam.Type.kTypeFloat3: p.SetValue<Vector3>(EditorGUILayout.Vector3Field("",p.GetValue<Vector3>())); break;
                    case VFXParam.Type.kTypeFloat4: p.SetValue<Vector4>(EditorGUILayout.Vector4Field("",p.GetValue<Vector4>())); break;
                    case VFXParam.Type.kTypeInt: p.SetValue<int>(EditorGUILayout.IntSlider(p.GetValue<int>(),-1000,1000)); break;
                    case VFXParam.Type.kTypeTexture2D: GUILayout.Label("Texture2D"); break;
                    case VFXParam.Type.kTypeTexture3D: GUILayout.Label("Texture3D"); break;
                    case VFXParam.Type.kTypeUint: p.SetValue<uint>((uint)EditorGUILayout.IntSlider((int)p.GetValue<uint>(),0,1000)); break;
                    case VFXParam.Type.kTypeUnknown: break;
                    default: break;
                }
                GUILayout.EndHorizontal();
                ++i;
            }
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
            safeTarget.targetNodeBlock.Invalidate();
            safeTarget.targetNodeBlock.ParentCanvas().Repaint();
        }
    }

}
