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
        private float m_LastOffset = 0.0f;
        private Rect m_CurrentRect;

        private void InitFirstRect(Rect position)
        {
            m_CurrentRect = new Rect(position.x, position.y, position.width, GUIStyle.none.lineHeight);
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
            InitFirstRect(position);

            SerializedProperty intensityProp = property.FindPropertyRelative("localIntensity");
            SerializedProperty positionProp = property.FindPropertyRelative("position");
            SerializedProperty positionOffsetProp = property.FindPropertyRelative("positionOffset");
            SerializedProperty angularOffsetProp = property.FindPropertyRelative("angularOffset");
            SerializedProperty translationScaleProp = property.FindPropertyRelative("translationScale");
            SerializedProperty lensFlareProp = property.FindPropertyRelative("lensFlareTexture");
            SerializedProperty tintProp = property.FindPropertyRelative("tint");
            SerializedProperty blendModeProp = property.FindPropertyRelative("blendMode");
            SerializedProperty countProp = property.FindPropertyRelative("count");
            SerializedProperty allowMultipleElementProp = property.FindPropertyRelative("allowMultipleElement");
            SerializedProperty rotationProp = property.FindPropertyRelative("rotation");
            SerializedProperty speedProp = property.FindPropertyRelative("speed");
            SerializedProperty autoRotateProp = property.FindPropertyRelative("autoRotate");
            SerializedProperty preserveAspectRatioProp = property.FindPropertyRelative("preserveAspectRatio");
            SerializedProperty modulateByLightColor = property.FindPropertyRelative("modulateByLightColor");
            SerializedProperty isFoldOpenedProp = property.FindPropertyRelative("isFoldOpened");
            SerializedProperty flareTypeProp = property.FindPropertyRelative("flareType");

            SerializedProperty uniformScaleProp = property.FindPropertyRelative("uniformScale");
            SerializedProperty sizeXYProp = property.FindPropertyRelative("sizeXY");

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

            // Distortion
            SerializedProperty enableDistortionProp = property.FindPropertyRelative("enableRadialDistortion");
            SerializedProperty targetSizeDistortionProp = property.FindPropertyRelative("targetSizeDistortion");
            SerializedProperty distortionCurveProp = property.FindPropertyRelative("distortionCurve");
            SerializedProperty distortionRelativeToCenterProp = property.FindPropertyRelative("distortionRelativeToCenter");

            if (lensFlareProp.objectReferenceValue != null)
            {
                Texture texture = lensFlareProp.objectReferenceValue as Texture;
                float localAspectRatio = sizeXYProp.vector2Value.x / Mathf.Max(sizeXYProp.vector2Value.y, 1e-6f);
                float imgWidth = 1.5f * 35.0f;
                float usedAspectRatio = preserveAspectRatioProp.boolValue ? (((float)texture.width) / ((float)texture.height)) : localAspectRatio;
                if (isFoldOpenedProp.boolValue)
                {
                    Rect imgRect = new Rect(m_CurrentRect.x + 0.5f * (position.width - imgWidth), m_CurrentRect.y + GUIStyle.none.lineHeight + 5.0f, imgWidth, imgWidth);
                    EditorGUI.DrawTextureTransparent(imgRect, lensFlareProp.objectReferenceValue as Texture, ScaleMode.ScaleToFit, usedAspectRatio);
                }
                else
                {
                    float imgOffY = 0.5f * (GetPropertyHeight(property, label) - imgWidth - GUIStyle.none.lineHeight);
                    Rect imgRect = new Rect(position.x - 35.0f + 15.0f, position.y + imgOffY + GUIStyle.none.lineHeight, imgWidth, imgWidth);
                    EditorGUI.DrawTextureTransparent(imgRect, lensFlareProp.objectReferenceValue as Texture, ScaleMode.ScaleToFit, usedAspectRatio);
                }
            }
            Rect rect = m_CurrentRect;
            if (isFoldOpenedProp.boolValue)
            {
                m_CurrentRect.y += 1.5f * 35.0f;
            }
            EditorGUI.BeginProperty(new Rect(rect.x, rect.y, rect.width, 2.0f * rect.height), label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            Color tmpCol;
            bool tmpBool;
            float tmp;
            int iTmp;
            Vector2 tmpVec2;
            if (EditorGUI.BeginFoldoutHeaderGroup(new Rect(position.x, position.y, position.width, GUIStyle.none.lineHeight), isFoldOpenedProp.boolValue, EditorGUIUtility.TrTextContent("Lens Flare Element")))
            {
                rect = GetNextRect();
                EditorGUI.TextArea(rect, "Type", EditorStyles.boldLabel);
                ++EditorGUI.indentLevel;
                {
                    rect = GetNextRect();
                    SRPLensFlareType newType;
                    SRPLensFlareType typeValue = (UnityEngine.SRPLensFlareType)flareTypeProp.enumValueIndex;
                    if ((newType = ((SRPLensFlareType)(EditorGUI.EnumPopup(rect, Styles.flareType, typeValue)))) != typeValue)
                        flareTypeProp.enumValueIndex = (int)newType;

                    if (newType == SRPLensFlareType.Image)
                    {
                        Texture tmpTex;
                        rect = GetNextRect();
                        if ((tmpTex = (EditorGUI.ObjectField(rect, Styles.flareTexture, lensFlareProp.objectReferenceValue, typeof(Texture), false) as Texture)) != (lensFlareProp.objectReferenceValue as Texture))
                        {
                            lensFlareProp.objectReferenceValue = tmpTex;
                            lensFlareProp.serializedObject.ApplyModifiedProperties();
                        }

                        rect = GetNextRect();
                        if ((tmpBool = EditorGUI.Toggle(rect, Styles.preserveAspectRatio, preserveAspectRatioProp.boolValue)) != preserveAspectRatioProp.boolValue)
                            preserveAspectRatioProp.boolValue = tmpBool;
                    }
                    else if (newType == SRPLensFlareType.Circle || newType == SRPLensFlareType.Polygon)
                    {
                        SerializedProperty fallOffProp = property.FindPropertyRelative("fallOff");
                        SerializedProperty edgeOffsetProp = property.FindPropertyRelative("edgeOffset");
                        SerializedProperty sdfRoundnessProp = property.FindPropertyRelative("sdfRoundness");
                        SerializedProperty sideCountProp = property.FindPropertyRelative("sideCount");
                        SerializedProperty inverseSDFProp = property.FindPropertyRelative("inverseSDF");

                        rect = GetNextRect();
                        if ((tmp = EditorGUI.Slider(rect, Styles.edgeOffset, edgeOffsetProp.floatValue, 0.0f, 1.0f)) != edgeOffsetProp.floatValue)
                            edgeOffsetProp.floatValue = Mathf.Clamp01(tmp);

                        rect = GetNextRect();
                        if ((tmp = EditorGUI.FloatField(rect, Styles.fallOff, fallOffProp.floatValue)) != fallOffProp.floatValue)
                            fallOffProp.floatValue = Mathf.Max(tmp, 0.0f);

                        if (newType == SRPLensFlareType.Polygon)
                        {
                            rect = GetNextRect();
                            if ((tmp = EditorGUI.IntSlider(rect, Styles.sideCount, sideCountProp.intValue, 3, 32)) != sideCountProp.intValue)
                                sideCountProp.intValue = (int)Mathf.Max(tmp, 0);

                            rect = GetNextRect();
                            if ((tmp = EditorGUI.Slider(rect, Styles.sdfRoundness, sdfRoundnessProp.floatValue, 0.0f, 1.0f)) != sdfRoundnessProp.floatValue)
                                sdfRoundnessProp.floatValue = Mathf.Clamp01(tmp);
                        }

                        rect = GetNextRect();
                        if ((tmpBool = EditorGUI.Toggle(rect, Styles.inverseSDF, inverseSDFProp.boolValue)) != inverseSDFProp.boolValue)
                            inverseSDFProp.boolValue = tmpBool;
                    }
                }
                --EditorGUI.indentLevel;

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

                    if (!preserveAspectRatioProp.boolValue)
                    {
                        rect = GetNextRect();
                        if ((tmpVec2 = EditorGUI.Vector2Field(rect, Styles.sizeXY, sizeXYProp.vector2Value)) != sizeXYProp.vector2Value)
                            sizeXYProp.vector2Value = new Vector2(Mathf.Max(tmpVec2.x, 1e-6f), Mathf.Max(tmpVec2.y, 1e-6f));
                    }

                    rect = GetNextRect();
                    if ((tmp = EditorGUI.FloatField(rect, Styles.uniformScale, uniformScaleProp.floatValue)) != uniformScaleProp.floatValue)
                        uniformScaleProp.floatValue = Mathf.Max(tmp, 0.0f);

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
                EditorGUI.TextArea(rect, "Radial Distortion", EditorStyles.boldLabel);
                ++EditorGUI.indentLevel;
                {
                    rect = GetNextRect();
                    if ((tmpBool = EditorGUI.Toggle(rect, Styles.enableDistortion, enableDistortionProp.boolValue)) != enableDistortionProp.boolValue)
                        enableDistortionProp.boolValue = tmpBool;
                    if (enableDistortionProp.boolValue == true)
                    {
                        rect = GetNextRect();
                        if ((tmpVec2 = EditorGUI.Vector2Field(rect, Styles.targetSizeDistortion, targetSizeDistortionProp.vector2Value)) != targetSizeDistortionProp.vector2Value)
                            targetSizeDistortionProp.vector2Value = tmpVec2;
                        rect = GetNextRect();
                        EditorGUI.PropertyField(rect, distortionCurveProp, Styles.distortionCurve);
                        rect = GetNextRect();
                        if ((tmpBool = EditorGUI.Toggle(rect, Styles.distortionRelativeToCenter, distortionRelativeToCenterProp.boolValue)) != distortionRelativeToCenterProp.boolValue)
                            distortionRelativeToCenterProp.boolValue = tmpBool;
                    }
                }
                --EditorGUI.indentLevel;

                rect = GetNextRect();
                EditorGUI.TextArea(rect, "Multiple Elements", EditorStyles.boldLabel);
                ++EditorGUI.indentLevel;
                {
                    rect = GetNextRect();
                    if ((tmpBool = EditorGUI.Toggle(rect, Styles.allowMultipleElement, allowMultipleElementProp.boolValue)) != allowMultipleElementProp.boolValue)
                        allowMultipleElementProp.boolValue = tmpBool;

                    if (allowMultipleElementProp.boolValue)
                    {
                        rect = GetNextRect();
                        if ((iTmp = EditorGUI.IntField(rect, Styles.count, countProp.intValue)) != countProp.intValue)
                            countProp.intValue = Mathf.Clamp(iTmp, 2, 4096); // 4096 is large enough for all imaginable use case (I hope)
                    }
                    if (allowMultipleElementProp.boolValue)
                    {
                        rect = GetNextRect();
                        SRPLensFlareDistribution newDistribution;
                        SRPLensFlareDistribution distributionValue = (UnityEngine.SRPLensFlareDistribution)distributionProp.enumValueIndex;
                        if ((newDistribution = ((SRPLensFlareDistribution)(EditorGUI.EnumPopup(rect, Styles.distribution, distributionValue)))) != distributionValue)
                            distributionProp.enumValueIndex = (int)newDistribution;

                        rect = GetNextRect();
                        if ((tmp = EditorGUI.FloatField(rect, Styles.lengthSpread, lengthSpreadProp.floatValue)) != lengthSpreadProp.floatValue)
                            lengthSpreadProp.floatValue = tmp;

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
                                intensityVariationProp.floatValue = Mathf.Clamp01(tmp);

                            rect = GetNextRect();
                            EditorGUI.PropertyField(rect, colorGradientProp, Styles.colors);

                            rect = GetNextRect();
                            if ((tmpVec2 = EditorGUI.Vector2Field(rect, Styles.positionVariation, positionVariationProp.vector2Value)) != positionVariationProp.vector2Value)
                                positionVariationProp.vector2Value = tmpVec2;

                            rect = GetNextRect();
                            if ((tmp = EditorGUI.FloatField(rect, Styles.rotationVariation, rotationVariationProp.floatValue)) != rotationVariationProp.floatValue)
                                rotationVariationProp.floatValue = Mathf.Max(tmp, 0.0f);

                            rect = GetNextRect();
                            if ((tmp = EditorGUI.FloatField(rect, Styles.scaleVariation, scaleVariationProp.floatValue)) != scaleVariationProp.floatValue)
                                scaleVariationProp.floatValue = Mathf.Max(tmp, 0.0f);
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

                isFoldOpenedProp.boolValue = true;
            }
            else
            {
                rect = GetNextRect(35.0f);
                SRPLensFlareType newType;
                SRPLensFlareType typeValue = (UnityEngine.SRPLensFlareType)flareTypeProp.enumValueIndex;
                if ((newType = ((SRPLensFlareType)(EditorGUI.EnumPopup(rect, Styles.flareType, typeValue)))) != typeValue)
                    flareTypeProp.enumValueIndex = (int)newType;

                rect = GetNextRect();
                if ((tmp = EditorGUI.FloatField(rect, Styles.intensity, intensityProp.floatValue)) != intensityProp.floatValue)
                    intensityProp.floatValue = Mathf.Max(tmp, 0.0f);

                rect = GetNextRect();
                if ((tmpCol = EditorGUI.ColorField(rect, Styles.tint, tintProp.colorValue)) != tintProp.colorValue)
                    tintProp.colorValue = tmpCol;
                if (allowMultipleElementProp.boolValue)
                {
                    rect = GetNextRect();
                    if ((iTmp = EditorGUI.IntField(rect, Styles.count, countProp.intValue)) != countProp.intValue)
                        countProp.intValue = Mathf.Max(iTmp, 1);
                }

                isFoldOpenedProp.boolValue = false;
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
            SerializedProperty flareTypeProp = property.FindPropertyRelative("flareType");
            SerializedProperty enableDistortionProp = property.FindPropertyRelative("enableRadialDistortion");
            SerializedProperty allowMultipleElementProp = property.FindPropertyRelative("allowMultipleElement");

            SRPLensFlareType flareType = (SRPLensFlareType)flareTypeProp.enumValueIndex;

            float coef;
            float offset = 0.0f;
            if (isFoldOpened.boolValue)
            {
                if (preserveAspectRatio.boolValue)
                    coef = 25.0f;
                else
                    coef = 26.0f;

                if (flareType == SRPLensFlareType.Polygon || flareType == SRPLensFlareType.Circle)
                {
                    coef += 1.0f;

                    if (flareType == SRPLensFlareType.Polygon)
                        coef += 2.0f;
                }

                if (enableDistortionProp.boolValue == false)
                {
                    coef -= 3.0f;
                }

                if (allowMultipleElementProp.boolValue)
                {
                    coef += 3.0f;
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

                offset = 1.5f * 35.0f;
            }
            else
            {
                coef = 5.0f;
                if (!allowMultipleElementProp.boolValue)
                {
                    coef -= 1.0f;
                }
            }

            return coef * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) + offset;
        }

        sealed class Styles
        {
            static public readonly GUIContent intensity = EditorGUIUtility.TrTextContent("Intensity", "Intensity of this element.");
            static public readonly GUIContent position = EditorGUIUtility.TrTextContent("Starting Position", "Starting position.");
            static public readonly GUIContent positionOffset = EditorGUIUtility.TrTextContent("Position Offset", "Position Offset.");
            static public readonly GUIContent angularOffset = EditorGUIUtility.TrTextContent("Angular Offset", "Angular Offset.");
            static public readonly GUIContent translationScale = EditorGUIUtility.TrTextContent("Translation Scale", "This parameter is usefull to lock the lens flare axis to horizontal or vertical.");
            static public readonly GUIContent flareTexture = EditorGUIUtility.TrTextContent("Flare Texture", "Texture used to for this Lens Flare Element.");
            static public readonly GUIContent tint = EditorGUIUtility.TrTextContent("Tint", "Tint of the texture can be modulated by the light it is attached to if Modulate By Light Color is enabled..");
            static public readonly GUIContent blendMode = EditorGUIUtility.TrTextContent("Blend Mode", "Blend mode used.");
            static public readonly GUIContent preserveAspectRatio = EditorGUIUtility.TrTextContent("Preserve Aspect Ratio", "Preserve Aspect ratio (width / height).");

            static public readonly GUIContent uniformScale = EditorGUIUtility.TrTextContent("Scale", "Uniform scale used to size the flare on width and height.");
            static public readonly GUIContent sizeXY = EditorGUIUtility.TrTextContent("Size", "Size for each dimension. Can be used with Radial Distortion.");

            static public readonly GUIContent allowMultipleElement = EditorGUIUtility.TrTextContent("Enable", "Allow MultipleElements.");
            static public readonly GUIContent count = EditorGUIUtility.TrTextContent("Count", "Number of Elements.");
            static public readonly GUIContent rotation = EditorGUIUtility.TrTextContent("Rotation", "Local rotation of the texture.");
            static public readonly GUIContent autoRotate = EditorGUIUtility.TrTextContent("Auto Rotate", "Rotate the texture relative to the angle on the screen (the rotation will be added to the parameter 'rotation').");
            static public readonly GUIContent modulateByLightColor = EditorGUIUtility.TrTextContent("Modulate By Light Color", "Modulate by light color if the asset is used on the same object as a light component.");
            static public readonly GUIContent flareType = EditorGUIUtility.TrTextContent("Type", "Type of Flare.");

            static public readonly GUIContent distribution = EditorGUIUtility.TrTextContent("Distribution", "Method of distribution for multiple elements.");
            static public readonly GUIContent lengthSpread = EditorGUIUtility.TrTextContent("Length Spread", "Length to spread the distribution of flares, spread start at 'starting position'.");
            static public readonly GUIContent seed = EditorGUIUtility.TrTextContent("Seed", "Value used to define randomness.");

            static public readonly GUIContent intensityVariation = EditorGUIUtility.TrTextContent("Intensity Variation", "Scale factor applied on the variation of the intensities.");
            static public readonly GUIContent positionVariation = EditorGUIUtility.TrTextContent("Position Variation", "Scale factor applied on the variation of the positions.");
            static public readonly GUIContent scaleVariation = EditorGUIUtility.TrTextContent("Scale Variation", "Coefficient applied on the variation of the scale (relative to the current scale).");
            static public readonly GUIContent rotationVariation = EditorGUIUtility.TrTextContent("Rotation Variation", "Scale factor applied on the variation of the rotation (relative to the current rotation or auto-rotate).");
            static public readonly GUIContent colors = EditorGUIUtility.TrTextContent("Color Gradient", "Colors sampled uniformly for Uniform or Curve Distribution and Random when the distribution is 'Random'.");
            static public readonly GUIContent positionCurve = EditorGUIUtility.TrTextContent("Position Variation", "Curve describing how to place flares distribution.");
            static public readonly GUIContent scaleCurve = EditorGUIUtility.TrTextContent("Scale", "Curve describing how to scale flares distribution.");

            // For Distortion
            static public readonly GUIContent enableDistortion = EditorGUIUtility.TrTextContent("Enable", "Radial distortion changes the size of the lens flare element(s) as they move around the screen.");
            static public readonly GUIContent targetSizeDistortion = EditorGUIUtility.TrTextContent("Radial Edge Size", "Target size used on the edge of the screen.");
            static public readonly GUIContent distortionCurve = EditorGUIUtility.TrTextContent("Radial Edge Curve", "Curve blending from screen center to the edges of the screen.");
            static public readonly GUIContent distortionRelativeToCenter = EditorGUIUtility.TrTextContent("Relative To Center", "Use the distance from the centre of the screen instead of the distance along axis to calculate distortion.");

            // For Procedural
            static public readonly GUIContent fallOff = EditorGUIUtility.TrTextContent("Falloff", "Fall of the gradient used for the Procedural Flare.");
            static public readonly GUIContent edgeOffset = EditorGUIUtility.TrTextContent("Gradient", "Gradient Offset used for the Procedural Flare.");
            static public readonly GUIContent sdfRoundness = EditorGUIUtility.TrTextContent("Roundness", "Roundness of the polygon flare (0: Sharp Polygon, 1: Circle).");
            static public readonly GUIContent sideCount = EditorGUIUtility.TrTextContent("Side Count", "Side count of the regular polygon generated.");
            static public readonly GUIContent inverseSDF = EditorGUIUtility.TrTextContent("Inverse", "Inverse the gradient direction.");
        }
    }
}
