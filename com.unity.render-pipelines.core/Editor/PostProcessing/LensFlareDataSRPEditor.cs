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
            static public readonly GUIContent colorGradient = EditorGUIUtility.TrTextContent("Color Gradient", "Specifies the gradient applied across all the elements.");
            static public readonly GUIContent positionVariation = EditorGUIUtility.TrTextContent("Position Variation", "Sets the offset applied to the current position of the element.");
            static public readonly GUIContent rotationVariation = EditorGUIUtility.TrTextContent("Rotation Variation", "Sets the offset applied to the current element rotation.");
            static public readonly GUIContent scaleVariation = EditorGUIUtility.TrTextContent("Scale Variation", "Sets the offset applied to the current scale of the element.");
            static public readonly GUIContent positionCurve = EditorGUIUtility.TrTextContent("Position Variation", "Defines how the multiple elements are placed along the spread using a curve.");
            static public readonly GUIContent scaleCurve = EditorGUIUtility.TrTextContent("Scale", "Defines how the multiple elements are scaled along the spread.");

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
        Rect? reservedListSizeRect;

        void OnEnable()
        {
            m_Elements = serializedObject.FindProperty("elements");

            m_List = new ReorderableList(serializedObject, m_Elements, true, true, true, true);
            m_List.drawHeaderCallback = DrawListHeader;
            m_List.drawFooterCallback = DrawListFooter;
            m_List.onAddCallback = OnAdd;
            m_List.drawElementBackgroundCallback = DrawElementBackground;
            m_List.drawElementCallback = DrawElement;
            m_List.elementHeightCallback = ElementHeight;
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

            // Set Default values
            (target as LensFlareDataSRP).elements[newIndex] = new LensFlareDataElementSRP();
            serializedObject.Update();
        }

        #region Header and Footer
        void DrawListHeader(Rect rect)
        {
            Rect sizeRect = rect;
            sizeRect.x += Styles.sizeOffset;
            sizeRect.xMin = sizeRect.xMax - Styles.sizeWidth;

            // If we draw the size now, and the user decrease it,
            // it can lead to out of range issue. See Footer.
            reservedListSizeRect = sizeRect;

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
            if (!reservedListSizeRect.HasValue)
                return;

            DrawListSize(reservedListSizeRect.Value);
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

            int line = 5;   //[Enable], Count, Distribution, Length Spread, Color Gradient
            switch (GetEnum<SRPLensFlareDistribution>(distribution))
            {
                case SRPLensFlareDistribution.Curve:
                    line += 2;
                    break;  //Position Variation, Scale
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
                DrawSummary(contentRect, element);

            EditorGUIUtility.wideMode = oldWideMode;
        }

        bool DrawElementHeader(Rect headerRect, SerializedProperty isFoldOpened, bool selectedInList, SerializedProperty element)
        {
            Rect labelRect = headerRect;
            labelRect.xMin += 16;
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

            return newState;
        }

        void DrawSummary(Rect summaryRect, SerializedProperty element)
        {
            SerializedProperty type = element.FindPropertyRelative("flareType");
            SerializedProperty tint = element.FindPropertyRelative("tint");
            SerializedProperty intensity = element.FindPropertyRelative("m_LocalIntensity");
            SerializedProperty allowMultipleElement = element.FindPropertyRelative("allowMultipleElement");
            SerializedProperty count = element.FindPropertyRelative("m_Count");

            Rect thumbnailRect = OffsetForThumbnail(ref summaryRect);
            Rect thumbnailIconeRect = thumbnailRect;
            thumbnailIconeRect.xMin += Styles.iconMargin;
            thumbnailIconeRect.xMax -= Styles.iconMargin;
            thumbnailIconeRect.yMin += Styles.iconMargin;
            thumbnailIconeRect.yMax -= Styles.iconMargin;
            Color guiColor = GUI.color;
            GUI.color = Color.black; //set background color for thunmbnail
            switch (GetEnum<SRPLensFlareType>(type))
            {
                case SRPLensFlareType.Image:
                    SerializedProperty flareTexture = element.FindPropertyRelative("lensFlareTexture");
                    SerializedProperty preserveAspectRatio = element.FindPropertyRelative("preserveAspectRatio");
                    SerializedProperty sizeXY = element.FindPropertyRelative("sizeXY");
                    float aspectRatio = ((flareTexture.objectReferenceValue is Texture texture) && preserveAspectRatio.boolValue)
                        ? texture.width / (float)texture.height
                        : sizeXY.vector2Value.x / Mathf.Max(sizeXY.vector2Value.y, 1e-6f);
                    EditorGUI.DrawTextureTransparent(thumbnailRect, flareTexture.objectReferenceValue as Texture, ScaleMode.ScaleToFit, aspectRatio);
                    break;

                case SRPLensFlareType.Circle:
                    EditorGUI.DrawRect(thumbnailRect, GUI.color);   //draw the margin
                    EditorGUI.DrawTextureTransparent(thumbnailIconeRect, LensFlareEditorUtils.Icons.circle, ScaleMode.ScaleToFit, 1);
                    break;

                case SRPLensFlareType.Polygon:
                    EditorGUI.DrawRect(thumbnailRect, GUI.color);   //draw the margin
                    EditorGUI.DrawTextureTransparent(thumbnailIconeRect, LensFlareEditorUtils.Icons.polygon, ScaleMode.ScaleToFit, 1);
                    break;
            }
            GUI.color = guiColor;

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
                        fieldRect.MoveNext();
                        EditorGUI.PropertyField(fieldRect.Current, colorGradient, Styles.colorGradient);
                        break;

                    case SRPLensFlareDistribution.Curve:
                        SerializedProperty positionCurve = element.FindPropertyRelative("positionCurve");
                        SerializedProperty scaleCurve = element.FindPropertyRelative("scaleCurve");

                        fieldRect.MoveNext();
                        EditorGUI.PropertyField(fieldRect.Current, colorGradient, Styles.colorGradient);

                        fieldRect.MoveNext();
                        EditorGUI.PropertyField(fieldRect.Current, positionCurve, Styles.positionCurve);

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
