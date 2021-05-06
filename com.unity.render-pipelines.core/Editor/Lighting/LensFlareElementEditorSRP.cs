using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// LensFlareElementEditorSRP shows how each element in the SRP Lens Flare Asset are show in the UI
    /// </summary>
    [CustomPropertyDrawer(typeof(LensFlareDataElementSRP))]
    internal class LensFlareElementEditorSRP : PropertyDrawer
    {
        float m_LastOffset = 0.0f;
        Rect m_CurrentRect;

        void InitFirstRect(Rect position)
        {
            m_CurrentRect = new Rect(position.x, position.y, position.width, GUIStyle.none.lineHeight);
        }

        Rect GetNextRect(float xOffset = 0.0f)
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

            SRPLensFlareType flareType = (UnityEngine.SRPLensFlareType)flareTypeProp.enumValueIndex;
            Texture texture = lensFlareProp.objectReferenceValue ? lensFlareProp.objectReferenceValue as Texture : null;
            float localAspectRatio = sizeXYProp.vector2Value.x / Mathf.Max(sizeXYProp.vector2Value.y, 1e-6f);
            float imgWidth = 1.5f * 35.0f;
            float usedAspectRatio;
            if (flareType == SRPLensFlareType.Image)
                usedAspectRatio = (lensFlareProp.objectReferenceValue && preserveAspectRatioProp.boolValue) ? (((float)texture.width) / ((float)texture.height)) : localAspectRatio;
            else
                usedAspectRatio = 1.0f;
            if (isFoldOpenedProp.boolValue)
            {
                Rect imgRect = new Rect(m_CurrentRect.x + 0.5f * (position.width - imgWidth), m_CurrentRect.y + GUIStyle.none.lineHeight + 5.0f, imgWidth, imgWidth);
                if (flareType == SRPLensFlareType.Image)
                {
                    EditorGUI.DrawTextureTransparent(imgRect, lensFlareProp.objectReferenceValue as Texture, ScaleMode.ScaleToFit, usedAspectRatio);
                    m_CurrentRect.y += 1.5f * 35.0f;
                }
            }
            else
            {
                float imgOffY = 0.5f * (GetPropertyHeight(property, label) - imgWidth - GUIStyle.none.lineHeight);
                Rect imgRect = new Rect(position.x - 35.0f + 15.0f, position.y + imgOffY + GUIStyle.none.lineHeight, imgWidth, imgWidth);
                if (flareType == SRPLensFlareType.Image)
                    EditorGUI.DrawTextureTransparent(imgRect, lensFlareProp.objectReferenceValue as Texture, ScaleMode.ScaleToFit, usedAspectRatio);
                else if (flareType == SRPLensFlareType.Circle)
                    EditorGUI.DrawTextureTransparent(imgRect, Styles.circleIcon.image, ScaleMode.ScaleToFit, usedAspectRatio);
                else //if (flareType != SRPLensFlareType.Polygon)
                    EditorGUI.DrawTextureTransparent(imgRect, Styles.polygonIcon.image, ScaleMode.ScaleToFit, usedAspectRatio);
            }
            Rect rect = m_CurrentRect;
            EditorGUI.BeginProperty(new Rect(rect.x, rect.y, rect.width, 2.0f * rect.height), label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            Color tmpCol;
            bool tmpBool;
            float tmp;
            int iTmp;
            Vector2 tmpVec2;
            Rect localRect = new Rect(position.x, position.y, position.width, GUIStyle.none.lineHeight);
            if (EditorGUI.BeginFoldoutHeaderGroup(localRect, isFoldOpenedProp.boolValue, Styles.lensFlareElement))
            {
                rect = GetNextRect();
                EditorGUI.TextArea(rect, Styles.typeElement.text, style: EditorStyles.boldLabel);
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
                        if ((tmp = EditorGUI.Slider(rect, Styles.fallOff, fallOffProp.floatValue, 0.0f, 1.0f)) != fallOffProp.floatValue)
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

                rect = GetNextRect();
                EditorGUI.TextArea(rect, Styles.colorElement.text, EditorStyles.boldLabel);
                {
                    rect = GetNextRect();
                    if ((tmpCol = EditorGUI.ColorField(rect, Styles.tint, tintProp.colorValue)) != tintProp.colorValue)
                        tintProp.colorValue = tmpCol;
                    rect = GetNextRect();
                    if ((tmpBool = EditorGUI.Toggle(rect, Styles.modulateByLightColor, modulateByLightColor.boolValue)) != modulateByLightColor.boolValue)
                        modulateByLightColor.boolValue = tmpBool;
                    rect = GetNextRect();
                    if ((tmp = EditorGUI.FloatField(rect, Styles.intensity, intensityProp.floatValue)) != intensityProp.floatValue)
                        intensityProp.floatValue = Mathf.Max(tmp, 0.0f);
                    rect = GetNextRect();
                    SRPLensFlareBlendMode newBlendMode;
                    SRPLensFlareBlendMode blendModeValue = (UnityEngine.SRPLensFlareBlendMode)blendModeProp.enumValueIndex;
                    if ((newBlendMode = ((SRPLensFlareBlendMode)(EditorGUI.EnumPopup(rect, Styles.blendMode, blendModeValue)))) != blendModeValue)
                        blendModeProp.enumValueIndex = (int)newBlendMode;
                }

                rect = GetNextRect();
                EditorGUI.TextArea(rect, Styles.transformElement.text, EditorStyles.boldLabel);
                {
                    rect = GetNextRect();
                    if ((tmpVec2 = EditorGUI.Vector2Field(rect, Styles.positionOffset, positionOffsetProp.vector2Value)) != positionOffsetProp.vector2Value)
                        positionOffsetProp.vector2Value = tmpVec2;
                    rect = GetNextRect();
                    if ((tmpBool = EditorGUI.Toggle(rect, Styles.autoRotate, autoRotateProp.boolValue)) != autoRotateProp.boolValue)
                        autoRotateProp.boolValue = tmpBool;
                    rect = GetNextRect();
                    if ((tmp = EditorGUI.FloatField(rect, Styles.rotation, rotationProp.floatValue)) != rotationProp.floatValue)
                        rotationProp.floatValue = tmp;
                    rect = GetNextRect();
                    if ((tmpVec2 = EditorGUI.Vector2Field(rect, Styles.sizeXY, sizeXYProp.vector2Value)) != sizeXYProp.vector2Value)
                        sizeXYProp.vector2Value = new Vector2(Mathf.Max(tmpVec2.x, 1e-6f), Mathf.Max(tmpVec2.y, 1e-6f));
                    rect = GetNextRect();
                    if ((tmp = EditorGUI.FloatField(rect, Styles.uniformScale, uniformScaleProp.floatValue)) != uniformScaleProp.floatValue)
                        uniformScaleProp.floatValue = Mathf.Max(tmp, 0.0f);
                }

                rect = GetNextRect();
                EditorGUI.TextArea(rect, Styles.axisTransformElement.text, EditorStyles.boldLabel);
                {
                    rect = GetNextRect();
                    if ((tmp = EditorGUI.FloatField(rect, Styles.position, positionProp.floatValue)) != positionProp.floatValue)
                        positionProp.floatValue = tmp;
                    rect = GetNextRect();
                    if ((tmp = EditorGUI.FloatField(rect, Styles.angularOffset, angularOffsetProp.floatValue)) != angularOffsetProp.floatValue)
                        angularOffsetProp.floatValue = tmp;
                    rect = GetNextRect();
                    if ((tmpVec2 = EditorGUI.Vector2Field(rect, Styles.translationScale, translationScaleProp.vector2Value)) != translationScaleProp.vector2Value)
                        translationScaleProp.vector2Value = tmpVec2;
                }

                rect = GetNextRect();
                EditorGUI.TextArea(rect, Styles.radialDistortionElement.text, EditorStyles.boldLabel);
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

                rect = GetNextRect();
                EditorGUI.TextArea(rect, Styles.multipleElementsElement.text, EditorStyles.boldLabel);
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
                if ((tmpCol = EditorGUI.ColorField(rect, Styles.tint, tintProp.colorValue)) != tintProp.colorValue)
                    tintProp.colorValue = tmpCol;

                rect = GetNextRect();
                if ((tmp = EditorGUI.FloatField(rect, Styles.intensity, intensityProp.floatValue)) != intensityProp.floatValue)
                    intensityProp.floatValue = Mathf.Max(tmp, 0.0f);


                if (allowMultipleElementProp.boolValue)
                {
                    rect = GetNextRect();
                    if ((iTmp = EditorGUI.IntField(rect, Styles.count, countProp.intValue)) != countProp.intValue)
                        countProp.intValue = Mathf.Max(iTmp, 2);
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
            SerializedProperty distributionProp = property.FindPropertyRelative("distribution");
            SerializedProperty flareTypeProp = property.FindPropertyRelative("flareType");
            SerializedProperty enableDistortionProp = property.FindPropertyRelative("enableRadialDistortion");
            SerializedProperty allowMultipleElementProp = property.FindPropertyRelative("allowMultipleElement");

            SRPLensFlareType flareType = (SRPLensFlareType)flareTypeProp.enumValueIndex;

            float coef;
            float offset = 0.0f;
            if (isFoldOpened.boolValue)
            {
                if (flareType == SRPLensFlareType.Polygon || flareType == SRPLensFlareType.Circle)
                    coef = 26.0f;
                else
                    coef = 27.0f;

                if (flareType == SRPLensFlareType.Polygon || flareType == SRPLensFlareType.Circle)
                {
                    coef -= 0.5f;

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

        static class Styles
        {
            static public string k_IconFolder = @"Packages/com.unity.render-pipelines.core/Editor/Resources/";
            static public GUIContent circleIcon = EditorGUIUtility.TrIconContent(UnityEditor.Rendering.CoreEditorUtils.LoadIcon(Styles.k_IconFolder, "CircleFlareThumbnail", ".png", false));
            static public GUIContent polygonIcon = EditorGUIUtility.TrIconContent(UnityEditor.Rendering.CoreEditorUtils.LoadIcon(Styles.k_IconFolder, "PolygonFlareThumbnail", ".png", false));

            static public readonly GUIContent lensFlareElement = EditorGUIUtility.TrTextContent("Lens Flare Element");
            static public readonly GUIContent typeElement = EditorGUIUtility.TrTextContent("Type");
            static public readonly GUIContent colorElement = EditorGUIUtility.TrTextContent("Color");
            static public readonly GUIContent transformElement = EditorGUIUtility.TrTextContent("Transform");
            static public readonly GUIContent axisTransformElement = EditorGUIUtility.TrTextContent("Axis Transform");
            static public readonly GUIContent radialDistortionElement = EditorGUIUtility.TrTextContent("Radial Distortion");
            static public readonly GUIContent multipleElementsElement = EditorGUIUtility.TrTextContent("Multiple Elements");

            static public readonly GUIContent intensity = EditorGUIUtility.TrTextContent("Intensity", "Sets the intensity of the element.");
            static public readonly GUIContent position = EditorGUIUtility.TrTextContent("Starting Position", "Sets the starting position of this element in screen space relative to its source.");
            static public readonly GUIContent positionOffset = EditorGUIUtility.TrTextContent("Position Offset", "Sets the offset of this element in screen space relative to its source.");
            static public readonly GUIContent angularOffset = EditorGUIUtility.TrTextContent("Angular Offset", "Sets the angular offset of this element in degrees relative to its current position.");
            static public readonly GUIContent translationScale = EditorGUIUtility.TrTextContent("Translation Scale", "Controls the direction and speed the element appears to move. For example, values of (1,0) make the lens flare move horizontally.");
            static public readonly GUIContent flareTexture = EditorGUIUtility.TrTextContent("Flare Texture", "Specifies the Texture this element uses.");
            static public readonly GUIContent tint = EditorGUIUtility.TrTextContent("Tint", "Specifies the tint of the element. If the element type is set to Image, the Flare Texture is multiplied by this color.");
            static public readonly GUIContent blendMode = EditorGUIUtility.TrTextContent("Blend Mode", "Specifies the blend mode this element uses.");
            static public readonly GUIContent preserveAspectRatio = EditorGUIUtility.TrTextContent("Use Aspect Ratio", "When enabled, uses original aspect ratio of the width and height of the element's Flare Texture (or 1 for shape).");

            static public readonly GUIContent uniformScale = EditorGUIUtility.TrTextContent("Uniform Scale", "Sets the scale of this element.");
            static public readonly GUIContent sizeXY = EditorGUIUtility.TrTextContent("Scale", "Sets the stretch of each dimension in relative to the scale. You can use this with Radial Distortion.");

            static public readonly GUIContent allowMultipleElement = EditorGUIUtility.TrTextContent("Enable", "When enabled, allows multiple lens flare elements.");
            static public readonly GUIContent count = EditorGUIUtility.TrTextContent("Count", "Sets the number of elements.");
            static public readonly GUIContent rotation = EditorGUIUtility.TrTextContent("Rotation", "Sets the local rotation of the elements.");
            static public readonly GUIContent autoRotate = EditorGUIUtility.TrTextContent("Auto Rotate", "When enabled, automatically rotates the element between its position and the center of the screen. Requires the Starting Position property to have a value greater than 0.");
            static public readonly GUIContent modulateByLightColor = EditorGUIUtility.TrTextContent("Modulate By Light Color", "When enabled,changes the color of the elements based on the light color, if this asset is attached to a light.");
            static public readonly GUIContent flareType = EditorGUIUtility.TrTextContent("Type", "Specifies the type of this lens flare element.");

            static public readonly GUIContent distribution = EditorGUIUtility.TrTextContent("Distribution", "Controls how multiple lens flare elements are distributed.");
            static public readonly GUIContent lengthSpread = EditorGUIUtility.TrTextContent("Length Spread", "Sets the length lens flare elements are spread across in screen space.");
            static public readonly GUIContent seed = EditorGUIUtility.TrTextContent("Seed", "Sets the seed value used to define randomness.");

            static public readonly GUIContent intensityVariation = EditorGUIUtility.TrTextContent("Intensity Variation", "Controls the offset of the intensities. A value of 0 means no variations, a value of 1 means variations between 0 and 1.");
            static public readonly GUIContent positionVariation = EditorGUIUtility.TrTextContent("Position Variation", "Sets the offset applied to the current position of the element.");
            static public readonly GUIContent scaleVariation = EditorGUIUtility.TrTextContent("Scale Variation", "Sets the offset applied to the current scale of the element.");
            static public readonly GUIContent rotationVariation = EditorGUIUtility.TrTextContent("Rotation Variation", "Sets the offset applied to the current element rotation.");
            static public readonly GUIContent colors = EditorGUIUtility.TrTextContent("Color Gradient", "Specifies the gradient applied across all the elements.");
            static public readonly GUIContent positionCurve = EditorGUIUtility.TrTextContent("Position Variation", "Defines how the multiple elements are placed along the spread using a curve.");
            static public readonly GUIContent scaleCurve = EditorGUIUtility.TrTextContent("Scale", "Defines how the multiple elements are scaled along the spread.");

            // For Distortion
            static public readonly GUIContent enableDistortion = EditorGUIUtility.TrTextContent("Enable", "When enabled, distorts the element relative to its distance from the flare position in screen space.");
            static public readonly GUIContent targetSizeDistortion = EditorGUIUtility.TrTextContent("Radial Edge Size", "Sets the target size of the edge of the screen. Values of (1, 1) match the actual screen size.");
            static public readonly GUIContent distortionCurve = EditorGUIUtility.TrTextContent("Radial Edge Curve", "Controls the amount of distortion between the position of the lens flare and the edge of the screen.");
            static public readonly GUIContent distortionRelativeToCenter = EditorGUIUtility.TrTextContent("Relative To Center", "When enabled, the amount of radial distortion changes between the center of the screen and the edge of the screen.");

            // For Procedural
            static public readonly GUIContent fallOff = EditorGUIUtility.TrTextContent("Falloff", "Controls the smoothness of the gradient. A higher value creates a sharper gradient.");
            static public readonly GUIContent edgeOffset = EditorGUIUtility.TrTextContent("Gradient", "Controls the offset of the Procedural Flare gradient relative to its starting point. A higher value means the gradient starts further from the center of the shape.");
            static public readonly GUIContent sdfRoundness = EditorGUIUtility.TrTextContent("Roundness", "Specifies the roundness of the polygon flare. A value of 0 creates a sharp polygon, a value of 1 creates a circle.");
            static public readonly GUIContent sideCount = EditorGUIUtility.TrTextContent("Side Count", "Specifies the number of sides of the lens flare polygon.");
            static public readonly GUIContent inverseSDF = EditorGUIUtility.TrTextContent("Invert", "When enabled, will invert the gradient direction.");
        }
    }
}
