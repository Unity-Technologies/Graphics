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
            public static float indentPadding = 17;

            public static GUIContent fullScreenPassMaterial = new GUIContent("FullScreen Material", "FullScreen Material used for the full screen DrawProcedural.");
            public static GUIContent materialPassName = new GUIContent("Pass Name", "The shader pass to use for your fullscreen pass.");
            public static GUIContent fetchColorBuffer = new GUIContent("Fetch Color Buffer", "Tick this if your effect sample/fetch the camera color buffer");

            public readonly static string writeAndFetchColorBufferWarning = "Fetching and Writing to the camera color buffer at the same time is not supported on most platforms.";
            public readonly static string stencilWriteOverReservedBits = "The Stencil Write Mask of your material overwrites the bits reserved by HDRP. To avoid rendering errors, set the Write Mask to " + (int)(UserStencilUsage.AllUserBits);
            public readonly static string stencilHelpInfo = $"Stencil is enabled on the material. To help you configure the stencil operations, use these values for the bits available in HDRP: User Bit 0: {(int)UserStencilUsage.UserBit0} User Bit 1: {(int)UserStencilUsage.UserBit1}";
        }

        // Fullscreen pass
        SerializedProperty m_FullScreenPassMaterial;
        SerializedProperty m_MaterialPassName;
        SerializedProperty m_FetchColorBuffer;
        SerializedProperty m_TargetColorBuffer;
        SerializedProperty m_TargetDepthBuffer;

        bool m_ShowStencilWriteWarning = false;
        bool m_ShowStencilInfoBox = false;

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
                        if (index == -1)
                            index = 0; // Select the first pass by default when the previous pass name doesn't exist in the new material.
                        index = EditorGUI.IntPopup(rect, Styles.materialPassName, index, GetMaterialPassNames(mat), Enumerable.Range(0, mat.passCount).ToArray());
                        if (EditorGUI.EndChangeCheck())
                            m_MaterialPassName.stringValue = mat.GetPassName(index);
                    }
                    EditorGUI.EndProperty();
                    rect.y += Styles.defaultLineSpace;

                    GetStencilInfo(mat, out bool stencilEnabled, out int writeMask);

                    if (stencilEnabled && targetDepthBuffer != CustomPass.TargetBuffer.Custom)
                    {
                        if (!m_ShowStencilInfoBox)
                        {
                            m_ShowStencilInfoBox = true;
                            GUI.changed = true;
                        }
                        Rect helpBoxRect = rect;
                        helpBoxRect.height = Styles.helpBoxHeight;
                        helpBoxRect.xMin += Styles.indentPadding;
                        EditorGUI.HelpBox(helpBoxRect, Styles.stencilHelpInfo, MessageType.Info);
                        rect.y += Styles.helpBoxHeight;
                    }
                    else if (m_ShowStencilInfoBox)
                    {
                        m_ShowStencilInfoBox = false;
                        GUI.changed = true; // Workaround to update the internal state of the ReorderableList and update the height of the element.
                    }

                    if (DoesWriteMaskContainsReservedBits(writeMask))
                    {
                        if (!m_ShowStencilWriteWarning)
                        {
                            m_ShowStencilWriteWarning = true;
                            GUI.changed = true; // Workaround to update the internal state of the ReorderableList and update the height of the element.
                        }
                        Rect helpBoxRect = rect;
                        helpBoxRect.height = Styles.helpBoxHeight;
                        helpBoxRect.xMin += Styles.indentPadding;
                        EditorGUI.HelpBox(helpBoxRect, Styles.stencilWriteOverReservedBits, MessageType.Warning);
                        rect.y += Styles.helpBoxHeight;
                    }
                    else if (m_ShowStencilWriteWarning)
                    {
                        m_ShowStencilWriteWarning = false;
                        GUI.changed = true; // Workaround to update the internal state of the ReorderableList and update the height of the element.
                    }
                }
            }
        }

        bool DoesWriteMaskContainsReservedBits(int writeMask)
        {
            if (targetDepthBuffer == CustomPass.TargetBuffer.Custom)
                return false;

            return ((writeMask & (int)~UserStencilUsage.AllUserBits) != 0);
        }

        void GetStencilInfo(Material material, out bool stencilEnabled, out int writeMaskValue)
        {
            writeMaskValue = 0;
            stencilEnabled = false;

            if (material.shader == null)
                return;

            try
            {
                // Try to retrieve the serialized information of the stencil in the shader
                var serializedShader = new SerializedObject(material.shader);
                var parsed = serializedShader.FindProperty("m_ParsedForm");
                var subShaders = parsed.FindPropertyRelative("m_SubShaders");
                var subShader = subShaders.GetArrayElementAtIndex(0);
                var passes = subShader.FindPropertyRelative("m_Passes");
                var pass = passes.GetArrayElementAtIndex(0);
                var state = pass.FindPropertyRelative("m_State");
                var writeMask = state.FindPropertyRelative("stencilWriteMask");
                var readMask = state.FindPropertyRelative("stencilWriteMask");
                var reference = state.FindPropertyRelative("stencilRef");
                var stencilOpFront = state.FindPropertyRelative("stencilOpFront");
                var passOp = stencilOpFront.FindPropertyRelative("pass");
                var failOp = stencilOpFront.FindPropertyRelative("fail");
                var zFailOp = stencilOpFront.FindPropertyRelative("zFail");
                var writeMaskFloatValue = writeMask.FindPropertyRelative("val");
                var writeMaskPropertyName = writeMask.FindPropertyRelative("name");

                bool IsStencilEnabled()
                {
                    bool enabled = false;
                    enabled |= IsNotDefaultValue(reference, 0);
                    enabled |= IsNotDefaultValue(passOp, 0);
                    enabled |= IsNotDefaultValue(failOp, 0);
                    enabled |= IsNotDefaultValue(zFailOp, 0);
                    enabled |= IsNotDefaultValue(writeMask, 255);
                    enabled |= IsNotDefaultValue(readMask, 255);
                    return enabled;
                }

                bool IsNotDefaultValue(SerializedProperty prop, float defaultValue)
                {
                    var value = prop.FindPropertyRelative("val");
                    var propertyName = prop.FindPropertyRelative("name");

                    if (value.floatValue != defaultValue)
                        return true;
                    if (material.HasProperty(propertyName.stringValue))
                        return true;
                    return false;
                }

                // First check if the stencil is enabled in the shader:
                // We can do this by checking if there are any non-default values in the stencil state
                stencilEnabled = IsStencilEnabled();
                if (!stencilEnabled)
                    return;

                if (material.HasProperty(writeMaskPropertyName.stringValue))
                    writeMaskValue = (int)material.GetFloat(writeMaskPropertyName.stringValue);
                else
                    writeMaskValue = (int)writeMaskFloatValue.floatValue;
            }
            catch { }
        }

        protected override float GetPassHeight(SerializedProperty customPass)
        {
            int lineCount = (m_FullScreenPassMaterial.objectReferenceValue is Material ? 3 : 2);
            int height = (int)(Styles.defaultLineSpace * lineCount);

            height += (m_FetchColorBuffer.boolValue && targetColorBuffer == CustomPass.TargetBuffer.Camera) ? (int)Styles.helpBoxHeight : 0;
            if (m_FullScreenPassMaterial.objectReferenceValue is Material mat)
            {
                if (targetDepthBuffer == CustomPass.TargetBuffer.Camera)
                {
                    GetStencilInfo(mat, out bool stencilEnabled, out int writeMask);
                    height += stencilEnabled ? (int)Styles.helpBoxHeight : 0;
                    height += (DoesWriteMaskContainsReservedBits(writeMask)) ? (int)Styles.helpBoxHeight : 0;
                }
            }

            return height;
        }
    }
}
