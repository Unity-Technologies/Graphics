using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using static Unity.Burst.Intrinsics.X86.Avx;
using static UnityEditor.Rendering.FilterWindow;
using static UnityEngine.Rendering.DebugUI.MessageBox;

namespace UnityEditor.Rendering
{
    [CustomEditor(typeof(LensFlareDataSRP))]
    [SupportedOnRenderPipeline]
    class LensFlareDataSRPEditor : Editor
    {
        static class Styles
        {
            public const int sizeWidth = 47;
            public const int sizeOffset = 5;
            public static readonly int headerHeight = (int)EditorGUIUtility.singleLineHeight;
            public static readonly int cathegorySpacing = 5;
            public const int footerSeparatorHeight = 5;
            public const int thumbnailSizeWidth = 72;
            public const int thumbnailSizeHeight = 72; // 1/1 ratio
            public const int iconMargin = 6; //margin for icon be ing at 75% of 52 thumbnail size
            public const int horiwontalSpaceBetweenThumbnailAndInspector = 5;
            public const int shrinkingLabel = 10;

            public static readonly Color elementBackgroundColor = EditorGUIUtility.isProSkin
                ? new Color32(65, 65, 65, 255)
                : new Color32(200, 200, 200, 255);

            public static readonly GUIContent mainHeader = EditorGUIUtility.TrTextContent("Elements", "List of elements in the Lens Flare.");
            public static readonly GUIContent elementHeader = EditorGUIUtility.TrTextContent("Lens Flare Element", "Elements in the Lens Flare.");

            // Cathegory headers
            static public readonly string typeCathegory = L10n.Tr("Type");
            static public readonly string noiseCathegory = L10n.Tr("Noise");
            static public readonly string colorCathegory = L10n.Tr("Color");
            static public readonly string cutoffCathegory = L10n.Tr("Cutoff");
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
            static public readonly GUIContent shapeCutOffSpeed = EditorGUIUtility.TrTextContent("Cutoff Speed", "Sets the speed at which the radius occludes the element.\nA value of zero (with a large radius) does not occlude anything. The higher this value, the faster the element is occluded on the side of the screen.\nThe effect of this value is more noticeable with multiple elements.");
            static public readonly GUIContent shapeCutOffRadius = EditorGUIUtility.TrTextContent("Cutoff Radius", "Sets the normalized radius of the lens shape used to occlude the lens flare element.\nA radius of one is equivalent to the scale of the element.");
            static public readonly GUIContent lensFlareDataSRP = EditorGUIUtility.TrTextContent("Asset", "Lens Flare Data SRP asset as an element.");
            // Type::Ring:
            static public readonly GUIContent noiseAmplitude = EditorGUIUtility.TrTextContent("Amplitude", "Amplitude of the sampling of the noise.");
            static public readonly GUIContent noiseFrequency = EditorGUIUtility.TrTextContent("Repeat", "Frequency of the sampling for the noise.");
            static public readonly GUIContent noiseSpeed = EditorGUIUtility.TrTextContent("Speed", "Scale the speed of the animation.");
            static public readonly GUIContent ringThickness = EditorGUIUtility.TrTextContent("Ring Thickness", "Ring Thickness.");

            // Color
            static public readonly GUIContent tintColorType = EditorGUIUtility.TrTextContent("Color Type", "Specify how to colorize the flare.");
            static public readonly GUIContent tint = EditorGUIUtility.TrTextContent("Tint", "Specifies the tint of the element. If the element type is set to Image, the Flare Texture is multiplied by this color.");
            static public readonly GUIContent tintRadial = EditorGUIUtility.TrTextContent("Tint Radial", "Specifies the radial gradient tint of the element. If the element type is set to Image, the Flare Texture is multiplied by this color.");
            static public readonly GUIContent tintAngular = EditorGUIUtility.TrTextContent("Tint Angular", "Specifies the angular gradient tint of the element. If the element type is set to Image, the Flare Texture is multiplied by this color.");
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
                Expression.Call(null, FillPropertyContextMenuInfo, propertyParam, Expression.Constant(null, typeof(SerializedProperty)), Expression.Constant(null, typeof(GenericMenu)), Expression.Constant(null, typeof(VisualElement)))
            );
            var FillPropertyContextMenuLambda = Expression.Lambda<Func<SerializedProperty, GenericMenu>>(FillPropertyContextMenuBlock, propertyParam);
            FillPropertyContextMenu = FillPropertyContextMenuLambda.Compile();
        }

