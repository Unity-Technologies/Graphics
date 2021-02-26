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
        static float m_Indent = 0.0f;

        private float m_LastOffset = 0.0f;
        private Rect m_CurrentRect;

        private void InitFirstRect(Rect position)
        {
            m_CurrentRect = new Rect(position.x + m_Indent, position.y, position.width - m_Indent, GUIStyle.none.lineHeight);
        }

        private Rect GetNextRect(float xOffset = 0.0f)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            m_CurrentRect.y += lineHeight;

            if (m_LastOffset != 0.0f)
            {
                m_CurrentRect.x -= xOffset;
                m_CurrentRect.width += xOffset;
                m_LastOffset = 0.0f;
            }

            if (xOffset != 0.0f)
            {
                m_CurrentRect.x += xOffset;
                m_CurrentRect.width -= xOffset;
                m_LastOffset = xOffset;
            }

            return m_CurrentRect;
        }

        /// <summary>
        /// Override this method to make your own IMGUI based GUI for the property.
        /// Draw for one element one the list of SRPLensFlareElement
        /// </summary>
        /// <param name="position">Rectangle on the screen to use for the property GUI</param>
        /// <param name="property">The SerializedProperty to make the custom GUI for.</param>
        /// <param name="label">The label of this property.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float originX = position.x;
            float offsetHeight = 1.75f * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
            //Rect rect = new Rect(position.x + m_Indent, position.y, position.width - m_Indent, GUIStyle.none.lineHeight);
            InitFirstRect(position);

            SerializedProperty intensityProp = property.FindPropertyRelative("localIntensity");
            SerializedProperty positionProp = property.FindPropertyRelative("position");
            SerializedProperty positionOffsetProp = property.FindPropertyRelative("positionOffset");
            SerializedProperty angularOffsetProp = property.FindPropertyRelative("angularOffset");
            SerializedProperty translationScaleProp = property.FindPropertyRelative("translationScale");
            SerializedProperty lensFlareProp = property.FindPropertyRelative("lensFlareTexture");
            SerializedProperty tintProp = property.FindPropertyRelative("tint");
            SerializedProperty blendModeProp = property.FindPropertyRelative("blendMode");
            SerializedProperty sizeProp = property.FindPropertyRelative("size");
            SerializedProperty aspectRatioProp = property.FindPropertyRelative("aspectRatio");
            SerializedProperty countProp = property.FindPropertyRelative("count");
            SerializedProperty rotationProp = property.FindPropertyRelative("rotation");
            SerializedProperty speedProp = property.FindPropertyRelative("speed");
            SerializedProperty autoRotateProp = property.FindPropertyRelative("autoRotate");
            SerializedProperty preserveAspectRatioProp = property.FindPropertyRelative("preserveAspectRatio");
            SerializedProperty modulateByLightColor = property.FindPropertyRelative("modulateByLightColor");
            SerializedProperty isFoldOpened = property.FindPropertyRelative("isFoldOpened");

            //
            SerializedProperty distributionProp = property.FindPropertyRelative("distribution");

            SerializedProperty lengthSpreadProp = property.FindPropertyRelative("lengthSpread");
            SerializedProperty colorGradientProp = property.FindPropertyRelative("colorGradient");
            SerializedProperty positionCurveProp = property.FindPropertyRelative("positionCurve");
            SerializedProperty scaleCurveProp = property.FindPropertyRelative("scaleCurve");

            // Random
            SerializedProperty seedProp = property.FindPropertyRelative("seed");
            SerializedProperty intensityVariationProp = property.FindPropertyRelative("intensityVariation");
            SerializedProperty positionVariationProp = property.FindPropertyRelative("positionVariation");
            SerializedProperty scaleVariationProp = property.FindPropertyRelative("scaleVariation");
            SerializedProperty sizeVariationProp = property.FindPropertyRelative("sizeVariation");
            SerializedProperty rotationVariationProp = property.FindPropertyRelative("rotationVariation");

            if (lensFlareProp.objectReferenceValue != null)
            {
                float imgWidth = 1.5f * m_Indent;
                float imgOffX = 0.5f * (GetPropertyHeight(property, label) - imgWidth - GUIStyle.none.lineHeight);
                //Rect imgRect = new Rect(position.x - m_Indent + 15.0f, originY + imgOffY + GUIStyle.none.lineHeight, imgWidth, imgWidth);
                Rect imgRect = new Rect(m_CurrentRect.x, m_CurrentRect.y, imgWidth, imgWidth);
                Texture texture = lensFlareProp.objectReferenceValue as Texture;
                float usedAspectRatio = preserveAspectRatioProp.boolValue ? (((float)texture.width) / ((float)texture.height)) : aspectRatioProp.floatValue;
                EditorGUI.DrawTextureTransparent(imgRect, lensFlareProp.objectReferenceValue as Texture, ScaleMode.ScaleToFit, usedAspectRatio);
            }
            Rect rect = m_CurrentRect;
            m_CurrentRect.y += 1.5f * 35.0f;
            EditorGUI.BeginProperty(new Rect(rect.x, rect.y, rect.width, 2.0f * rect.height), label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            Color tmpCol;
            bool tmpBool;
            float tmp;
            int iTmp;
            Vector2 tmpVec2;
            if (EditorGUI.BeginFoldoutHeaderGroup(new Rect(position.x + 0.25f * m_Indent, position.y, position.width - 0.25f * m_Indent, GUIStyle.none.lineHeight), isFoldOpened.boolValue, EditorGUIUtility.TrTextContent("Lens Flare Element")))
            {
                rect = GetNextRect();
                EditorGUI.TextArea(rect, "Common", EditorStyles.boldLabel);
                ++EditorGUI.indentLevel;
                {
                    rect = GetNextRect();
                    if ((tmp = EditorGUI.FloatField(rect, Styles.intensity, intensityProp.floatValue)) != intensityProp.floatValue)
                        intensityProp.floatValue = Mathf.Max(tmp, 0.0f);
                    rect = GetNextRect();
                    if ((tmpCol = EditorGUI.ColorField(rect, Styles.tint, tintProp.colorValue)) != tintProp.colorValue)
                        tintProp.colorValue = tmpCol;
                    rect = GetNextRect();
                    SRPLensFlareBlendMode newBlendMode;
                    SRPLensFlareBlendMode blendModeValue = (UnityEngine.SRPLensFlareBlendMode)blendModeProp.enumValueIndex;
                    if ((newBlendMode = ((SRPLensFlareBlendMode)(EditorGUI.EnumPopup(rect, Styles.blendMode, blendModeValue)))) != blendModeValue)
                        blendModeProp.enumValueIndex = (int)newBlendMode;
                    rect = GetNextRect();
                    if ((tmpBool = EditorGUI.Toggle(rect, Styles.modulateByLightColor, modulateByLightColor.boolValue)) != modulateByLightColor.boolValue)
                        modulateByLightColor.boolValue = tmpBool;
                    rect = GetNextRect();
                    if ((tmp = EditorGUI.FloatField(rect, Styles.rotation, rotationProp.floatValue)) != rotationProp.floatValue)
                        rotationProp.floatValue = tmp;
                    rect = GetNextRect();
                    if ((tmp = EditorGUI.FloatField(rect, Styles.size, sizeProp.floatValue)) != sizeProp.floatValue)
                        sizeProp.floatValue = Mathf.Max(tmp, 1e-5f);
                    if (!preserveAspectRatioProp.boolValue)
                    {
                        rect = GetNextRect();
                        if ((tmp = EditorGUI.FloatField(rect, Styles.aspectRatio, aspectRatioProp.floatValue)) != aspectRatioProp.floatValue)
                            aspectRatioProp.floatValue = Mathf.Max(tmp, 1e-5f);
                    }
                    rect = GetNextRect();
                    if ((tmpBool = EditorGUI.Toggle(rect, Styles.autoRotate, autoRotateProp.boolValue)) != autoRotateProp.boolValue)
                        autoRotateProp.boolValue = tmpBool;
                }
                --EditorGUI.indentLevel;
                rect = GetNextRect();
                EditorGUI.TextArea(rect, "Axis Transforms", EditorStyles.boldLabel);
                ++EditorGUI.indentLevel;
                {
                    rect = GetNextRect();
                    if ((tmp = EditorGUI.FloatField(rect, Styles.position, positionProp.floatValue)) != positionProp.floatValue)
                        positionProp.floatValue = tmp;
                    rect = GetNextRect();
                    if ((tmpVec2 = EditorGUI.Vector2Field(rect, Styles.positionOffset, positionOffsetProp.vector2Value)) != positionOffsetProp.vector2Value)
                        positionOffsetProp.vector2Value = tmpVec2;
                    rect = GetNextRect();
                    if ((tmp = EditorGUI.FloatField(rect, Styles.angularOffset, angularOffsetProp.floatValue)) != angularOffsetProp.floatValue)
                        angularOffsetProp.floatValue = tmp;
                    rect = GetNextRect();
                    if ((tmpVec2 = EditorGUI.Vector2Field(rect, Styles.translationScale, translationScaleProp.vector2Value)) != translationScaleProp.vector2Value)
                        translationScaleProp.vector2Value = tmpVec2;
                }
                --EditorGUI.indentLevel;
                rect = GetNextRect();
                EditorGUI.TextArea(rect, "Type", EditorStyles.boldLabel);
                ++EditorGUI.indentLevel;
                {
                    Texture tmpTex;
                    rect = GetNextRect();
                    if ((tmpTex = (EditorGUI.ObjectField(rect, Styles.flareTexture, lensFlareProp.objectReferenceValue, typeof(Texture), false) as Texture)) != (lensFlareProp.objectReferenceValue as Texture))
                    {
                        lensFlareProp.objectReferenceValue = tmpTex;
                        aspectRatioProp.serializedObject.ApplyModifiedProperties();
                    }
                    rect = GetNextRect();
                    if ((tmpBool = EditorGUI.Toggle(rect, Styles.preserveAspectRatio, preserveAspectRatioProp.boolValue)) != preserveAspectRatioProp.boolValue)
                        preserveAspectRatioProp.boolValue = tmpBool;
                }
                --EditorGUI.indentLevel;
                rect = GetNextRect();
                EditorGUI.TextArea(rect, "Multiple Elements", EditorStyles.boldLabel);
                ++EditorGUI.indentLevel;
                {
                    rect = GetNextRect();
                    if ((iTmp = EditorGUI.IntField(rect, Styles.count, countProp.intValue)) != countProp.intValue)
                        countProp.intValue = Mathf.Max(iTmp, 1);

                    if (countProp.intValue > 1)
                    {
                        rect = GetNextRect();
                        SRPLensFlareDistribution newDistribution;
                        SRPLensFlareDistribution distributionValue = (UnityEngine.SRPLensFlareDistribution)distributionProp.enumValueIndex;
                        if ((newDistribution = ((SRPLensFlareDistribution)(EditorGUI.EnumPopup(rect, Styles.distribution, distributionValue)))) != distributionValue)
                            distributionProp.enumValueIndex = (int)newDistribution;

                        rect = GetNextRect();
                        if ((tmp = EditorGUI.FloatField(rect, Styles.lengthSpread, lengthSpreadProp.floatValue)) != lengthSpreadProp.floatValue)
                            lengthSpreadProp.floatValue = Mathf.Max(tmp, 1e-1f);

                        if (newDistribution == SRPLensFlareDistribution.Uniform)
                        {
                            rect = GetNextRect();
                            EditorGUI.PropertyField(rect, colorGradientProp, Styles.colors);
                        }
                        else if (newDistribution == SRPLensFlareDistribution.Random)
                        {
                            rect = GetNextRect();
                            if ((iTmp = EditorGUI.IntField(rect, Styles.seed, seedProp.intValue)) != seedProp.intValue)
                                seedProp.intValue = Mathf.Max(iTmp, 0);

                            rect = GetNextRect();
                            if ((tmp = EditorGUI.FloatField(rect, Styles.intensityVariation, intensityVariationProp.floatValue)) != intensityVariationProp.floatValue)
                                intensityVariationProp.floatValue = Mathf.Max(tmp, 0.0f);

                            rect = GetNextRect();
                            EditorGUI.PropertyField(rect, colorGradientProp, Styles.colors);

                            rect = GetNextRect();
                            if ((tmpVec2 = EditorGUI.Vector2Field(rect, Styles.positionVariation, positionVariationProp.vector2Value)) != positionVariationProp.vector2Value)
                                positionVariationProp.vector2Value = tmpVec2;

                            rect = GetNextRect();
                            if ((tmp = EditorGUI.FloatField(rect, Styles.scaleVariation, scaleVariationProp.floatValue)) != scaleVariationProp.floatValue)
                                scaleVariationProp.floatValue = Mathf.Max(tmp, 0.0f);

                            rect = GetNextRect();
                            if ((tmp = EditorGUI.FloatField(rect, Styles.rotationVariation, rotationVariationProp.floatValue)) != rotationVariationProp.floatValue)
                                rotationVariationProp.floatValue = Mathf.Max(tmp, 0.0f);
                        }
                        else if (newDistribution == SRPLensFlareDistribution.Curve)
                        {
                            rect = GetNextRect();
                            EditorGUI.PropertyField(rect, colorGradientProp, Styles.colors);
                            rect = GetNextRect();
                            EditorGUI.PropertyField(rect, positionCurveProp, Styles.positionCurve);
                            rect = GetNextRect();
                            EditorGUI.PropertyField(rect, scaleCurveProp, Styles.scaleCurve);
                        }
                    }
                }
                --EditorGUI.indentLevel;

                isFoldOpened.boolValue = true;
            }
            else
            {
                Texture tmpTex;
                rect = GetNextRect();
                if ((tmpTex = (EditorGUI.ObjectField(rect, Styles.flareTexture, lensFlareProp.objectReferenceValue, typeof(Texture), false) as Texture)) != (lensFlareProp.objectReferenceValue as Texture))
                {
                    lensFlareProp.objectReferenceValue = tmpTex;
                    aspectRatioProp.floatValue = ((float)tmpTex.width) / ((float)tmpTex.height);
                    aspectRatioProp.serializedObject.ApplyModifiedProperties();
                }

                rect = GetNextRect();
                if ((tmpCol = EditorGUI.ColorField(rect, Styles.tint, tintProp.colorValue)) != tintProp.colorValue)
                    tintProp.colorValue = tmpCol;

                rect = GetNextRect();
                if ((tmp = EditorGUI.FloatField(rect, Styles.intensity, intensityProp.floatValue)) != intensityProp.floatValue)
                    intensityProp.floatValue = Mathf.Max(tmp, 0.0f);

                rect = GetNextRect();
                if ((tmp = EditorGUI.FloatField(rect, Styles.position, positionProp.floatValue)) != positionProp.floatValue)
                    positionProp.floatValue = tmp;

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
            SerializedProperty preserveAspectRatio = property.FindPropertyRelative("preserveAspectRatio");
            SerializedProperty distributionProp = property.FindPropertyRelative("distribution");
            SerializedProperty countProp = property.FindPropertyRelative("count");

            float coef;
            if (isFoldOpened.boolValue)
            {
                if (preserveAspectRatio.boolValue)
                    coef = 19.0f;
                else
                    coef = 20.0f;

                if (countProp.intValue > 1)
                {
                    coef += 2.0f;
                    if ((SRPLensFlareDistribution)distributionProp.enumValueIndex == SRPLensFlareDistribution.Uniform)
                    {
                        coef += 1.0f;
                    }
                    else if ((SRPLensFlareDistribution)distributionProp.enumValueIndex == SRPLensFlareDistribution.Random)
                    {
                        coef += 6.0f;
                    }
                    else if ((SRPLensFlareDistribution)distributionProp.enumValueIndex == SRPLensFlareDistribution.Curve)
                    {
                        coef += 3.0f;
                    }
                }
            }
            else
            {
                coef = 5.0f;
            }

            return coef * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) + 1.5f * 35.0f;
        }

        sealed class Styles
        {
            static public readonly GUIContent intensity = EditorGUIUtility.TrTextContent("Intensity", "Intensity of this element.");
            static public readonly GUIContent position = EditorGUIUtility.TrTextContent("Starting Position", "Starting position.");
            static public readonly GUIContent positionOffset = EditorGUIUtility.TrTextContent("Position Offset", "Position Offset.");
            static public readonly GUIContent angularOffset = EditorGUIUtility.TrTextContent("Angular Offset", "Angular Offset.");
            static public readonly GUIContent translationScale = EditorGUIUtility.TrTextContent("Translation Scale", "Translation Scale.");
            static public readonly GUIContent flareTexture = EditorGUIUtility.TrTextContent("Flare Texture", "Texture used to for this Lens Flare Element.");
            static public readonly GUIContent tint = EditorGUIUtility.TrTextContent("Tint", "Tint of the texture can be modulated by the light it is attached to if Modulate By Light Color is enabled..");
            static public readonly GUIContent blendMode = EditorGUIUtility.TrTextContent("Blend Mode", "Blend mode used.");
            static public readonly GUIContent size = EditorGUIUtility.TrTextContent("Size", "Scale applied to the element.");
            static public readonly GUIContent aspectRatio = EditorGUIUtility.TrTextContent("Aspect Ratio", "Aspect ratio (width / height).");
            static public readonly GUIContent preserveAspectRatio = EditorGUIUtility.TrTextContent("Preserve Aspect Ratio", "Preserve Aspect ratio (width / height).");
            static public readonly GUIContent count = EditorGUIUtility.TrTextContent("Count", "REPLACE ME.");
            static public readonly GUIContent rotation = EditorGUIUtility.TrTextContent("Rotation", "Local rotation of the texture.");
            static public readonly GUIContent autoRotate = EditorGUIUtility.TrTextContent("Auto Rotate", "Rotate the texture relative to the angle on the screen (the rotation will be added to the parameter 'rotation').");
            static public readonly GUIContent modulateByLightColor = EditorGUIUtility.TrTextContent("Modulate By Light Color", "Modulate by light color if the asset is used on the same object as a light component..");

            static public readonly GUIContent distribution = EditorGUIUtility.TrTextContent("Distribution", "REPLACE ME.");
            static public readonly GUIContent lengthSpread = EditorGUIUtility.TrTextContent("Length Spread", "REPLACE ME.");
            static public readonly GUIContent seed = EditorGUIUtility.TrTextContent("Seed", "REPLACE ME.");

            static public readonly GUIContent intensityVariation = EditorGUIUtility.TrTextContent("Intensity Variation", "REPLACE ME.");
            static public readonly GUIContent positionVariation = EditorGUIUtility.TrTextContent("Position Variation", "REPLACE ME.");
            static public readonly GUIContent scaleVariation = EditorGUIUtility.TrTextContent("Scale Variation", "REPLACE ME.");
            static public readonly GUIContent rotationVariation = EditorGUIUtility.TrTextContent("Rotation Variation", "REPLACE ME.");
            static public readonly GUIContent colors = EditorGUIUtility.TrTextContent("Colors", "REPLACE ME.");
            static public readonly GUIContent positionCurve = EditorGUIUtility.TrTextContent("Position Spacing", "REPLACE ME.");
            static public readonly GUIContent scaleCurve = EditorGUIUtility.TrTextContent("Scale Variation", "REPLACE ME.");
        }
    }
}
