using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;
using System;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// FullScreen custom pass drawer
    /// </summary>
    [CustomPassDrawerAttribute(typeof(FullScreenCustomPass))]
    class FullScreenCustomPassDrawer : CustomPassDrawer
    {
        private class Styles
        {
            public static float defaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            public static float helpBoxHeight = EditorGUIUtility.singleLineHeight * 2;

            public static GUIContent fullScreenPassMaterial = new GUIContent("FullScreen Material", "FullScreen Material used for the full screen DrawProcedural.");
            public static GUIContent materialPassName = new GUIContent("Pass Name", "The shader pass to use for your fullscreen pass.");
            public static GUIContent fetchColorBuffer = new GUIContent("Fetch Color Buffer", "Tick this if your effect sample/fetch the camera color buffer");

            public readonly static string writeAndFetchColorBufferWarning = "Fetching and Writing to the camera color buffer at the same time is not supported on most platforms.";
            public readonly static string stencilWriteOverReservedBits = "The Stencil Write Mask of your material overwrites the bits reserved by HDRP. To avoid rendering errors, set the Write Mask to " + (int)(UserStencilUsage.UserBit0 | UserStencilUsage.UserBit1);
        }

        // Fullscreen pass
        SerializedProperty m_FullScreenPassMaterial;
        SerializedProperty m_MaterialPassName;
        SerializedProperty m_FetchColorBuffer;
        SerializedProperty m_TargetColorBuffer;
        SerializedProperty m_TargetDepthBuffer;

        CustomPass.TargetBuffer targetColorBuffer => (CustomPass.TargetBuffer)m_TargetColorBuffer.intValue;
        CustomPass.TargetBuffer targetDepthBuffer => (CustomPass.TargetBuffer)m_TargetDepthBuffer.intValue;

        protected override void Initialize(SerializedProperty customPass)
        {
            m_FullScreenPassMaterial = customPass.FindPropertyRelative("fullscreenPassMaterial");
            m_MaterialPassName = customPass.FindPropertyRelative("materialPassName");
            m_FetchColorBuffer = customPass.FindPropertyRelative("fetchColorBuffer");
            m_TargetColorBuffer = customPass.FindPropertyRelative("targetColorBuffer");
            m_TargetDepthBuffer = customPass.FindPropertyRelative("targetDepthBuffer");
        }

        protected override void DoPassGUI(SerializedProperty customPass, Rect rect)
        {
            EditorGUI.PropertyField(rect, m_FetchColorBuffer, Styles.fetchColorBuffer);
            rect.y += Styles.defaultLineSpace;

            if (m_FetchColorBuffer.boolValue && targetColorBuffer == CustomPass.TargetBuffer.Camera)
            {
                // We add a warning to prevent fetching and writing to the same render target
                Rect helpBoxRect = rect;
                helpBoxRect.height = Styles.helpBoxHeight;
                EditorGUI.HelpBox(helpBoxRect, Styles.writeAndFetchColorBufferWarning, MessageType.Warning);
                rect.y += Styles.helpBoxHeight;
            }

            EditorGUI.PropertyField(rect, m_FullScreenPassMaterial, Styles.fullScreenPassMaterial);
            rect.y += Styles.defaultLineSpace;
            if (m_FullScreenPassMaterial.objectReferenceValue is Material mat)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUI.BeginProperty(rect, Styles.materialPassName, m_MaterialPassName);
                    {
                        EditorGUI.BeginChangeCheck();
                        int index = mat.FindPass(m_MaterialPassName.stringValue);
                        index = EditorGUI.IntPopup(rect, Styles.materialPassName, index, GetMaterialPassNames(mat), Enumerable.Range(0, mat.passCount).ToArray());
                        if (EditorGUI.EndChangeCheck())
                            m_MaterialPassName.stringValue = mat.GetPassName(index);
                    }
                    EditorGUI.EndProperty();
                    rect.y += Styles.defaultLineSpace;

                    if (DoesWriteMaskContainsReservedBits(mat))
                    {
                        Rect helpBoxRect = rect;
                        helpBoxRect.height = Styles.helpBoxHeight;
                        EditorGUI.HelpBox(helpBoxRect, Styles.stencilWriteOverReservedBits, MessageType.Warning);
                        rect.y += Styles.helpBoxHeight;
                    }
                    // TODO: get the material stencil property and show a warning if writing to HDRP bits
                }
            }
        }

        bool DoesWriteMaskContainsReservedBits(Material material)
        {
            if (targetDepthBuffer == CustomPass.TargetBuffer.Custom)
                return false;

            int writeMask = GetStencilWriteMask(material);
            return ((writeMask & (int)~(UserStencilUsage.UserBit0 | UserStencilUsage.UserBit1)) != 0);
        }

        int GetStencilWriteMask(Material material)
        {
            if (material.shader == null)
                return 0;

            var serializedShader = new SerializedObject(material.shader);
            var parsed = serializedShader.FindProperty("m_ParsedForm");
            var subShaders = parsed.FindPropertyRelative("m_SubShaders");
            var subShader = subShaders.GetArrayElementAtIndex(0);
            var passes = subShader.FindPropertyRelative("m_Passes");
            var pass = passes.GetArrayElementAtIndex(0);
            var state = pass.FindPropertyRelative("m_State");
            var writeMask = state.FindPropertyRelative("stencilWriteMask");
            var writeMaskFloatValue = writeMask.FindPropertyRelative("val");
            var writeMaskPropertyName = writeMask.FindPropertyRelative("name");

            if (material.HasProperty(writeMaskPropertyName.stringValue))
                return (int)material.GetFloat(writeMaskPropertyName.stringValue);
            else
                return (int)writeMaskFloatValue.floatValue;
        }

        protected override float GetPassHeight(SerializedProperty customPass)
        {
            int lineCount = (m_FullScreenPassMaterial.objectReferenceValue is Material ? 3 : 2);
            int height = (int)(Styles.defaultLineSpace * lineCount);

            height += (m_FetchColorBuffer.boolValue && targetColorBuffer == CustomPass.TargetBuffer.Camera) ? (int)Styles.helpBoxHeight : 0;
            if (m_FullScreenPassMaterial.objectReferenceValue is Material mat)
                height += (DoesWriteMaskContainsReservedBits(mat)) ? (int)Styles.helpBoxHeight : 0;

            return height;
        }
    }
}
