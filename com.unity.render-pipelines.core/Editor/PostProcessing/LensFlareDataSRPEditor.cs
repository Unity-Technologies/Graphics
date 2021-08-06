using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    [CustomEditorForRenderPipeline(typeof(LensFlareDataSRP), typeof(RenderPipelineAsset))]
    class LensFlareDataSRPEditor : Editor
    {
        static class Styles
        {
            public const int sizeWidth = 47;
            public const int sizeOffset = 5;
            public static readonly int headerHeight = (int)EditorGUIUtility.singleLineHeight;
            public static readonly int cathegorySpacing = 5;
            public const int footerSeparatorHeight = 5;
            public const int thumbnailSize = 52;
            public const int iconMargin = 6;    //margin for icon be ing at 75% of 52 thumbnail size
            public const int horiwontalSpaceBetweenThumbnailAndInspector = 5;
            public const int shrinkingLabel = 10;

            public static readonly Color elementBackgroundColor = EditorGUIUtility.isProSkin
                ? new Color32(65, 65, 65, 255)
                : new Color32(200, 200, 200, 255);

            public static readonly GUIContent mainHeader = EditorGUIUtility.TrTextContent("Elements", "List of elements in the Lens Flare.");
            public static readonly GUIContent elementHeader = EditorGUIUtility.TrTextContent("Lens Flare Element", "Elements in the Lens Flare.");

            // Cathegory headers
            static public readonly string typeCathegory = L10n.Tr("Type");
            static public readonly string colorCathegory = L10n.Tr("Color");
            static public readonly string transformCathegory = L10n.Tr("Transform");
            static public readonly string axisTransformCathegory = L10n.Tr("Axis Transform");
            static public readonly string radialDistortionCathegory = L10n.Tr("Radial Distortion");
            static public readonly string multipleElementsCathegory = L10n.Tr("Multiple Elements");

            // Type
            static public readonly GUIContent type = EditorGUIUtility.TrTextContent("Type", "Specifies the type of this lens flare element.");
            static public readonly GUIContent flareTexture = EditorGUIUtility.TrTextContent("Flare Texture", "Specifies the Texture this element uses.");
            static public readonly GUIContent preserveAspectRatio = EditorGUIUtility.TrTextContent("Use Aspect Ratio", "When enabled, uses original aspect ratio of the width and height of the element's Flare Texture (or 1 for shape).");
            static public readonly GUIContent gradient = EditorGUIUtility.TrTextContent("Gradient", "Controls the offset of the Procedural Flare gradient relative to its starting point. A higher value means the gradient starts further from the center of the shape.");
            static public readonly GUIContent fallOff = EditorGUIUtility.TrTextContent("Falloff", "Controls the smoothness of the gradient. A higher value creates a sharper gradient.");
            static public readonly GUIContent sideCount = EditorGUIUtility.TrTextContent("Side Count", "Specifies the number of sides of the lens flare polygon.");
            static public readonly GUIContent sdfRoundness = EditorGUIUtility.TrTextContent("Roundness", "Specifies the roundness of the polygon flare. A value of 0 creates a sharp polygon, a value of 1 creates a circle.");
            static public readonly GUIContent inverseSDF = EditorGUIUtility.TrTextContent("Invert", "When enabled, will invert the gradient direction.");

            // Color
            static public readonly GUIContent tint = EditorGUIUtility.TrTextContent("Tint", "Specifies the tint of the element. If the element type is set to Image, the Flare Texture is multiplied by this color.");
            static public readonly GUIContent modulateByLightColor = EditorGUIUtility.TrTextContent("Modulate By Light Color", "When enabled,changes the color of the elements based on the light color, if this asset is attached to a light.");
            static public readonly GUIContent intensity = EditorGUIUtility.TrTextContent("Intensity", "Sets the intensity of the element.");
            static public readonly GUIContent blendMode = EditorGUIUtility.TrTextContent("Blend Mode", "Specifies the blend mode this element uses.");

            // Transform
            static public readonly GUIContent positionOffset = EditorGUIUtility.TrTextContent("Position Offset", "Sets the offset of this element in screen space relative to its source.");
            static public readonly GUIContent autoRotate = EditorGUIUtility.TrTextContent("Auto Rotate", "When enabled, automatically rotates the element between its position and the center of the screen. Requires the Starting Position property to have a value greater than 0.");
            static public readonly GUIContent rotation = EditorGUIUtility.TrTextContent("Rotation", "Sets the local rotation of the elements.");
            static public readonly GUIContent sizeXY = EditorGUIUtility.TrTextContent("Scale", "Sets the stretch of each dimension in relative to the scale. You can use this with Radial Distortion.");
            static public readonly GUIContent uniformScale = EditorGUIUtility.TrTextContent("Uniform Scale", "Sets the scale of this element.");

            // Axis Transform
            static public readonly GUIContent position = EditorGUIUtility.TrTextContent("Starting Position", "Sets the starting position of this element in screen space relative to its source.");
            static public readonly GUIContent angularOffset = EditorGUIUtility.TrTextContent("Angular Offset", "Sets the angular offset of this element in degrees relative to its current position.");
            static public readonly GUIContent translationScale = EditorGUIUtility.TrTextContent("Translation Scale", "Controls the direction and speed the element appears to move. For example, values of (1,0) make the lens flare move horizontally.");

            // Radial Distortion
            static public readonly GUIContent enableDistortion = EditorGUIUtility.TrTextContent("Enable", "When enabled, distorts the element relative to its distance from the flare position in screen space.");
            static public readonly GUIContent targetSizeDistortion = EditorGUIUtility.TrTextContent("Radial Edge Size", "Sets the target size of the edge of the screen. Values of (1, 1) match the actual screen size.");
            static public readonly GUIContent distortionCurve = EditorGUIUtility.TrTextContent("Radial Edge Curve", "Controls the amount of distortion between the position of the lens flare and the edge of the screen.");
            static public readonly GUIContent distortionRelativeToCenter = EditorGUIUtility.TrTextContent("Relative To Center", "When enabled, the amount of radial distortion changes between the center of the screen and the edge of the screen.");

            // Multiple Elements
            static public readonly GUIContent allowMultipleElement = EditorGUIUtility.TrTextContent("Enable", "When enabled, allows multiple lens flare elements.");
            static public readonly GUIContent count = EditorGUIUtility.TrTextContent("Count", "Sets the number of elements.");
            static public readonly GUIContent distribution = EditorGUIUtility.TrTextContent("Distribution", "Controls how multiple lens flare elements are distributed.");
            static public readonly GUIContent lengthSpread = EditorGUIUtility.TrTextContent("Length Spread", "Sets the length lens flare elements are spread across in screen space.");
            static public readonly GUIContent seed = EditorGUIUtility.TrTextContent("Seed", "Sets the seed value used to define randomness.");
            static public readonly GUIContent intensityVariation = EditorGUIUtility.TrTextContent("Intensity Variation", "Controls the offset of the intensities. A value of 0 means no variations, a value of 1 means variations between 0 and 1.");
            static public readonly GUIContent colorGradient = EditorGUIUtility.TrTextContent("Colors", "Specifies the gradient applied across all the elements.");
            static public readonly GUIContent positionVariation = EditorGUIUtility.TrTextContent("Position Variation", "Sets the offset applied to the current position of the element.");
            static public readonly GUIContent rotationVariation = EditorGUIUtility.TrTextContent("Rotation Variation", "Sets the offset applied to the current element rotation.");
            static public readonly GUIContent scaleVariation = EditorGUIUtility.TrTextContent("Scale Variation", "Sets the offset applied to the current scale of the element.");
            static public readonly GUIContent positionCurve = EditorGUIUtility.TrTextContent("Position Variation", "Defines how the multiple elements are placed along the spread using a curve.");
            static public readonly GUIContent scaleCurve = EditorGUIUtility.TrTextContent("Scale", "Defines how the multiple elements are scaled along the spread.");
            static public readonly GUIContent uniformAngleCurve = EditorGUIUtility.TrTextContent("Rotation", "The uniform angle of rotation (in degrees) applied to each element distributed along the curve.");
            static public readonly GUIContent uniformAngle = EditorGUIUtility.TrTextContent("Rotation", "The angle of rotation (in degrees) applied to each element incrementally.");

            static GUIStyle m_BlueFocusedBoldLabel;
            public static GUIStyle blueFocusedBoldLabel
            {
                get
                {
                    if (m_BlueFocusedBoldLabel == null)
                    {
                        m_BlueFocusedBoldLabel = new GUIStyle(EditorStyles.boldLabel);
                        //Note: GUI.skin.settings.selectionColor don't have the right color. Use the one from foldoutHeader instead
                        m_BlueFocusedBoldLabel.focused.textColor = EditorStyles.foldoutHeader.focused.textColor;
                    }
                    return m_BlueFocusedBoldLabel;
                }
            }
        }

        #region Reflection
        static Func<SerializedProperty, GenericMenu> FillPropertyContextMenu;

        static LensFlareDataSRPEditor()
        {
            MethodInfo FillPropertyContextMenuInfo = typeof(EditorGUI).GetMethod("FillPropertyContextMenu", BindingFlags.Static | BindingFlags.NonPublic);
            var propertyParam = Expression.Parameter(typeof(SerializedProperty), "property");
            var FillPropertyContextMenuBlock = Expression.Block(
                Expression.Call(null, FillPropertyContextMenuInfo, propertyParam, Expression.Constant(null, typeof(SerializedProperty)), Expression.Constant(null, typeof(GenericMenu)))
            );
            var FillPropertyContextMenuLambda = Expression.Lambda<Func<SerializedProperty, GenericMenu>>(FillPropertyContextMenuBlock, propertyParam);
            FillPropertyContextMenu = FillPropertyContextMenuLambda.Compile();
        }

        #endregion

        SerializedProperty m_Elements;
        ReorderableList m_List;
        Rect? m_ReservedListSizeRect;
        static Shader s_ProceduralThumbnailShader;
        static readonly int k_PreviewSize = 128;
        static readonly int k_FlareColorValue = Shader.PropertyToID("_FlareColorValue");
        static readonly int k_FlareTex = Shader.PropertyToID("_FlareTex");
        // cf. LensFlareCommon.hlsl
        static readonly int k_FlareData0 = Shader.PropertyToID("_FlareData0");
        static readonly int k_FlareData1 = Shader.PropertyToID("_FlareData1");
        static readonly int k_FlareData2 = Shader.PropertyToID("_FlareData2");
        static readonly int k_FlareData3 = Shader.PropertyToID("_FlareData3");
        static readonly int k_FlareData4 = Shader.PropertyToID("_FlareData4");
        static readonly int k_FlarePreviewData = Shader.PropertyToID("_FlarePreviewData");

        class TextureCacheElement
        {
            public int hash = 0;
            public Texture2D computedTexture = new Texture2D(k_PreviewSize, k_PreviewSize, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
        }

        RTHandle m_PreviewTexture;
        List<TextureCacheElement> m_PreviewTextureCache;
        Material m_PreviewLensFlare = null;

        void OnEnable()
        {
            m_Elements = serializedObject.FindProperty("elements");

            m_List = new ReorderableList(serializedObject, m_Elements, true, true, true, true);
            m_List.drawHeaderCallback = DrawListHeader;
            m_List.drawFooterCallback = DrawListFooter;
            m_List.onAddCallback = OnAdd;
            m_List.onRemoveCallback = OnRemove;
            m_List.drawElementBackgroundCallback = DrawElementBackground;
            m_List.drawElementCallback = DrawElement;
            m_List.elementHeightCallback = ElementHeight;

            if (s_ProceduralThumbnailShader == null)
                s_ProceduralThumbnailShader = Shader.Find("Hidden/Core/LensFlareDataDrivenPreview");
            m_PreviewLensFlare = new Material(s_ProceduralThumbnailShader);

            if (m_PreviewTexture == null)
            {
                m_PreviewTexture = RTHandles.Alloc(k_PreviewSize, k_PreviewSize, colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB);
            }
            if (m_PreviewTextureCache == null)
            {
                m_PreviewTextureCache = new List<TextureCacheElement>(m_Elements.arraySize);
                for (int i = 0; i < m_Elements.arraySize; ++i)
                {
                    m_PreviewTextureCache.Add(new TextureCacheElement());
                }
            }
        }

        void OnDisable()
        {
            m_PreviewTexture?.Release();
            m_PreviewTexture = null;
            if (m_PreviewTextureCache != null)
            {
                foreach (TextureCacheElement tce in m_PreviewTextureCache)
                    DestroyImmediate(tce.computedTexture);
                m_PreviewTextureCache = null;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            m_List.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        void OnAdd(ReorderableList list)
        {
            int newIndex = list.count;
            m_Elements.arraySize = newIndex + 1;
            serializedObject.ApplyModifiedProperties();

            m_PreviewTextureCache.Add(new TextureCacheElement());

            // Set Default values
            (target as LensFlareDataSRP).elements[newIndex] = new LensFlareDataElementSRP();
            serializedObject.Update();
        }

        void OnRemove(ReorderableList list)
        {
            int deletedIndex = list.index;

            list.serializedProperty.DeleteArrayElementAtIndex(deletedIndex);
            list.serializedProperty.serializedObject.ApplyModifiedProperties();
            DestroyImmediate(m_PreviewTextureCache[deletedIndex].computedTexture);
            m_PreviewTextureCache.RemoveAt(deletedIndex);

            list.index = Mathf.Clamp(deletedIndex - 1, 0, list.count - 1);
        }

        #region Header and Footer
        void DrawListHeader(Rect rect)
        {
            Rect sizeRect = rect;
            sizeRect.x += Styles.sizeOffset;
            sizeRect.xMin = sizeRect.xMax - Styles.sizeWidth;

            // If we draw the size now, and the user decrease it,
            // it can lead to out of range issue. See Footer.
            m_ReservedListSizeRect = sizeRect;

            Rect labelRect = rect;
            labelRect.xMax = sizeRect.xMin;

            EditorGUI.LabelField(labelRect, Styles.mainHeader, EditorStyles.boldLabel);
        }

        void DrawListSize(Rect rect)
        {
            int previousCount = m_List.count;
            int newCount = EditorGUI.DelayedIntField(rect, previousCount);
            if (newCount < 0)
                newCount = 0;
            if (newCount != previousCount)
            {
                m_Elements.arraySize = newCount;
                serializedObject.ApplyModifiedProperties();

                // Set Default values
                if (newCount > previousCount)
                {
                    LensFlareDataSRP lensFlareData = target as LensFlareDataSRP;
                    for (int i = previousCount; i < newCount; ++i)
                        lensFlareData.elements[i] = new LensFlareDataElementSRP();
                    m_Elements.serializedObject.Update();
                }
            }
        }

        void DrawListFooter(Rect rect)
        {
            // Default footer
            ReorderableList.defaultBehaviours.DrawFooter(rect, m_List);

            // Display the size in the footer. So the list will be able to refresh and
            // it should not do out of range when removing.
            if (!m_ReservedListSizeRect.HasValue)
                return;

            DrawListSize(m_ReservedListSizeRect.Value);
        }

        #endregion

        #region Heights computation
        float ElementHeight(int index)
        {
            if (m_Elements.arraySize <= index)
                return 0;

            SerializedProperty element = m_Elements.GetArrayElementAtIndex(index);
            SerializedProperty isFoldOpened = element.FindPropertyRelative("isFoldOpened");
            SerializedProperty allowMultipleElement = element.FindPropertyRelative("allowMultipleElement");

            int titleLine = 0;
            int line = 0;
            if (isFoldOpened.boolValue)
            {
                SerializedProperty distribution = element.FindPropertyRelative("distribution");
                SerializedProperty type = element.FindPropertyRelative("flareType");
                SerializedProperty enableRadialDistortion = element.FindPropertyRelative("enableRadialDistortion");

                titleLine = 6;
                line = GetTypeCathegoryLines(type)
                    + GetColorCathegoryLines()
                    + GetTransformCathegoryLines()
                    + GetAxisTransformCathegoryLines()
                    + GetRadialDistortionCathegoryLines(enableRadialDistortion)
                    + GetMultipleElementsCathegoryLines(allowMultipleElement, distribution);
            }
            else
            {
                line = 3;   //Type, Tint, Intensity
                if (allowMultipleElement.boolValue)
                    line += 1;  //Count
            }

            return (line + titleLine) * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing)
                + (Mathf.Max(titleLine - 1, 0)) * (Styles.cathegorySpacing)
                + Styles.footerSeparatorHeight + Styles.headerHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        int GetTypeCathegoryLines(SerializedProperty type)
        {
            int line = 1; //Type
            switch (GetEnum<SRPLensFlareType>(type))
            {
                case SRPLensFlareType.Circle:
                    line += 3;
                    break;  //Gradient, Falloff, Invert
                case SRPLensFlareType.Image:
                    line += 2;
                    break;  //Flare Texture, Use Aspect Ratio
                case SRPLensFlareType.Polygon:
                    line += 5;
                    break;  //Gradient, Falloff, Side count, Roundness, Invert
            }
            return line;
        }

        int GetColorCathegoryLines()
            => 4; //Tint, Modulate by Light Color, Intensity, Blend Mode

        int GetTransformCathegoryLines()
            => 5; //Position Offset, Auto Rotate, Rotation, Scale, Uniform Scale

        int GetAxisTransformCathegoryLines()
            => 3; //Starting Position, Angular Offset, Translation Scale

        int GetRadialDistortionCathegoryLines(SerializedProperty enabled)
            => enabled.boolValue ? 4 : 1; //[Enable], Radial Edge Size, Radial Edge Curve, Relative to Center

        int GetMultipleElementsCathegoryLines(SerializedProperty enabled, SerializedProperty distribution)
        {
            if (!enabled.boolValue)
                return 1;   //[Enable]

            int line = 5;   //[Enable], Count, Distribution, Length Spread, Colors
            switch (GetEnum<SRPLensFlareDistribution>(distribution))
            {
                case SRPLensFlareDistribution.Uniform:
                    line += 1;
                    break;  //UniformAngle
                case SRPLensFlareDistribution.Curve:
                    line += 3;
                    break;  //Position Variation, Rotation, Scale
                case SRPLensFlareDistribution.Random:
                    line += 5;
                    break;  //Seed, Intensity Variation, Position Variation, Rotation Variation, Scale Variation
            }
            return line;
        }

        #endregion

        #region Draw element
        void DrawElementBackground(Rect rect, int index, bool isActive, bool isFocused)
            => EditorGUI.DrawRect(rect, Styles.elementBackgroundColor);

        static Gradient SafeGradientValue(SerializedProperty sp)
        {
            BindingFlags instanceAnyPrivacyBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            PropertyInfo propertyInfo = typeof(SerializedProperty).GetProperty(
                "gradientValue",
                instanceAnyPrivacyBindingFlags,
                null,
                typeof(Gradient),
                new Type[0],
                null
            );

            if (propertyInfo == null)
                return null;

            Gradient gradientValue = propertyInfo.GetValue(sp, null) as Gradient;

            return gradientValue;
        }

        void ComputeThumbnail(ref Texture2D computedTexture, SerializedProperty element, SRPLensFlareType type, int index)
        {
            SerializedProperty colorProp = element.FindPropertyRelative("tint");
            SerializedProperty intensityProp = element.FindPropertyRelative("m_LocalIntensity");
            SerializedProperty sideCountProp = element.FindPropertyRelative("m_SideCount");
            SerializedProperty rotationProp = element.FindPropertyRelative("rotation");
            SerializedProperty edgeOffsetProp = element.FindPropertyRelative("m_EdgeOffset");
            SerializedProperty fallOffProp = element.FindPropertyRelative("m_FallOff");
            SerializedProperty sdfRoundnessProp = element.FindPropertyRelative("m_SdfRoundness");
            SerializedProperty inverseSDFProp = element.FindPropertyRelative("inverseSDF");
            SerializedProperty flareTextureProp = element.FindPropertyRelative("lensFlareTexture");
            SerializedProperty preserveAspectRatioProp = element.FindPropertyRelative("preserveAspectRatio");

            SerializedProperty sizeXYProp = element.FindPropertyRelative("sizeXY");

            SerializedProperty countProp = element.FindPropertyRelative("m_Count");
            SerializedProperty allowMultipleElementProp = element.FindPropertyRelative("allowMultipleElement");

            float invSideCount = 1f / ((float)sideCountProp.intValue);
            float intensity = intensityProp.floatValue;
            float usedSDFRoundness = sdfRoundnessProp.floatValue;

            Vector2 sizeXY = sizeXYProp.vector2Value;
            Vector2 sizeXYAbs = new Vector2(Mathf.Abs(sizeXY.x), Mathf.Abs(sizeXY.y));
            Vector2 localSize = new Vector2(sizeXY.x / Mathf.Max(sizeXYAbs.x, sizeXYAbs.y), sizeXY.y / Mathf.Max(sizeXYAbs.x, sizeXYAbs.y));
            const float maxStretch = 50.0f;
            localSize = new Vector2(Mathf.Min(localSize.x, maxStretch), Mathf.Min(localSize.y, maxStretch));

            Texture2D flareTex = flareTextureProp.objectReferenceValue as Texture2D;

            float usedAspectRatio;
            if (type == SRPLensFlareType.Image)
                usedAspectRatio = flareTex ? ((((float)flareTex.height) / (float)flareTex.width)) : 1.0f;
            else
                usedAspectRatio = 1.0f;

            if (type == SRPLensFlareType.Image && preserveAspectRatioProp.boolValue)
            {
                if (usedAspectRatio >= 1.0f)
                {
                    localSize = new Vector2(localSize.x / usedAspectRatio, localSize.y);
                }
                else
                {
                    localSize = new Vector2(localSize.x, localSize.y * usedAspectRatio);
                }
            }

            float usedGradientPosition = Mathf.Clamp01((1.0f - edgeOffsetProp.floatValue) - 1e-6f);
            if (type == SRPLensFlareType.Polygon)
                usedGradientPosition = Mathf.Pow(usedGradientPosition + 1.0f, 5);

            if (allowMultipleElementProp.boolValue && countProp.intValue > 1)
            {
                SerializedProperty distributionProp = element.FindPropertyRelative("distribution");
                SerializedProperty lengthSpreadProp = element.FindPropertyRelative("lengthSpread");
                SerializedProperty colorGradientProp = element.FindPropertyRelative("colorGradient");
                SerializedProperty seedProp = element.FindPropertyRelative("seed");
                SerializedProperty intensityVariationProp = element.FindPropertyRelative("m_IntensityVariation");
                SerializedProperty scaleVariationProp = element.FindPropertyRelative("scaleVariation");
                SerializedProperty rotationVariationProp = element.FindPropertyRelative("rotationVariation");
                SerializedProperty uniformAngleProp = element.FindPropertyRelative("uniformAngle");

                float RandomRange(float min, float max)
                {
                    return UnityEngine.Random.Range(min, max);
                }

                float rescale = float.MaxValue;

                UnityEngine.Random.State backupRandState = UnityEngine.Random.state;
                UnityEngine.Random.InitState(seedProp.intValue);

                float currentAngle = 0.0f;
                for (int idx = 0; idx < countProp.intValue; ++idx)
                {
                    Vector4 flareData0 = LensFlareCommonSRP.GetFlareData0(Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero, rotationProp.floatValue + currentAngle, 0f, 0f, Vector2.zero, false);

                    float cos0 = flareData0.x;
                    float sin0 = flareData0.y;

                    if (GetEnum<SRPLensFlareDistribution>(distributionProp) == SRPLensFlareDistribution.Random)
                        RandomRange(-1.0f, 1.0f); // Local Intensity

                    Vector2 localLocalSize;
                    if (GetEnum<SRPLensFlareDistribution>(distributionProp) == SRPLensFlareDistribution.Uniform)
                        localLocalSize = localSize;
                    else if (GetEnum<SRPLensFlareDistribution>(distributionProp) == SRPLensFlareDistribution.Random)
                        localLocalSize = localSize + localSize * (scaleVariationProp.floatValue * RandomRange(-1.0f, 1.0f));
                    else
                        localLocalSize = localSize;

                    Vector2 rotQuadCorner = new Vector2(cos0 * localLocalSize.x - sin0 * localLocalSize.y, sin0 * localLocalSize.x + cos0 * localLocalSize.y);
                    rescale = Mathf.Min(rescale, 1.0f / Mathf.Max(Mathf.Abs(rotQuadCorner.x), Mathf.Abs(rotQuadCorner.y)));

                    if (lengthSpreadProp.floatValue == 0.0f)
                    {
                        switch (GetEnum<SRPLensFlareDistribution>(distributionProp))
                        {
                            case SRPLensFlareDistribution.Uniform:
                                currentAngle += uniformAngleProp.floatValue;
                                break;
                            case SRPLensFlareDistribution.Curve:
                                RandomRange(0.0f, 1.0f); // Color
                                RandomRange(-1.0f, 1.0f); // Position
                                currentAngle = rotationProp.floatValue + RandomRange(-Mathf.PI, Mathf.PI) * rotationVariationProp.floatValue;
                                RandomRange(-1.0f, 1.0f); // Position Offset
                                break;
                            case SRPLensFlareDistribution.Random:
                                break;
                        }
                    }
                }

                UnityEngine.Random.InitState(seedProp.intValue);

                float localIntensity = intensity;
                currentAngle = 0.0f;
                for (int idx = 0; idx < countProp.intValue; ++idx)
                {
                    if (GetEnum<SRPLensFlareDistribution>(distributionProp) == SRPLensFlareDistribution.Random)
                    {
                        localIntensity = intensity * RandomRange(-1.0f, 1.0f) * intensityVariationProp.floatValue + 1.0f;
                    }

                    Vector2 localLocalSize;
                    if (GetEnum<SRPLensFlareDistribution>(distributionProp) == SRPLensFlareDistribution.Uniform)
                        localLocalSize = localSize;
                    else if (GetEnum<SRPLensFlareDistribution>(distributionProp) == SRPLensFlareDistribution.Random)
                        localLocalSize = localSize + localSize * (scaleVariationProp.floatValue * RandomRange(-1.0f, 1.0f));
                    else
                        localLocalSize = localSize;

                    // Set here what need to be setup in the material
                    if (type == SRPLensFlareType.Image)
                    {
                        if (flareTextureProp.objectReferenceValue != null)
                            m_PreviewLensFlare.SetTexture(k_FlareTex, flareTextureProp.objectReferenceValue as Texture2D);
                        else
                            m_PreviewLensFlare.SetTexture(k_FlareTex, Texture2D.blackTexture);
                    }
                    else
                    {
                        m_PreviewLensFlare.SetTexture(k_FlareTex, null);
                    }

                    Color usedColor = colorProp.colorValue;
                    if (GetEnum<SRPLensFlareDistribution>(distributionProp) == SRPLensFlareDistribution.Random)
                    {
                        UnityEngine.Gradient colorGradient = SafeGradientValue(colorGradientProp);
                        Color randCol = colorGradient.Evaluate(RandomRange(0.0f, 1.0f));
                        RandomRange(-1.0f, 1.0f); // Position
                        currentAngle = RandomRange(-Mathf.PI, Mathf.PI) * rotationVariationProp.floatValue;
                        RandomRange(-1.0f, 1.0f); // Position Offset
                        usedColor *= randCol;
                    }

                    m_PreviewLensFlare.SetVector(k_FlareColorValue, new Vector4(usedColor.r * localIntensity, usedColor.g * localIntensity, usedColor.b * localIntensity, 1f));
                    Vector4 flareData0 = LensFlareCommonSRP.GetFlareData0(Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero, rotationProp.floatValue + currentAngle, 0f, 0f, Vector2.zero, false);
                    m_PreviewLensFlare.SetVector(k_FlareData0, flareData0);
                    // x: OcclusionRadius, y: OcclusionSampleCount, z: ScreenPosZ, w: ScreenRatio
                    m_PreviewLensFlare.SetVector(k_FlareData1, new Vector4(0f, 0f, 0f, 1f));
                    // xy: ScreenPos, zw: FlareSize
                    m_PreviewLensFlare.SetVector(k_FlareData2, new Vector4(0f, 0f, rescale * localLocalSize.x, rescale * localLocalSize.y));
                    // x: Allow Offscreen, y: Edge Offset, z: Falloff, w: invSideCount
                    m_PreviewLensFlare.SetVector(k_FlareData3, new Vector4(0f, usedGradientPosition, Mathf.Exp(Mathf.Lerp(0.0f, 4.0f, Mathf.Clamp01(1.0f - fallOffProp.floatValue))), invSideCount));

                    if (type == SRPLensFlareType.Polygon)
                    {
                        // Precompute data for Polygon SDF (cf. LensFlareCommon.hlsl)
                        float rCos = Mathf.Cos(Mathf.PI * invSideCount);
                        float roundValue = rCos * usedSDFRoundness;
                        float r = rCos - roundValue;
                        float an = 2.0f * Mathf.PI * invSideCount;
                        float he = r * Mathf.Tan(0.5f * an);

                        // x: SDF Roundness, y: Poly Radius, z: PolyParam0, w: PolyParam1
                        m_PreviewLensFlare.SetVector(k_FlareData4, new Vector4(usedSDFRoundness, r, an, he));
                    }
                    else
                    {
                        // x: SDF Roundness, yzw: Unused
                        m_PreviewLensFlare.SetVector(k_FlareData4, new Vector4(usedSDFRoundness, 0f, 0f, 0f));
                    }

                    // xy: _FlarePreviewData.xy, z: ScreenRatio
                    m_PreviewLensFlare.SetVector(k_FlarePreviewData, new Vector4(k_PreviewSize, k_PreviewSize, 1f, 0f));

                    m_PreviewLensFlare.SetPass((int)type + ((type != SRPLensFlareType.Image && inverseSDFProp.boolValue) ? 2 : 0));

                    if (localIntensity > 0.0f)
                        RenderToTexture2D(ref computedTexture, idx == 0);

                    if (lengthSpreadProp.floatValue == 0.0f)
                    {
                        switch (GetEnum<SRPLensFlareDistribution>(distributionProp))
                        {
                            case SRPLensFlareDistribution.Uniform:
                                currentAngle += uniformAngleProp.floatValue;
                                break;
                        }
                    }
                }

                UnityEngine.Random.state = backupRandState;
            }
            else
            {
                Vector4 flareData0 = LensFlareCommonSRP.GetFlareData0(Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero, rotationProp.floatValue, 0f, 0f, Vector2.zero, false);

                float cos0 = flareData0.x;
                float sin0 = flareData0.y;

                Vector2 rotQuadCorner = new Vector2(cos0 * localSize.x - sin0 * localSize.y, sin0 * localSize.x + cos0 * localSize.y);
                float rescale = 1.0f / Mathf.Max(Mathf.Abs(rotQuadCorner.x), Mathf.Abs(rotQuadCorner.y));

                // Set here what need to be setup in the material
                if (type == SRPLensFlareType.Image)
                {
                    if (flareTextureProp.objectReferenceValue != null)
                        m_PreviewLensFlare.SetTexture(k_FlareTex, flareTextureProp.objectReferenceValue as Texture2D);
                    else
                        m_PreviewLensFlare.SetTexture(k_FlareTex, Texture2D.blackTexture);
                }
                else
                {
                    m_PreviewLensFlare.SetTexture(k_FlareTex, null);
                }

                m_PreviewLensFlare.SetVector(k_FlareColorValue, new Vector4(colorProp.colorValue.r * intensity, colorProp.colorValue.g * intensity, colorProp.colorValue.b * intensity, 1f));
                m_PreviewLensFlare.SetVector(k_FlareData0, flareData0);
                // x: OcclusionRadius, y: OcclusionSampleCount, z: ScreenPosZ, w: ScreenRatio
                m_PreviewLensFlare.SetVector(k_FlareData1, new Vector4(0f, 0f, 0f, 1f));
                // xy: ScreenPos, zw: FlareSize
                m_PreviewLensFlare.SetVector(k_FlareData2, new Vector4(0f, 0f, rescale * localSize.x, rescale * localSize.y));
                // x: Allow Offscreen, y: Edge Offset, z: Falloff, w: invSideCount
                m_PreviewLensFlare.SetVector(k_FlareData3, new Vector4(0f, usedGradientPosition, Mathf.Exp(Mathf.Lerp(0.0f, 4.0f, Mathf.Clamp01(1.0f - fallOffProp.floatValue))), invSideCount));

                if (type == SRPLensFlareType.Polygon)
                {
                    // Precompute data for Polygon SDF (cf. LensFlareCommon.hlsl)
                    float rCos = Mathf.Cos(Mathf.PI * invSideCount);
                    float roundValue = rCos * usedSDFRoundness;
                    float r = rCos - roundValue;
                    float an = 2.0f * Mathf.PI * invSideCount;
                    float he = r * Mathf.Tan(0.5f * an);

                    // x: SDF Roundness, y: Poly Radius, z: PolyParam0, w: PolyParam1
                    m_PreviewLensFlare.SetVector(k_FlareData4, new Vector4(usedSDFRoundness, r, an, he));
                }
                else
                {
                    // x: SDF Roundness, yzw: Unused
                    m_PreviewLensFlare.SetVector(k_FlareData4, new Vector4(usedSDFRoundness, 0f, 0f, 0f));
                }

                // xy: _FlarePreviewData.xy, z: ScreenRatio
                m_PreviewLensFlare.SetVector(k_FlarePreviewData, new Vector4(k_PreviewSize, k_PreviewSize, 1f, 0f));

                m_PreviewLensFlare.SetPass((int)type + ((type != SRPLensFlareType.Image && inverseSDFProp.boolValue) ? 2 : 0));

                RenderToTexture2D(ref computedTexture);
            }
        }

        void RenderToTexture2D(ref Texture2D computedTexture, bool clear = true)
        {
            RenderTexture oldActive = RenderTexture.active;
            RenderTexture.active = m_PreviewTexture.rt;

            if (clear)
                GL.Clear(false, true, Color.black);

            GL.PushMatrix();
            GL.LoadOrtho();
            GL.Viewport(new Rect(0, 0, k_PreviewSize, k_PreviewSize));

            GL.Begin(GL.QUADS);
            GL.TexCoord2(0, 0);
            GL.Vertex3(0f, 0f, 0);
            GL.TexCoord2(0, 1);
            GL.Vertex3(0f, 1f, 0);
            GL.TexCoord2(1, 1);
            GL.Vertex3(1f, 1f, 0);
            GL.TexCoord2(1, 0);
            GL.Vertex3(1f, 0f, 0);
            GL.End();
            GL.PopMatrix();

            computedTexture.ReadPixels(new Rect(0, 0, k_PreviewSize, k_PreviewSize), 0, 0, false);
            computedTexture.Apply(false);

            RenderTexture.active = oldActive;
        }

        int GetElementHash(SerializedProperty element, SRPLensFlareType type, int index)
        {
            SerializedProperty sizeXYProp = element.FindPropertyRelative("sizeXY");

            SerializedProperty colorProp = element.FindPropertyRelative("tint");
            SerializedProperty intensityProp = element.FindPropertyRelative("m_LocalIntensity");
            SerializedProperty rotationProp = element.FindPropertyRelative("rotation");
            SerializedProperty uniformScaleProp = element.FindPropertyRelative("uniformScale");

            int hash = index.GetHashCode();
            hash = hash * 23 + intensityProp.floatValue.GetHashCode();
            hash = hash * 23 + uniformScaleProp.floatValue.GetHashCode();
            hash = hash * 23 + sizeXYProp.vector2Value.GetHashCode();
            hash = hash * 23 + type.GetHashCode();
            hash = hash * 23 + colorProp.colorValue.GetHashCode();
            hash = hash * 23 + rotationProp.floatValue.GetHashCode();

            SerializedProperty allowMultipleElement = element.FindPropertyRelative("allowMultipleElement");

            hash = hash * 23 + allowMultipleElement.boolValue.GetHashCode();
            if (allowMultipleElement.boolValue)
            {
                SerializedProperty count = element.FindPropertyRelative("m_Count");
                SerializedProperty seedProp = element.FindPropertyRelative("seed");
                SerializedProperty distributionProp = element.FindPropertyRelative("distribution");
                SerializedProperty intensityVariationProp = element.FindPropertyRelative("m_IntensityVariation");
                SerializedProperty rotationVariationProp = element.FindPropertyRelative("rotationVariation");
                SerializedProperty scaleVariationProp = element.FindPropertyRelative("scaleVariation");

                hash = hash * 23 + count.intValue.GetHashCode();
                hash = hash * 23 + seedProp.intValue.GetHashCode();
                hash = hash * 23 + distributionProp.intValue.GetHashCode();
                hash = hash * 23 + intensityVariationProp.floatValue.GetHashCode();
                hash = hash * 23 + rotationVariationProp.floatValue.GetHashCode();
                hash = hash * 23 + scaleVariationProp.floatValue.GetHashCode();
            }

            if (type == SRPLensFlareType.Image)
            {
                SerializedProperty flareTextureProp = element.FindPropertyRelative("lensFlareTexture");
                SerializedProperty preserveAspectRatioProp = element.FindPropertyRelative("preserveAspectRatio");
                if (flareTextureProp.objectReferenceValue != null)
                    hash = hash * 23 + (flareTextureProp.objectReferenceValue as Texture2D).GetHashCode();

                hash = hash * 23 + preserveAspectRatioProp.boolValue.GetHashCode();
            }
            else
            {
                SerializedProperty inverseSDFProp = element.FindPropertyRelative("inverseSDF");
                SerializedProperty sdfRoundnessProp = element.FindPropertyRelative("m_SdfRoundness");
                SerializedProperty edgeOffsetProp = element.FindPropertyRelative("m_EdgeOffset");
                SerializedProperty fallOffProp = element.FindPropertyRelative("m_FallOff");

                hash = hash * 23 + inverseSDFProp.boolValue.GetHashCode();
                hash = hash * 23 + sdfRoundnessProp.floatValue.GetHashCode();
                hash = hash * 23 + fallOffProp.floatValue.GetHashCode();
                hash = hash * 23 + edgeOffsetProp.floatValue.GetHashCode();

                if (type == SRPLensFlareType.Polygon)
                {
                    SerializedProperty sideCountProp = element.FindPropertyRelative("m_SideCount");
                    hash = hash * 23 + sideCountProp.intValue.GetHashCode();
                }
            }

            return hash;
        }

        Texture2D GetCachedThumbnailProceduralTexture(SerializedProperty element, SRPLensFlareType type, int index)
        {
            if (m_PreviewTextureCache.Count <= index)
            {
                m_PreviewTextureCache.Add(new TextureCacheElement());
            }
            TextureCacheElement tce = m_PreviewTextureCache[index];
            int currentHash = GetElementHash(element, type, index);
            if (tce.hash == currentHash)
                return tce.computedTexture;

            ComputeThumbnail(ref tce.computedTexture, element, type, index);
            tce.hash = currentHash;
            return tce.computedTexture;
        }

        void DrawThumbnailProcedural(Rect rect, SerializedProperty element, SRPLensFlareType type, int index)
        {
            EditorGUI.DrawRect(rect, Color.black);
            Color oldGuiColor = GUI.color;
            GUI.color = Color.black; //set background color for transparency

            if (type != SRPLensFlareType.Image)
            {
                EditorGUI.DrawRect(rect, GUI.color); //draw margin
                rect.xMin += Styles.iconMargin;
                rect.xMax -= Styles.iconMargin;
                rect.yMin += Styles.iconMargin;
                rect.yMax -= Styles.iconMargin;
            }

            Texture2D previewTecture = GetCachedThumbnailProceduralTexture(element, type, index);
            EditorGUI.DrawTextureTransparent(rect, previewTecture, ScaleMode.ScaleToFit, 1f);
            GUI.color = oldGuiColor;
        }

        void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            Rect headerRect = rect;
            headerRect.height = Styles.headerHeight;

            Rect contentRect = rect;
            contentRect.yMin += Styles.headerHeight + EditorGUIUtility.standardVerticalSpacing;
            contentRect.xMin -= 12;

            bool oldWideMode = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;

            SerializedProperty element = m_Elements.GetArrayElementAtIndex(index);
            SerializedProperty isFoldOpened = element.FindPropertyRelative("isFoldOpened");

            if (DrawElementHeader(headerRect, isFoldOpened, selectedInList: isActive, element))
                DrawFull(contentRect, element);
            else
                DrawSummary(contentRect, element, index);

            EditorGUIUtility.wideMode = oldWideMode;
        }

        bool DrawElementHeader(Rect headerRect, SerializedProperty isFoldOpened, bool selectedInList, SerializedProperty element)
        {
            Rect visibilityRect = headerRect;
            visibilityRect.xMin += 16;
            visibilityRect.width = 13;
            visibilityRect.y += 2;
            visibilityRect.height = 13;

            Rect labelRect = headerRect;
            labelRect.xMin = visibilityRect.xMax + 5;
            labelRect.xMax -= 16;

            Rect contextMenuRect = labelRect;
            contextMenuRect.xMin = labelRect.xMax;
            contextMenuRect.width = 16;
            contextMenuRect.y += 1;
            contextMenuRect.height = 16;

            EditorGUI.BeginProperty(headerRect, Styles.elementHeader, element); //handle contextual menu on name

            Rect foldoutRect = headerRect;
            foldoutRect.y += 3f;
            foldoutRect.width = 13;
            foldoutRect.height = 13;

            // Title
            if (Event.current.type == EventType.Repaint)
                Styles.blueFocusedBoldLabel.Draw(labelRect, Styles.elementHeader, hasKeyboardFocus: selectedInList, isActive: false, on: false, isHover: false);

            // Collapsable behaviour
            bool previousState = isFoldOpened.boolValue;
            bool newState = GUI.Toggle(foldoutRect, previousState, GUIContent.none, EditorStyles.foldout);

            var e = Event.current;
            if (e.type == EventType.MouseDown && labelRect.Contains(e.mousePosition) && e.button == 0)
            {
                newState = !previousState;
                GUI.changed = true;
                e.Use();
            }

            // ellipsis menu
            if (GUI.Button(contextMenuRect, CoreEditorStyles.contextMenuIcon, CoreEditorStyles.contextMenuStyle))
            {
                GenericMenu menu = FillPropertyContextMenu(element);
                menu.DropDown(new Rect(new Vector2(contextMenuRect.x, contextMenuRect.yMax), Vector2.zero));
            }

            if (newState ^ previousState)
                isFoldOpened.boolValue = newState;

            EditorGUI.EndProperty();

            SerializedProperty visible = element.FindPropertyRelative("visible");
            EditorGUI.BeginChangeCheck();
            bool newVisibility = GUI.Toggle(visibilityRect, visible.boolValue, GUIContent.none, CoreEditorStyles.smallTickbox);
            if (EditorGUI.EndChangeCheck())
                visible.boolValue = newVisibility;

            return newState;
        }

        void DrawSummary(Rect summaryRect, SerializedProperty element, int index)
        {
            SerializedProperty type = element.FindPropertyRelative("flareType");
            SerializedProperty tint = element.FindPropertyRelative("tint");
            SerializedProperty intensity = element.FindPropertyRelative("m_LocalIntensity");
            SerializedProperty allowMultipleElement = element.FindPropertyRelative("allowMultipleElement");
            SerializedProperty count = element.FindPropertyRelative("m_Count");

            Rect thumbnailRect = OffsetForThumbnail(ref summaryRect);
            DrawThumbnailProcedural(thumbnailRect, element, GetEnum<SRPLensFlareType>(type), index);

            IEnumerator<Rect> fieldRect = ReserveFields(summaryRect, allowMultipleElement.boolValue ? 4 : 3);
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth -= thumbnailRect.width + Styles.horiwontalSpaceBetweenThumbnailAndInspector + Styles.shrinkingLabel;

            fieldRect.MoveNext();
            EditorGUI.PropertyField(fieldRect.Current, type, Styles.type);

            fieldRect.MoveNext();
            EditorGUI.PropertyField(fieldRect.Current, tint, Styles.tint);

            fieldRect.MoveNext();
            EditorGUI.PropertyField(fieldRect.Current, intensity, Styles.intensity);

            if (allowMultipleElement.boolValue)
            {
                fieldRect.MoveNext();
                EditorGUI.PropertyField(fieldRect.Current, count, Styles.count);
            }

            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        void DrawFull(Rect remainingRect, SerializedProperty element)
        {
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth -= Styles.shrinkingLabel;

            DrawTypeCathegory(ref remainingRect, element);
            DrawColorCathegory(ref remainingRect, element);
            DrawTransformCathegory(ref remainingRect, element);
            DrawAxisTransformCathegory(ref remainingRect, element);
            DrawRadialDistortionCathegory(ref remainingRect, element);
            DrawMultipleElementsCathegory(ref remainingRect, element);

            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        void DrawTypeCathegory(ref Rect remainingRect, SerializedProperty element)
        {
            SerializedProperty type = element.FindPropertyRelative("flareType");

            IEnumerator<Rect> fieldRect = ReserveCathegory(remainingRect, GetTypeCathegoryLines(type));

            fieldRect.MoveNext();
            EditorGUI.LabelField(fieldRect.Current, Styles.typeCathegory, EditorStyles.boldLabel);

            fieldRect.MoveNext();
            EditorGUI.PropertyField(fieldRect.Current, type, Styles.type);

            SRPLensFlareType flareType = GetEnum<SRPLensFlareType>(type);
            switch (flareType)
            {
                case SRPLensFlareType.Image:
                {
                    SerializedProperty flareTexture = element.FindPropertyRelative("lensFlareTexture");
                    SerializedProperty preserveAspectRatio = element.FindPropertyRelative("preserveAspectRatio");

                    // display it with texture icon
                    fieldRect.MoveNext();
                    EditorGUI.BeginChangeCheck();
                    Texture newTexture = EditorGUI.ObjectField(fieldRect.Current, Styles.flareTexture, flareTexture.objectReferenceValue, typeof(Texture), false) as Texture;
                    if (EditorGUI.EndChangeCheck())
                        flareTexture.objectReferenceValue = newTexture;

                    fieldRect.MoveNext();
                    EditorGUI.PropertyField(fieldRect.Current, preserveAspectRatio, Styles.preserveAspectRatio);
                }
                break;

                case SRPLensFlareType.Circle:
                case SRPLensFlareType.Polygon:
                {
                    SerializedProperty gradient = element.FindPropertyRelative("m_EdgeOffset");
                    SerializedProperty fallOff = element.FindPropertyRelative("m_FallOff");
                    SerializedProperty inverseSDF = element.FindPropertyRelative("inverseSDF");

                    fieldRect.MoveNext();
                    EditorGUI.PropertyField(fieldRect.Current, gradient, Styles.gradient);

                    fieldRect.MoveNext();
                    EditorGUI.PropertyField(fieldRect.Current, fallOff, Styles.fallOff);

                    if (flareType == SRPLensFlareType.Polygon)
                    {
                        SerializedProperty sideCount = element.FindPropertyRelative("m_SideCount");
                        SerializedProperty sdfRoundness = element.FindPropertyRelative("m_SdfRoundness");

                        fieldRect.MoveNext();
                        EditorGUI.PropertyField(fieldRect.Current, sideCount, Styles.sideCount);

                        fieldRect.MoveNext();
                        EditorGUI.PropertyField(fieldRect.Current, sdfRoundness, Styles.sdfRoundness);
                    }

                    fieldRect.MoveNext();
                    EditorGUI.PropertyField(fieldRect.Current, inverseSDF, Styles.inverseSDF);
                }
                break;
            }

            // update remaining
            fieldRect.MoveNext();
            remainingRect = fieldRect.Current;
        }

        void DrawColorCathegory(ref Rect remainingRect, SerializedProperty element)
        {
            SerializedProperty tint = element.FindPropertyRelative("tint");
            SerializedProperty modulateByLightColor = element.FindPropertyRelative("modulateByLightColor");
            SerializedProperty intensity = element.FindPropertyRelative("m_LocalIntensity");
            SerializedProperty blendMode = element.FindPropertyRelative("blendMode");

            IEnumerator<Rect> fieldRect = ReserveCathegory(remainingRect, GetColorCathegoryLines());

            fieldRect.MoveNext();
            EditorGUI.LabelField(fieldRect.Current, Styles.colorCathegory, EditorStyles.boldLabel);

            fieldRect.MoveNext();
            EditorGUI.PropertyField(fieldRect.Current, tint, Styles.tint);

            fieldRect.MoveNext();
            EditorGUI.PropertyField(fieldRect.Current, modulateByLightColor, Styles.modulateByLightColor);

            fieldRect.MoveNext();
            EditorGUI.PropertyField(fieldRect.Current, intensity, Styles.intensity);

            fieldRect.MoveNext();
            EditorGUI.PropertyField(fieldRect.Current, blendMode, Styles.blendMode);

            // update remaining
            fieldRect.MoveNext();
            remainingRect = fieldRect.Current;
        }

        void DrawTransformCathegory(ref Rect remainingRect, SerializedProperty element)
        {
            SerializedProperty positionOffset = element.FindPropertyRelative("positionOffset");
            SerializedProperty autoRotate = element.FindPropertyRelative("autoRotate");
            SerializedProperty rotation = element.FindPropertyRelative("rotation");
            SerializedProperty sizeXY = element.FindPropertyRelative("sizeXY");
            SerializedProperty uniformScale = element.FindPropertyRelative("uniformScale");

            IEnumerator<Rect> fieldRect = ReserveCathegory(remainingRect, GetTransformCathegoryLines());

            fieldRect.MoveNext();
            EditorGUI.LabelField(fieldRect.Current, Styles.transformCathegory, EditorStyles.boldLabel);

            fieldRect.MoveNext();
            EditorGUI.PropertyField(fieldRect.Current, positionOffset, Styles.positionOffset);

            fieldRect.MoveNext();
            EditorGUI.PropertyField(fieldRect.Current, autoRotate, Styles.autoRotate);

            fieldRect.MoveNext();
            EditorGUI.PropertyField(fieldRect.Current, rotation, Styles.rotation);

            fieldRect.MoveNext();
            EditorGUI.PropertyField(fieldRect.Current, sizeXY, Styles.sizeXY);

            fieldRect.MoveNext();
            EditorGUI.PropertyField(fieldRect.Current, uniformScale, Styles.uniformScale);

            // update remaining
            fieldRect.MoveNext();
            remainingRect = fieldRect.Current;
        }

        void DrawAxisTransformCathegory(ref Rect remainingRect, SerializedProperty element)
        {
            SerializedProperty position = element.FindPropertyRelative("position");
            SerializedProperty angularOffset = element.FindPropertyRelative("angularOffset");
            SerializedProperty translationScale = element.FindPropertyRelative("translationScale");

            IEnumerator<Rect> fieldRect = ReserveCathegory(remainingRect, GetAxisTransformCathegoryLines());

            fieldRect.MoveNext();
            EditorGUI.LabelField(fieldRect.Current, Styles.axisTransformCathegory, EditorStyles.boldLabel);

            fieldRect.MoveNext();
            EditorGUI.PropertyField(fieldRect.Current, position, Styles.position);

            fieldRect.MoveNext();
            EditorGUI.PropertyField(fieldRect.Current, angularOffset, Styles.angularOffset);

            fieldRect.MoveNext();
            EditorGUI.PropertyField(fieldRect.Current, translationScale, Styles.translationScale);

            // update remaining
            fieldRect.MoveNext();
            remainingRect = fieldRect.Current;
        }

        void DrawRadialDistortionCathegory(ref Rect remainingRect, SerializedProperty element)
        {
            SerializedProperty enableDistortion = element.FindPropertyRelative("enableRadialDistortion");

            IEnumerator<Rect> fieldRect = ReserveCathegory(remainingRect, GetRadialDistortionCathegoryLines(enableDistortion));

            fieldRect.MoveNext();
            EditorGUI.LabelField(fieldRect.Current, Styles.radialDistortionCathegory, EditorStyles.boldLabel);

            fieldRect.MoveNext();
            EditorGUI.PropertyField(fieldRect.Current, enableDistortion, Styles.enableDistortion);

            if (enableDistortion.boolValue)
            {
                SerializedProperty targetSizeDistortion = element.FindPropertyRelative("targetSizeDistortion");
                SerializedProperty distortionCurve = element.FindPropertyRelative("distortionCurve");
                SerializedProperty distortionRelativeToCenter = element.FindPropertyRelative("distortionRelativeToCenter");

                fieldRect.MoveNext();
                EditorGUI.PropertyField(fieldRect.Current, targetSizeDistortion, Styles.targetSizeDistortion);

                fieldRect.MoveNext();
                EditorGUI.PropertyField(fieldRect.Current, distortionCurve, Styles.distortionCurve);

                fieldRect.MoveNext();
                EditorGUI.PropertyField(fieldRect.Current, distortionRelativeToCenter, Styles.distortionRelativeToCenter);
            }

            // update remaining
            fieldRect.MoveNext();
            remainingRect = fieldRect.Current;
        }

        void DrawMultipleElementsCathegory(ref Rect remainingRect, SerializedProperty element)
        {
            SerializedProperty allowMultipleElement = element.FindPropertyRelative("allowMultipleElement");
            SerializedProperty distribution = element.FindPropertyRelative("distribution"); //needed for lines computation

            IEnumerator<Rect> fieldRect = ReserveCathegory(remainingRect, GetMultipleElementsCathegoryLines(allowMultipleElement, distribution));

            fieldRect.MoveNext();
            EditorGUI.LabelField(fieldRect.Current, Styles.multipleElementsCathegory, EditorStyles.boldLabel);

            fieldRect.MoveNext();
            EditorGUI.PropertyField(fieldRect.Current, allowMultipleElement, Styles.allowMultipleElement);

            if (allowMultipleElement.boolValue)
            {
                SerializedProperty count = element.FindPropertyRelative("m_Count");
                SerializedProperty colorGradient = element.FindPropertyRelative("colorGradient"); //used in all 3 case
                SerializedProperty lengthSpread = element.FindPropertyRelative("lengthSpread");

                fieldRect.MoveNext();
                EditorGUI.PropertyField(fieldRect.Current, count, Styles.count);

                fieldRect.MoveNext();
                EditorGUI.PropertyField(fieldRect.Current, distribution, Styles.distribution);

                fieldRect.MoveNext();
                EditorGUI.PropertyField(fieldRect.Current, lengthSpread, Styles.lengthSpread);

                switch (GetEnum<SRPLensFlareDistribution>(distribution))
                {
                    case SRPLensFlareDistribution.Uniform:
                        SerializedProperty uniformAngle = element.FindPropertyRelative("uniformAngle");

                        fieldRect.MoveNext();
                        EditorGUI.PropertyField(fieldRect.Current, colorGradient, Styles.colorGradient);

                        fieldRect.MoveNext();
                        EditorGUI.PropertyField(fieldRect.Current, uniformAngle, Styles.uniformAngle);
                        break;

                    case SRPLensFlareDistribution.Curve:
                        SerializedProperty positionCurve = element.FindPropertyRelative("positionCurve");
                        SerializedProperty scaleCurve = element.FindPropertyRelative("scaleCurve");
                        SerializedProperty uniformAngleCurve = element.FindPropertyRelative("uniformAngleCurve");

                        fieldRect.MoveNext();
                        EditorGUI.PropertyField(fieldRect.Current, colorGradient, Styles.colorGradient);

                        fieldRect.MoveNext();
                        EditorGUI.PropertyField(fieldRect.Current, positionCurve, Styles.positionCurve);

                        fieldRect.MoveNext();
                        EditorGUI.PropertyField(fieldRect.Current, uniformAngleCurve, Styles.uniformAngleCurve);

                        fieldRect.MoveNext();
                        EditorGUI.PropertyField(fieldRect.Current, scaleCurve, Styles.scaleCurve);
                        break;

                    case SRPLensFlareDistribution.Random:
                        SerializedProperty seed = element.FindPropertyRelative("seed");
                        SerializedProperty intensityVariation = element.FindPropertyRelative("m_IntensityVariation");
                        SerializedProperty positionVariation = element.FindPropertyRelative("positionVariation");
                        SerializedProperty rotationVariation = element.FindPropertyRelative("rotationVariation");
                        SerializedProperty scaleVariation = element.FindPropertyRelative("scaleVariation");

                        fieldRect.MoveNext();
                        EditorGUI.PropertyField(fieldRect.Current, seed, Styles.seed);

                        fieldRect.MoveNext();
                        EditorGUI.PropertyField(fieldRect.Current, intensityVariation, Styles.intensityVariation);

                        fieldRect.MoveNext();
                        EditorGUI.PropertyField(fieldRect.Current, colorGradient, Styles.colorGradient);

                        fieldRect.MoveNext();
                        EditorGUI.PropertyField(fieldRect.Current, positionVariation, Styles.positionVariation);

                        fieldRect.MoveNext();
                        EditorGUI.PropertyField(fieldRect.Current, rotationVariation, Styles.rotationVariation);

                        fieldRect.MoveNext();
                        EditorGUI.PropertyField(fieldRect.Current, scaleVariation, Styles.scaleVariation);
                        break;
                }
            }

            // update remaining
            fieldRect.MoveNext();
            remainingRect = fieldRect.Current;
        }

        #endregion

        #region Utility
        IEnumerator<Rect> ReserveFields(Rect rect, int fields)
        {
            Rect nextRect = rect;
            nextRect.height = EditorGUIUtility.singleLineHeight;
            yield return nextRect;

            --fields;
            for (; fields > 0; --fields)
            {
                nextRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                yield return nextRect;
            }
        }

        IEnumerator<Rect> ReserveCathegory(Rect remaining, int fields)
        {
            Rect headerRect = remaining;
            headerRect.height = EditorGUIUtility.singleLineHeight;
            yield return headerRect;

            remaining.yMin = headerRect.yMax + EditorGUIUtility.standardVerticalSpacing;
            IEnumerator<Rect> fieldEnumerator = ReserveFields(remaining, fields);
            while (fieldEnumerator.MoveNext())
            {
                remaining.yMin = fieldEnumerator.Current.yMax + EditorGUIUtility.standardVerticalSpacing;
                yield return fieldEnumerator.Current;
            }

            //lastly, return updated remaining. Return it because Iterator cannot use ref parameters
            remaining.yMin += Styles.cathegorySpacing;
            yield return remaining;
        }

        Rect OffsetForThumbnail(ref Rect remainingRect)
        {
            Rect thumbnailRect = remainingRect;
            thumbnailRect.width = Styles.thumbnailSize;
            thumbnailRect.height = Styles.thumbnailSize;
            thumbnailRect.y += ((int)remainingRect.height - Styles.thumbnailSize - Styles.footerSeparatorHeight) >> 1;
            remainingRect.xMin += Styles.thumbnailSize + Styles.horiwontalSpaceBetweenThumbnailAndInspector;
            return thumbnailRect;
        }

        // enumValueIndex is not the underling int but the index in enum names.
        // if enum don't start at 0, or skip a value, comparing int with enumValueIndex will fail.
        T GetEnum<T>(SerializedProperty property)
            => (T)(object)property.intValue;

        void SetEnum<T>(SerializedProperty property, T value)
            => property.intValue = (int)(object)value;
        #endregion
    }
}