        #endregion

        SerializedProperty m_Elements;
        ReorderableList m_List;
        Rect? m_ReservedListSizeRect;
        // cf. LensFlareCommon.hlsl
        static readonly int k_FlarePreviewData = Shader.PropertyToID("_FlarePreviewData");

        class TextureCacheElement
        {
            public int hash = 0;
            public Texture2D computedTexture = new Texture2D(Styles.thumbnailSizeWidth, Styles.thumbnailSizeHeight, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
        }

        RTHandle m_PreviewTexture;
        List<TextureCacheElement> m_PreviewTextureCache;
        CommandBuffer m_Cmd = null;

        void OnEnable()
        {
            m_Elements = serializedObject.FindProperty("elements");

            if (m_Cmd == null)
                m_Cmd = new CommandBuffer();

            m_List = new ReorderableList(serializedObject, m_Elements, true, true, true, true);
            m_List.drawHeaderCallback = DrawListHeader;
            m_List.drawFooterCallback = DrawListFooter;
            m_List.onAddCallback = OnAdd;
            m_List.onRemoveCallback = OnRemove;
            m_List.drawElementBackgroundCallback = DrawElementBackground;
            m_List.drawElementCallback = DrawElement;
            m_List.elementHeightCallback = ElementHeight;

            if (m_PreviewTexture == null)
            {
                m_PreviewTexture = RTHandles.Alloc(Styles.thumbnailSizeWidth, Styles.thumbnailSizeHeight, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB);
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
            SerializedProperty type = element.FindPropertyRelative("flareType");

            int titleLine = 0;
            int line;
            if (isFoldOpened.boolValue)
            {
                if (GetEnum<SRPLensFlareType>(type) != SRPLensFlareType.LensFlareDataSRP)
                {
                    SerializedProperty distribution = element.FindPropertyRelative("distribution");
                    SerializedProperty enableRadialDistortion = element.FindPropertyRelative("enableRadialDistortion");

                    int multipleElementsLines = GetMultipleElementsCathegoryLines(type, allowMultipleElement, distribution);

                    if (multipleElementsLines == 0)
                        titleLine = 6;
                    else
                        titleLine = 7;
                    line = GetTypeCathegoryLines(type)
                        + GetCutoffCathegoryLines()
                        + GetColorCathegoryLines()
                        + GetTransformCathegoryLines()
                        + GetAxisTransformCathegoryLines()
                        + GetRadialDistortionCathegoryLines(enableRadialDistortion)
                        + multipleElementsLines;
                }
                else
                {
                    titleLine = 1; // Type
                    line = 2; //Type, LensFlareDataSRP
                }
            }
            else
            {
                if (GetEnum<SRPLensFlareType>(type) != SRPLensFlareType.LensFlareDataSRP)
                {
                    line = 3;   //Type, Tint, Intensity
                    if (allowMultipleElement.boolValue)
                        line += 1;  //Count
                }
                else
                {
                    line = 3;   //Type, LensFlareDataSRP
                }
            }
            line = Math.Max(line, 4);

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
                    break;  //Gradient, Falloff, Invert, Side count, Roundness
                case SRPLensFlareType.Ring:
                    line += 8 + 2;
                    break;  //Gradient, Falloff, Invert, [Noise], Amplitude, Frequency, Speed, Ring Thickness
                case SRPLensFlareType.LensFlareDataSRP:
                    line += 2;
                    break;  //Asset
            }
            return line;
        }

        int GetColorCathegoryLines()
            => 5; //tintColorType, Tint/TintGradient, Modulate by Light Color, Intensity, Blend Mode

        int GetTransformCathegoryLines()
            => 5; //Position Offset, Auto Rotate, Rotation, Scale, Uniform Scale

        int GetAxisTransformCathegoryLines()
            => 3; //Starting Position, Angular Offset, Translation Scale
        int GetCutoffCathegoryLines()
            => 2; //Speed, Size

        int GetRadialDistortionCathegoryLines(SerializedProperty enabled)
            => enabled.boolValue ? 4 : 1; //[Enable], Radial Edge Size, Radial Edge Curve, Relative to Center

