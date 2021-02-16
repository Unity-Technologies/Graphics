using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// SRPLensFlareElementEditor shows how each element in the SRP Lens Flare Asset are show in the UI
    /// </summary>
    [CustomPropertyDrawer(typeof(SRPLensFlareDataElement))]
    public class SRPLensFlareElementEditor : PropertyDrawer
    {
        static float m_Indent = 35.0f;

        /// <summary>
        /// Override this method to make your own IMGUI based GUI for the property.
        /// Draw for one element one the list of SRPLensFlareElement
        /// </summary>
        /// <param name="position">Rectangle on the screen to use for the property GUI</param>
        /// <param name="property">The SerializedProperty to make the custom GUI for.</param>
        /// <param name="label">The label of this property.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float originY = position.y;
            float offsetHeight = 1.75f * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
            Rect rect = new Rect(position.x + m_Indent, position.y, position.width - m_Indent, GUIStyle.none.lineHeight);

            SerializedProperty intensityProp = property.FindPropertyRelative("localIntensity");
            SerializedProperty positionProp = property.FindPropertyRelative("position");
            SerializedProperty lensFlareProp = property.FindPropertyRelative("lensFlareTexture");
            SerializedProperty tintProp = property.FindPropertyRelative("tint");
            SerializedProperty blendModeProp = property.FindPropertyRelative("blendMode");
            SerializedProperty sizeProp = property.FindPropertyRelative("size");
            SerializedProperty aspectRatioProp = property.FindPropertyRelative("aspectRatio");
            SerializedProperty rotationProp = property.FindPropertyRelative("rotation");
            SerializedProperty speedProp = property.FindPropertyRelative("speed");
            SerializedProperty autoRotateProp = property.FindPropertyRelative("autoRotate");
            SerializedProperty modulateByLightColor = property.FindPropertyRelative("modulateByLightColor");
            SerializedProperty isFoldOpened = property.FindPropertyRelative("isFoldOpened");

            if (lensFlareProp.objectReferenceValue != null)
            {
                float imgWidth = 1.5f * m_Indent;
                float imgOffY = 0.5f * (GetPropertyHeight(property, label) - imgWidth - GUIStyle.none.lineHeight);
                Rect imgRect = new Rect(position.x - m_Indent + 15.0f, originY + imgOffY + GUIStyle.none.lineHeight, imgWidth, imgWidth);
                EditorGUI.DrawTextureTransparent(imgRect, lensFlareProp.objectReferenceValue as Texture, ScaleMode.ScaleToFit, aspectRatioProp.floatValue);
            }
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.BeginProperty(new Rect(rect.x, rect.y, rect.width, 2.0f * rect.height), label, property);

            if (EditorGUI.BeginFoldoutHeaderGroup(new Rect(position.x + 0.25f * m_Indent, position.y, position.width - 0.25f * m_Indent, GUIStyle.none.lineHeight), isFoldOpened.boolValue, EditorGUIUtility.TrTextContent("Lens Flare Element")))
            {
                Texture tmpTex;
                if ((tmpTex = (EditorGUI.ObjectField(rect, EditorGUIUtility.TrTextContent("Flare Texture"), lensFlareProp.objectReferenceValue, typeof(Texture), false) as Texture)) != (lensFlareProp.objectReferenceValue as Texture))
                {
                    lensFlareProp.objectReferenceValue = tmpTex;
                    aspectRatioProp.floatValue = ((float)tmpTex.height) / ((float)tmpTex.width);
                    aspectRatioProp.serializedObject.ApplyModifiedProperties();
                }
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                Color tmpCol;
                if ((tmpCol = EditorGUI.ColorField(rect, EditorGUIUtility.TrTextContent("Tint"), tintProp.colorValue)) != tintProp.colorValue)
                    tintProp.colorValue = tmpCol;
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                bool tmpBool;
                if ((tmpBool = EditorGUI.Toggle(rect, EditorGUIUtility.TrTextContent("Modulate By Light Color"), modulateByLightColor.boolValue)) != modulateByLightColor.boolValue)
                    modulateByLightColor.boolValue = tmpBool;
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                float tmp;
                if ((tmp = EditorGUI.FloatField(rect, EditorGUIUtility.TrTextContent("Intensity"), intensityProp.floatValue)) != intensityProp.floatValue)
                    intensityProp.floatValue = Mathf.Max(tmp, 0.0f);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                SRPLensFlareBlendMode newBlendMode;
                SRPLensFlareBlendMode blendModeValue = (UnityEngine.SRPLensFlareBlendMode)blendModeProp.enumValueIndex;
                if ((newBlendMode = ((SRPLensFlareBlendMode)(EditorGUI.EnumPopup(rect, "Blend Mode", blendModeValue)))) != blendModeValue)
                    blendModeProp.enumValueIndex = (int)newBlendMode;
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                if ((tmp = EditorGUI.FloatField(rect, EditorGUIUtility.TrTextContent("Relative Position Scale"), positionProp.floatValue)) != positionProp.floatValue)
                    positionProp.floatValue = tmp;
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                if ((tmp = EditorGUI.FloatField(rect, EditorGUIUtility.TrTextContent("Size"), sizeProp.floatValue)) != sizeProp.floatValue)
                    sizeProp.floatValue = Mathf.Max(tmp, 1e-5f);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                if ((tmp = EditorGUI.FloatField(rect, EditorGUIUtility.TrTextContent("Aspect Ratio"), aspectRatioProp.floatValue)) != aspectRatioProp.floatValue)
                    aspectRatioProp.floatValue = Mathf.Max(tmp, 1e-5f);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                if ((tmp = EditorGUI.FloatField(rect, EditorGUIUtility.TrTextContent("Rotation"), rotationProp.floatValue)) != rotationProp.floatValue)
                    rotationProp.floatValue = Mathf.Max(tmp, 0.0f);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                if ((tmp = EditorGUI.FloatField(rect, EditorGUIUtility.TrTextContent("Speed"), speedProp.floatValue)) != speedProp.floatValue)
                    speedProp.floatValue = tmp;
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                if ((tmpBool = EditorGUI.Toggle(rect, EditorGUIUtility.TrTextContent("Auto Rotate"), autoRotateProp.boolValue)) != autoRotateProp.boolValue)
                    autoRotateProp.boolValue = tmpBool;
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                isFoldOpened.boolValue = true;
            }
            else
            {
                //rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                Texture tmpTex;
                if ((tmpTex = (EditorGUI.ObjectField(rect, EditorGUIUtility.TrTextContent("Flare Texture"), lensFlareProp.objectReferenceValue, typeof(Texture), false) as Texture)) != (lensFlareProp.objectReferenceValue as Texture))
                {
                    lensFlareProp.objectReferenceValue = tmpTex;
                    aspectRatioProp.floatValue = ((float)tmpTex.width) / ((float)tmpTex.height);
                    aspectRatioProp.serializedObject.ApplyModifiedProperties();
                }
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                Color tmpCol;
                if ((tmpCol = EditorGUI.ColorField(rect, EditorGUIUtility.TrTextContent("Tint"), tintProp.colorValue)) != tintProp.colorValue)
                    tintProp.colorValue = tmpCol;
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                float tmp;
                if ((tmp = EditorGUI.FloatField(rect, EditorGUIUtility.TrTextContent("Intensity"), intensityProp.floatValue)) != intensityProp.floatValue)
                    intensityProp.floatValue = Mathf.Max(tmp, 0.0f);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                if ((tmp = EditorGUI.FloatField(rect, EditorGUIUtility.TrTextContent("Relative Position Scale"), positionProp.floatValue)) != positionProp.floatValue)
                    positionProp.floatValue = tmp;
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                isFoldOpened.boolValue = false;
            }
            EditorGUI.EndFoldoutHeaderGroup();
            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Override this method to specify how tall the GUI for this field is in pixels
        /// </summary>
        /// <param name="property">The SerializedProperty to make the custom GUI for.</param>
        /// <param name="label">The label of this property.</param>
        /// <returns>The height in pixels.</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty isFoldOpened = property.FindPropertyRelative("isFoldOpened");

            float coef = isFoldOpened.boolValue ? 12.0f : 5.0f;

            return coef * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
        }
    }
}