        int GetMultipleElementsCathegoryLines(SerializedProperty type, SerializedProperty enabled, SerializedProperty distribution)
        {
            SRPLensFlareType eType = GetEnum<SRPLensFlareType>(type);
            if (eType == SRPLensFlareType.Ring || eType == SRPLensFlareType.LensFlareDataSRP)
                return 0;

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
            LensFlareDataSRP lsSRP = target as LensFlareDataSRP;
            LensFlareDataElementSRP elementLocal = lsSRP.elements[index].Clone();
            elementLocal.blendMode = SRPLensFlareBlendMode.Additive;
            elementLocal.visible = true;

            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.DisableShaderKeyword("FLARE_HAS_OCCLUSION");
            Vector4 flareData1 = new Vector4(0.0f, 0.0f, 0.0f, ((float)Styles.thumbnailSizeHeight) / ((float)Styles.thumbnailSizeWidth));
            Vector2 screenSize = new Vector2(Styles.thumbnailSizeWidth, Styles.thumbnailSizeHeight);
            float screenRatio = screenSize.y / screenSize.x;
            Vector2 vScreenRatio = new Vector2(screenRatio, 1.0f);

            Shader local = Shader.Find("Hidden/Core/LensFlareDataDrivenPreview2");
            Material localMat = new Material(local);
            localMat.SetOverrideTag("RenderType", "Transparent");

            UnityEngine.Rendering.CoreUtils.SetRenderTarget(cmd, m_PreviewTexture.rt, ClearFlag.Color, Color.black);

            SerializedProperty allowMultipleElementProp = element.FindPropertyRelative("allowMultipleElement");
            SerializedProperty lengthSpreadProp = element.FindPropertyRelative("lengthSpread");
            SerializedProperty countProp = element.FindPropertyRelative("m_Count");
            SerializedProperty uniformScaleProp = element.FindPropertyRelative("uniformScale");
            SerializedProperty sizeXYProp = element.FindPropertyRelative("sizeXY");

            Vector2 center;
            float scale;
            if ((allowMultipleElementProp.boolValue && lengthSpreadProp.floatValue != 0.0f && countProp.intValue > 1 && type != SRPLensFlareType.Ring) ||
                type == SRPLensFlareType.LensFlareDataSRP)
            {
                center = new Vector2(0.5f, -0.5f);
                scale = 1.0f;
            }
            else
            {
                center = Vector2.zero;
                scale = 10.0f / uniformScaleProp.floatValue / Mathf.Max(sizeXYProp.vector2Value.x, sizeXYProp.vector2Value.y);
            }
            cmd.SetGlobalVector(k_FlarePreviewData, new Vector4(Styles.thumbnailSizeWidth, Styles.thumbnailSizeHeight, 1f, 0f));
            LensFlareCommonSRP.ProcessLensFlareSRPElementsSingle(
                elementLocal,
                cmd,
                Color.white,
                null,
                1.0f, scale,
                localMat, center,
                false,
                vScreenRatio,
                flareData1, false, 0);

            Graphics.ExecuteCommandBuffer(cmd);
            cmd.CopyTexture(m_PreviewTexture.rt, computedTexture);
            Graphics.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        int GetElementHash(SerializedProperty element, SRPLensFlareType type, int index)
        {
            int hash = index.GetHashCode();

            // Warning:
            // SerializedProperty::animationCurveValue
            // SerializedProperty::gradientValue
            // return a copy so GetHashCode is always different

            SerializedProperty lensFlareDataSRP = element.FindPropertyRelative("lensFlareDataSRP");
            if (type == SRPLensFlareType.LensFlareDataSRP && lensFlareDataSRP.boxedValue != null)
                hash = hash * 23 + lensFlareDataSRP.boxedValue.GetHashCode();

            SerializedProperty localIntensity = element.FindPropertyRelative("m_LocalIntensity");
            hash = hash * 23 + localIntensity.floatValue.GetHashCode();
            SerializedProperty position = element.FindPropertyRelative("position");
            hash = hash * 23 + position.floatValue.GetHashCode();
            SerializedProperty positionOffset = element.FindPropertyRelative("positionOffset");
            hash = hash * 23 + positionOffset.vector2Value.GetHashCode();
            SerializedProperty angularOffset = element.FindPropertyRelative("angularOffset");
            hash = hash * 23 + angularOffset.floatValue.GetHashCode();
            SerializedProperty translationScale = element.FindPropertyRelative("translationScale");
            hash = hash * 23 + translationScale.vector2Value.GetHashCode();
            SerializedProperty lensFlareTexture = element.FindPropertyRelative("lensFlareTexture");
            //if (lensFlareTexture.objectReferenceValue != null)
            //    hash = hash * 23 + (lensFlareTexture.objectReferenceValue as Texture2D).GetHashCode();
            SerializedProperty uniformScale = element.FindPropertyRelative("uniformScale");
            hash = hash * 23 + uniformScale.floatValue.GetHashCode();
            SerializedProperty sizeXY = element.FindPropertyRelative("sizeXY");
            hash = hash * 23 + sizeXY.vector2Value.GetHashCode();
            SerializedProperty allowMultipleElement = element.FindPropertyRelative("allowMultipleElement");
            hash = hash * 23 + allowMultipleElement.boolValue.GetHashCode();
            SerializedProperty count = element.FindPropertyRelative("m_Count");
            hash = hash * 23 + count.intValue.GetHashCode();
            SerializedProperty rotation = element.FindPropertyRelative("rotation");
            hash = hash * 23 + rotation.floatValue.GetHashCode();
            SerializedProperty preserveAspectRatio = element.FindPropertyRelative("preserveAspectRatio");
            hash = hash * 23 + preserveAspectRatio.boolValue.GetHashCode();

            SerializedProperty ringThickness = element.FindPropertyRelative("ringThickness");
            hash = hash * 23 + ringThickness.floatValue.GetHashCode();
            SerializedProperty hoopFactor = element.FindPropertyRelative("hoopFactor");
            hash = hash * 23 + hoopFactor.floatValue.GetHashCode();

            SerializedProperty noiseAmplitude = element.FindPropertyRelative("noiseAmplitude");
            hash = hash * 23 + noiseAmplitude.floatValue.GetHashCode();
            SerializedProperty noiseFrequency = element.FindPropertyRelative("noiseFrequency");
            hash = hash * 23 + noiseFrequency.intValue.GetHashCode();
            SerializedProperty noiseSpeed = element.FindPropertyRelative("noiseSpeed");
            hash = hash * 23 + noiseSpeed.floatValue.GetHashCode();

            SerializedProperty shapeCutOffSpeed = element.FindPropertyRelative("shapeCutOffSpeed");
            hash = hash * 23 + shapeCutOffSpeed.floatValue.GetHashCode();
            SerializedProperty shapeCutOffRadius = element.FindPropertyRelative("shapeCutOffRadius");
            hash = hash * 23 + shapeCutOffRadius.floatValue.GetHashCode();

            SerializedProperty tintColorType = element.FindPropertyRelative("tintColorType");
            hash = hash * 23 + tintColorType.enumValueIndex.GetHashCode();
            SerializedProperty tint = element.FindPropertyRelative("tint");
            hash = hash * 23 + tint.colorValue.GetHashCode();
            //SerializedProperty tintGradient = element.FindPropertyRelative("tintGradient");
            //hash = hash * 23 + (tintGradient.boxedValue as TextureGradient).GetTexture().updateCount.GetHashCode();
            SerializedProperty blendMode = element.FindPropertyRelative("blendMode");
            hash = hash * 23 + blendMode.enumValueIndex.GetHashCode();

            SerializedProperty autoRotate = element.FindPropertyRelative("autoRotate");
            hash = hash * 23 + autoRotate.boolValue.GetHashCode();
            SerializedProperty flareType = element.FindPropertyRelative("flareType");
            hash = hash * 23 + flareType.enumValueIndex.GetHashCode();

            SerializedProperty distribution = element.FindPropertyRelative("distribution");
            hash = hash * 23 + distribution.enumValueIndex.GetHashCode();

            SerializedProperty lengthSpread = element.FindPropertyRelative("lengthSpread");
            hash = hash * 23 + lengthSpread.floatValue.GetHashCode();
            //SerializedProperty colorGradient = element.FindPropertyRelative("colorGradient");
            //hash = hash * 23 + (colorGradient.boxedValue as Gradient).GetHashCode();
            //SerializedProperty positionCurve = element.FindPropertyRelative("positionCurve");
            //hash = hash * 23 + (positionCurve.boxedValue as AnimationCurve).keys.GetHashCode();
            //SerializedProperty scaleCurve = element.FindPropertyRelative("scaleCurve");
            //hash = hash * 23 + (scaleCurve.boxedValue as AnimationCurve).GetHashCode();
            //SerializedProperty uniformAngleCurve = element.FindPropertyRelative("uniformAngleCurve");
            //hash = hash * 23 + (uniformAngleCurve.boxedValue as AnimationCurve).GetHashCode();

            SerializedProperty seed = element.FindPropertyRelative("seed");
            hash = hash * 23 + seed.intValue.GetHashCode();
            SerializedProperty intensityVariation = element.FindPropertyRelative("m_IntensityVariation");
            hash = hash * 23 + intensityVariation.floatValue.GetHashCode();
            SerializedProperty positionVariation = element.FindPropertyRelative("positionVariation");
            hash = hash * 23 + positionVariation.vector2Value.GetHashCode();
            SerializedProperty scaleVariation = element.FindPropertyRelative("scaleVariation");
            hash = hash * 23 + scaleVariation.floatValue.GetHashCode();
            SerializedProperty rotationVariation = element.FindPropertyRelative("rotationVariation");
            hash = hash * 23 + rotationVariation.floatValue.GetHashCode();

            SerializedProperty enableRadialDistortion = element.FindPropertyRelative("enableRadialDistortion");
            hash = hash * 23 + enableRadialDistortion.boolValue.GetHashCode();
            SerializedProperty targetSizeDistortion = element.FindPropertyRelative("targetSizeDistortion");
            hash = hash * 23 + targetSizeDistortion.vector2Value.GetHashCode();
            //SerializedProperty distortionCurve = element.FindPropertyRelative("distortionCurve");
            //hash = hash * 23 + (distortionCurve.boxedValue as AnimationCurve).GetHashCode();
            SerializedProperty distortionRelativeToCenter = element.FindPropertyRelative("distortionRelativeToCenter");
            hash = hash * 23 + distortionRelativeToCenter.boolValue.GetHashCode();

            SerializedProperty fallOff = element.FindPropertyRelative("m_FallOff");
            hash = hash * 23 + fallOff.floatValue.GetHashCode();
            SerializedProperty edgeOffset = element.FindPropertyRelative("m_EdgeOffset");
            hash = hash * 23 + edgeOffset.floatValue.GetHashCode();
            SerializedProperty sdfRoundness = element.FindPropertyRelative("m_SdfRoundness");
            hash = hash * 23 + sdfRoundness.floatValue.GetHashCode();
            SerializedProperty sideCount = element.FindPropertyRelative("m_SideCount");
            hash = hash * 23 + sideCount.intValue.GetHashCode();
            SerializedProperty inverseSDF = element.FindPropertyRelative("inverseSDF");
            hash = hash * 23 + inverseSDF.boolValue.GetHashCode();

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

            Texture2D previewTecture = GetCachedThumbnailProceduralTexture(element, type, index);
            EditorGUI.DrawTextureTransparent(rect, previewTecture, ScaleMode.ScaleToFit, 1f);

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                SerializedProperty isFoldOpened = element.FindPropertyRelative("isFoldOpened");
                isFoldOpened.boolValue = true;
            }

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
            SerializedProperty type = element.FindPropertyRelative("flareType");

            if (DrawElementHeader(headerRect, isFoldOpened, selectedInList: isActive, element))
                DrawFull(contentRect, element, index);
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
            SerializedProperty tintColorType = element.FindPropertyRelative("tintColorType");
            SerializedProperty tint = element.FindPropertyRelative("tint");
            SerializedProperty tintGradient = element.FindPropertyRelative("tintGradient");
            SerializedProperty tintGradientGradient = tintGradient.FindPropertyRelative("m_Gradient");
            SerializedProperty intensity = element.FindPropertyRelative("m_LocalIntensity");
            SerializedProperty allowMultipleElement = element.FindPropertyRelative("allowMultipleElement");
            SerializedProperty count = element.FindPropertyRelative("m_Count");

            Rect thumbnailRect = OffsetForThumbnail(ref summaryRect);
            DrawThumbnailProcedural(thumbnailRect, element, GetEnum<SRPLensFlareType>(type), index);

            bool allowMultipleElementValue;
            if (GetEnum<SRPLensFlareType>(type) == SRPLensFlareType.LensFlareDataSRP ||
                GetEnum<SRPLensFlareType>(type) == SRPLensFlareType.Ring)
                allowMultipleElementValue = false;
            else
                allowMultipleElementValue = allowMultipleElement.boolValue;
            IEnumerator<Rect> fieldRect = ReserveFields(summaryRect, allowMultipleElementValue ? 4 : 3);
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth -= thumbnailRect.width + Styles.horiwontalSpaceBetweenThumbnailAndInspector + Styles.shrinkingLabel;

            fieldRect.MoveNext();
            EditorGUI.PropertyField(fieldRect.Current, type, Styles.type);

            if (GetEnum<SRPLensFlareType>(type) == SRPLensFlareType.LensFlareDataSRP)
            {
                SerializedProperty lensFlareDataSRP = element.FindPropertyRelative("lensFlareDataSRP");
                fieldRect.MoveNext();
                DrawLensFlareDataSRPFieldWithCycleDetection(fieldRect.Current, lensFlareDataSRP, Styles.lensFlareDataSRP);
                EditorGUIUtility.labelWidth = oldLabelWidth;
                return;
            }

            fieldRect.MoveNext();
            if (tintColorType.enumValueIndex == (int)SRPLensFlareColorType.Constant)
                EditorGUI.PropertyField(fieldRect.Current, tint, Styles.tint);
            else
            {
                EditorGUI.BeginChangeCheck();
                Gradient newGradient;
                if (tintColorType.enumValueIndex == (int)SRPLensFlareColorType.RadialGradient)
                    newGradient = EditorGUI.GradientField(fieldRect.Current, Styles.tintRadial, tintGradientGradient.gradientValue);
                else // (tintColorType.enumValueIndex == (int)SRPLensFlareColorType.AngularGradient)
                    newGradient = EditorGUI.GradientField(fieldRect.Current, Styles.tintAngular, tintGradientGradient.gradientValue);
                if (EditorGUI.EndChangeCheck())
                {
                    element.serializedObject.ApplyModifiedProperties();
                    foreach (var target in targets)
                    {
                        LensFlareDataSRP lsSRP = target as LensFlareDataSRP;
                        lsSRP.elements[index].tintGradient.SetKeys(newGradient.colorKeys, newGradient.alphaKeys, newGradient.mode, newGradient.colorSpace);
                        lsSRP.elements[index].tintGradient.SetDirty();
                    }
                }
            }

            fieldRect.MoveNext();
            EditorGUI.PropertyField(fieldRect.Current, intensity, Styles.intensity);

            if (allowMultipleElementValue)
            {
                fieldRect.MoveNext();
                EditorGUI.PropertyField(fieldRect.Current, count, Styles.count);
            }

            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        void DrawFull(Rect remainingRect, SerializedProperty element, int index)
        {
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth -= Styles.shrinkingLabel;

            SerializedProperty type = element.FindPropertyRelative("flareType");
            SRPLensFlareType eType = GetEnum<SRPLensFlareType>(type);
            DrawTypeCathegory(ref remainingRect, element);
            if (eType != SRPLensFlareType.LensFlareDataSRP)
            {
                DrawCutoffCathegory(ref remainingRect, element);
                DrawColorCathegory(ref remainingRect, element, index);
                DrawTransformCathegory(ref remainingRect, element);
                DrawAxisTransformCathegory(ref remainingRect, element);
                DrawRadialDistortionCathegory(ref remainingRect, element);
                if (eType != SRPLensFlareType.Ring)
                    DrawMultipleElementsCathegory(ref remainingRect, element);
            }

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
                case SRPLensFlareType.Ring:
                {
                    SerializedProperty gradient = element.FindPropertyRelative("m_EdgeOffset");
                    SerializedProperty fallOff = element.FindPropertyRelative("m_FallOff");
                    SerializedProperty inverseSDF = element.FindPropertyRelative("inverseSDF");

                    fieldRect.MoveNext();
                    EditorGUI.PropertyField(fieldRect.Current, gradient, Styles.gradient);

                    fieldRect.MoveNext();
                    EditorGUI.PropertyField(fieldRect.Current, fallOff, Styles.fallOff);

                    fieldRect.MoveNext();
                    EditorGUI.PropertyField(fieldRect.Current, inverseSDF, Styles.inverseSDF);

                    if (flareType == SRPLensFlareType.Polygon)
                    {
                        SerializedProperty sideCount = element.FindPropertyRelative("m_SideCount");
                        SerializedProperty sdfRoundness = element.FindPropertyRelative("m_SdfRoundness");

                        fieldRect.MoveNext();
                        EditorGUI.PropertyField(fieldRect.Current, sideCount, Styles.sideCount);

                        fieldRect.MoveNext();
                        EditorGUI.PropertyField(fieldRect.Current, sdfRoundness, Styles.sdfRoundness);
                    }

                    if (flareType == SRPLensFlareType.Ring)
                    {
                        fieldRect.MoveNext();
                        EditorGUI.LabelField(fieldRect.Current, Styles.noiseCathegory, EditorStyles.boldLabel);

                        SerializedProperty noiseAmplitude = element.FindPropertyRelative("noiseAmplitude");
                        SerializedProperty noiseFrequency = element.FindPropertyRelative("noiseFrequency");
                        SerializedProperty noiseSpeed = element.FindPropertyRelative("noiseSpeed");

                        EditorGUI.indentLevel++;
                        fieldRect.MoveNext();
                        EditorGUI.PropertyField(fieldRect.Current, noiseAmplitude, Styles.noiseAmplitude);

                        fieldRect.MoveNext();
                        EditorGUI.PropertyField(fieldRect.Current, noiseFrequency, Styles.noiseFrequency);

                        fieldRect.MoveNext();
                        EditorGUI.PropertyField(fieldRect.Current, noiseSpeed, Styles.noiseSpeed);

                        EditorGUI.indentLevel--;

                        if (flareType == SRPLensFlareType.Ring)
                        {
                            SerializedProperty ringThickness = element.FindPropertyRelative("ringThickness");
                            fieldRect.MoveNext();
                            EditorGUI.PropertyField(fieldRect.Current, ringThickness, Styles.ringThickness);
                        }
                    }
                }
                break;

                case SRPLensFlareType.LensFlareDataSRP:
                    {
                        SerializedProperty lensFlareDataSRP = element.FindPropertyRelative("lensFlareDataSRP");
                        fieldRect.MoveNext();
                        DrawLensFlareDataSRPFieldWithCycleDetection(fieldRect.Current, lensFlareDataSRP, Styles.lensFlareDataSRP);
                    }
                break;
            }

            // update remaining
            fieldRect.MoveNext();
            remainingRect = fieldRect.Current;
        }

        void DrawCutoffCathegory(ref Rect remainingRect, SerializedProperty element)
        {
            SerializedProperty type = element.FindPropertyRelative("flareType");

            IEnumerator<Rect> fieldRect = ReserveCathegory(remainingRect, GetTypeCathegoryLines(type));

            fieldRect.MoveNext();
            EditorGUI.LabelField(fieldRect.Current, Styles.cutoffCathegory, EditorStyles.boldLabel);

            SerializedProperty shapeCutOffSpeed = element.FindPropertyRelative("shapeCutOffSpeed");
            SerializedProperty shapeCutOffRadius = element.FindPropertyRelative("shapeCutOffRadius");

            fieldRect.MoveNext();
            EditorGUI.PropertyField(fieldRect.Current, shapeCutOffSpeed, Styles.shapeCutOffSpeed);

            fieldRect.MoveNext();
            EditorGUI.PropertyField(fieldRect.Current, shapeCutOffRadius, Styles.shapeCutOffRadius);

            // update remaining
            fieldRect.MoveNext();
            remainingRect = fieldRect.Current;
        }

        void DrawColorCathegory(ref Rect remainingRect, SerializedProperty element, int index)
        {
            SerializedProperty tintColorType = element.FindPropertyRelative("tintColorType");
            SerializedProperty tint = element.FindPropertyRelative("tint");
            SerializedProperty tintGradient = element.FindPropertyRelative("tintGradient");
            SerializedProperty tintGradientGradient = tintGradient.FindPropertyRelative("m_Gradient");
            SerializedProperty modulateByLightColor = element.FindPropertyRelative("modulateByLightColor");
            SerializedProperty intensity = element.FindPropertyRelative("m_LocalIntensity");
            SerializedProperty blendMode = element.FindPropertyRelative("blendMode");

            IEnumerator<Rect> fieldRect = ReserveCathegory(remainingRect, GetColorCathegoryLines());

            fieldRect.MoveNext();
            EditorGUI.LabelField(fieldRect.Current, Styles.colorCathegory, EditorStyles.boldLabel);

            fieldRect.MoveNext();
            EditorGUI.PropertyField(fieldRect.Current, tintColorType, Styles.tintColorType);

            fieldRect.MoveNext();
            EditorGUI.indentLevel++;
            if (tintColorType.enumValueIndex == (int)SRPLensFlareColorType.Constant)
            {
                EditorGUI.PropertyField(fieldRect.Current, tint, Styles.tint);
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                Gradient newGradient;
                if (tintColorType.enumValueIndex == (int)SRPLensFlareColorType.RadialGradient)
                    newGradient = EditorGUI.GradientField(fieldRect.Current, Styles.tintRadial, tintGradientGradient.gradientValue);
                else // (tintColorType.enumValueIndex == (int)SRPLensFlareColorType.AngularGradient)
                    newGradient = EditorGUI.GradientField(fieldRect.Current, Styles.tintAngular, tintGradientGradient.gradientValue);
                if (EditorGUI.EndChangeCheck())
                {
                    element.serializedObject.ApplyModifiedProperties();
                    foreach (var target in targets)
                    {
                        LensFlareDataSRP lsSRP = target as LensFlareDataSRP;
                        lsSRP.elements[index].tintGradient.SetKeys(newGradient.colorKeys, newGradient.alphaKeys, newGradient.mode, newGradient.colorSpace);
                        lsSRP.elements[index].tintGradient.SetDirty();
                    }
                }
            }
            EditorGUI.indentLevel--;

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
            SerializedProperty type = element.FindPropertyRelative("flareType");

            IEnumerator<Rect> fieldRect = ReserveCathegory(remainingRect, GetMultipleElementsCathegoryLines(type, allowMultipleElement, distribution));

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
            thumbnailRect.yMin += ((4.0f * EditorGUIUtility.singleLineHeight + 3.0f * EditorGUIUtility.standardVerticalSpacing) - (float)Styles.thumbnailSizeHeight) / 2.0f;
            thumbnailRect.width = Styles.thumbnailSizeWidth;
            thumbnailRect.height = Styles.thumbnailSizeHeight;

            remainingRect.xMin += Styles.thumbnailSizeWidth + Styles.horiwontalSpaceBetweenThumbnailAndInspector;
            return thumbnailRect;
        }

        // enumValueIndex is not the underling int but the index in enum names.
        // if enum don't start at 0, or skip a value, comparing int with enumValueIndex will fail.
        T GetEnum<T>(SerializedProperty property)
            => (T)(object)property.intValue;

        void SetEnum<T>(SerializedProperty property, T value)
            => property.intValue = (int)(object)value;

        void DrawLensFlareDataSRPFieldWithCycleDetection(Rect rect, SerializedProperty lensFlareDataSRPProperty, GUIContent label)
        {
            LensFlareDataSRP currentAsset = target as LensFlareDataSRP;

            EditorGUI.BeginChangeCheck();
            LensFlareDataSRP newValue = EditorGUI.ObjectField(rect, label, lensFlareDataSRPProperty.objectReferenceValue, typeof(LensFlareDataSRP), false) as LensFlareDataSRP;

            if (EditorGUI.EndChangeCheck())
            {
                // Check for cycles before setting the value
                bool wouldCreateCycle = false;

                if (newValue != null && currentAsset != null)
                {
                    // Direct self-reference check
                    if (currentAsset == newValue)
                    {
                        wouldCreateCycle = true;
                    }
                    else
                    {
                        // Multi-level cycle check - see if newValue already references currentAsset
                        HashSet<LensFlareDataSRP> visited = new HashSet<LensFlareDataSRP>();

                        // Recursive function to check if targetAsset is found in asset's dependency chain
                        bool CheckCycle(LensFlareDataSRP asset, LensFlareDataSRP targetAsset)
                        {
                            if (asset == null || visited.Contains(asset))
                                return false;

                            visited.Add(asset);

                            foreach (var element in asset.elements)
                            {
                                if (element.flareType == SRPLensFlareType.LensFlareDataSRP && element.lensFlareDataSRP != null)
                                {
                                    if (element.lensFlareDataSRP == targetAsset || CheckCycle(element.lensFlareDataSRP, targetAsset))
                                        return true;
                                }
                            }
                            return false;
                        }

                        wouldCreateCycle = CheckCycle(newValue, currentAsset);
                    }
                }

                if (wouldCreateCycle)
                {
                    // Cycle detected - set to null and show a warning
                    lensFlareDataSRPProperty.objectReferenceValue = null;
                    Debug.LogWarning($"Cannot assign lens flare asset '{newValue.name}' because it would create a cyclic dependency. Setting to null to prevent infinite loop.");
                }
                else
                {
                    lensFlareDataSRPProperty.objectReferenceValue = newValue;
                }
            }
        }

        #endregion
    }
}
