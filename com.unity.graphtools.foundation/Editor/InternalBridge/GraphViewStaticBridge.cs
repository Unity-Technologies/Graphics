using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AssetImporters;
#if UNITY_2022_2_OR_NEWER
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
#endif
using UnityEditor.UIElements;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.UIR;
using Cursor = UnityEngine.UIElements.Cursor;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Bridge
{
    static class GraphViewStaticBridge
    {
        public static class EventCommandNames
        {
            public const string Cut = UnityEngine.EventCommandNames.Cut;
            public const string Copy = UnityEngine.EventCommandNames.Copy;
            public const string Paste = UnityEngine.EventCommandNames.Paste;
            public const string Duplicate = UnityEngine.EventCommandNames.Duplicate;
            public const string Delete = UnityEngine.EventCommandNames.Delete;
            public const string SoftDelete = UnityEngine.EventCommandNames.SoftDelete;
            public const string FrameSelected = UnityEngine.EventCommandNames.FrameSelected;
            public const string SelectAll = UnityEngine.EventCommandNames.SelectAll;
            public const string DeselectAll = UnityEngine.EventCommandNames.DeselectAll;
            public const string InvertSelection = UnityEngine.EventCommandNames.InvertSelection;
        }

        public static readonly int editorPixelsPerPointId = Shader.PropertyToID("_EditorPixelsPerPoint");
        public static readonly int graphViewScaleId = Shader.PropertyToID("_GraphViewScale");

        static Shader s_GraphViewShader;

        public static Color EditorPlayModeTint => UIElementsUtility.editorPlayModeTintColor;

        public static void ShowColorPicker(Action<Color> callback, Color initialColor, bool withAlpha)
        {
            ColorPicker.Show(callback, initialColor, withAlpha);
        }

        public static float RoundToPixelGrid(float v)
        {
            return GUIUtility.RoundToPixelGrid(v);
        }

        public static Vector2 RoundToPixelGrid(Vector2 v)
        {
            return new Vector2(GUIUtility.RoundToPixelGrid(v.x), GUIUtility.RoundToPixelGrid(v.y));
        }

        public static Rect RoundToPixelGrid(Rect r)
        {
            var min = RoundToPixelGrid(r.min);
            var max = RoundToPixelGrid(r.max);
            return new Rect(min, max - min);
        }

        public static float PixelPerPoint => GUIUtility.pixelsPerPoint;

        public static void ApplyWireMaterial()
        {
            HandleUtility.ApplyWireMaterial();
        }

        /* For tests */
        public static void SetDisableInputEvents(this EditorWindow window, bool value)
        {
            window.disableInputEvents = value;
        }

        /* For tests */
        public static void ClearPersistentViewData(this EditorWindow window)
        {
            window.ClearPersistentViewData();
        }

        /* For tests */
        public static void DisableViewDataPersistence(this EditorWindow window)
        {
            window.DisableViewDataPersistence();
        }

        /* For tests */
        public static void SetTimeSinceStartupCallback(Func<long> cb)
        {
            if (cb == null)
                Panel.TimeSinceStartup = null;
            else
                Panel.TimeSinceStartup = () => cb();
        }

        /* For tests */
        public static void UpdateScheduledEvents(this VisualElement ve)
        {
            var scheduler = (TimerEventScheduler)((BaseVisualElementPanel)ve.panel).scheduler;
            scheduler.UpdateScheduledEvents();
        }

        public static List<EditorWindow> ShowGraphViewWindowWithTools(Type blackboardType, Type minimapType, Type graphViewType)
        {
            const float width = 1200;
            const float height = 800;

            const float toolsWidth = 200;

            var mainSplitView = ScriptableObject.CreateInstance<SplitView>();

            var sideSplitView = ScriptableObject.CreateInstance<SplitView>();
            sideSplitView.vertical = true;
            sideSplitView.position = new Rect(0, 0, toolsWidth, height);
            var dockArea = ScriptableObject.CreateInstance<DockArea>();
            dockArea.position = new Rect(0, 0, toolsWidth, height - toolsWidth);
            var blackboardWindow = ScriptableObject.CreateInstance(blackboardType) as EditorWindow;
            dockArea.AddTab(blackboardWindow);
            sideSplitView.AddChild(dockArea);

            dockArea = ScriptableObject.CreateInstance<DockArea>();
            dockArea.position = new Rect(0, 0, toolsWidth, toolsWidth);
            var minimapWindow = ScriptableObject.CreateInstance(minimapType) as EditorWindow;
            dockArea.AddTab(minimapWindow);
            sideSplitView.AddChild(dockArea);

            mainSplitView.AddChild(sideSplitView);
            dockArea = ScriptableObject.CreateInstance<DockArea>();
            var graphViewWindow = ScriptableObject.CreateInstance(graphViewType) as EditorWindow;
            dockArea.AddTab(graphViewWindow);
            dockArea.position = new Rect(0, 0, width - toolsWidth, height);
            mainSplitView.AddChild(dockArea);

            var containerWindow = ScriptableObject.CreateInstance<ContainerWindow>();
            containerWindow.m_DontSaveToLayout = false;
            containerWindow.position = new Rect(100, 100, width, height);
            containerWindow.rootView = mainSplitView;
            containerWindow.rootView.position = new Rect(0, 0, mainSplitView.position.width, mainSplitView.position.height);

            containerWindow.Show(ShowMode.NormalWindow, false, true, setFocus: true);

            return new List<EditorWindow> { graphViewWindow, blackboardWindow, minimapWindow };
        }

        public static IEnumerable<T> GetGraphViewWindows<T>(Type typeFilter) where T : EditorWindow
        {
            var guiViews = new List<GUIView>();
            GUIViewDebuggerHelper.GetViews(guiViews);

            // Get all GraphViews used by existing tool windows of our type
            using (var it = UIElementsUtility.GetPanelsIterator())
            {
                while (it.MoveNext())
                {
                    var dockArea = guiViews.FirstOrDefault(v => v.GetInstanceID() == it.Current.Key) as DockArea;
                    if (dockArea == null)
                        continue;

                    if (typeFilter == null)
                    {
                        foreach (var window in dockArea.m_Panes.OfType<T>())
                        {
                            yield return window;
                        }
                    }
                    else
                    {
                        foreach (var window in dockArea.m_Panes.Where(p => p.GetType() == typeFilter).Cast<T>())
                        {
                            yield return window;
                        }
                    }
                }
            }
        }

#if !UNITY_2022_2_OR_NEWER
        // Do not use this function in new code. It is here to support old code.
        // Set element dimensions using styles, with position: absolute.
        // When removing this function, also remove IsLayoutManual above, as it will always return false.
        public static void SetLayout(this VisualElement ve, Rect layout)
        {
            ve.layout = layout;
        }
#endif

        public static void SetCheckedPseudoState(this VisualElement ve, bool set)
        {
            if (set)
            {
                ve.pseudoStates |= PseudoStates.Checked;
            }
            else
            {
                ve.pseudoStates &= ~PseudoStates.Checked;
            }
        }

        public static void SetDisabledPseudoState(this VisualElement ve, bool set)
        {
            if (set)
            {
                ve.pseudoStates |= PseudoStates.Disabled;
            }
            else
            {
                ve.pseudoStates &= ~PseudoStates.Disabled;
            }
        }

        public static bool GetDisabledPseudoState(this VisualElement ve)
        {
            return (ve.pseudoStates & PseudoStates.Disabled) == PseudoStates.Disabled;
        }

        public static void ResetPositionProperties(this VisualElement ve)
        {
            ve.ResetPositionProperties();
        }

        public static Matrix4x4 WorldTransformInverse(this VisualElement ve)
        {
            return ve.worldTransformInverse;
        }

        public static void DrawImmediate(MeshGenerationContext mgc, Action callback)
        {
            mgc.painter.DrawImmediate(callback, true);
        }

        public static void SolidRectangle(MeshGenerationContext mgc, Rect rectParams, Color color, ContextType context)
        {
            mgc.Rectangle(MeshGenerationContextUtils.RectangleParams.MakeSolid(rectParams, color, context));
        }

        public static void Border(MeshGenerationContext mgc, Rect rectParams, Color color, ContextType context)
        {
            Border(mgc, rectParams, color, 1, Vector2.zero, context);
        }

        public static void Border(MeshGenerationContext mgc, Rect rectParams, Color[] colors, float borderWidth, Vector2[] radii, ContextType context)
        {
            var borderParams = new MeshGenerationContextUtils.BorderParams
            {
                rect = rectParams,
                playmodeTintColor = context == ContextType.Editor
                    ? UIElementsUtility.editorPlayModeTintColor
                    : Color.white,
                bottomColor = colors[0],
                topColor = colors[1],
                leftColor = colors[2],
                rightColor = colors[3],
                leftWidth = borderWidth,
                rightWidth = borderWidth,
                topWidth = borderWidth,
                bottomWidth = borderWidth,
                topLeftRadius = radii[0],
                topRightRadius = radii[1],
                bottomRightRadius = radii[2],
                bottomLeftRadius = radii[3]
            };
            mgc.Border(borderParams);
        }

        public static void Border(MeshGenerationContext mgc, Rect rectParams, Color color,float borderWidth,Vector2 radius, ContextType context)
        {
            var borderParams = new MeshGenerationContextUtils.BorderParams
            {
                rect = rectParams,
                playmodeTintColor = context == ContextType.Editor
                    ? UIElementsUtility.editorPlayModeTintColor
                    : Color.white,
                bottomColor = color,
                topColor = color,
                leftColor = color,
                rightColor = color,
                leftWidth = borderWidth,
                rightWidth = borderWidth,
                topWidth = borderWidth,
                bottomWidth = borderWidth,
                topLeftRadius = radius,
                topRightRadius = radius,
                bottomRightRadius = radius,
                bottomLeftRadius = radius
            };
            mgc.Border(borderParams);
        }

        public static MeshWriteData AllocateMeshWriteData(MeshGenerationContext mgc, int vertexCount, int indexCount)
        {
            return mgc.Allocate(vertexCount, indexCount, null, null, MeshGenerationContext.MeshFlags.UVisDisplacement);
        }

        public static void SetNextVertex(this MeshWriteData md, Vector3 pos, Vector2 uv, Color32 tint)
        {
#if UNITY_2021_1_OR_NEWER
            Color32 ids = new Color32(0, 0, 0, 0);
            Color32 flags = new Color32((byte)VertexFlags.IsGraphViewEdge, 0, 0, 0);
            md.SetNextVertex(new Vertex() { position = pos, uv = uv, tint = tint, ids = ids, flags = flags });
#else
            Color32 flags = new Color32(0, 0, 0, (byte)VertexFlags.LastType);
            md.SetNextVertex(new Vertex() { position = pos, uv = uv, tint = tint, idsFlags = flags });
#endif
        }

        public static Vector2 DoMeasure(this VisualElement ve, float desiredWidth, VisualElement.MeasureMode widthMode, float desiredHeight, VisualElement.MeasureMode heightMode)
        {
            return ve.DoMeasure(desiredWidth, widthMode, desiredHeight, heightMode);
        }

#if !UNITY_2022_2_OR_NEWER
        public static StyleLength GetComputedStyleWidth(this VisualElement ve)
        {
            return ve.computedStyle.width;
        }
#endif

        public static void SetRenderHintsForGraphView(this VisualElement ve)
        {
            ve.renderHints = RenderHints.ClipWithScissors;
        }

        public static T MandatoryQ<T>(this VisualElement e, string name = null, string className = null) where T : VisualElement
        {
            return UQueryExtensions.MandatoryQ<T>(e, name, className);
        }

        public static VisualElement MandatoryQ(this VisualElement e, string name = null, string className = null)
        {
            return UQueryExtensions.MandatoryQ(e, name, className);
        }

        internal static void SetIsCompositeRoot(this VisualElement ve)
        {
            ve.isCompositeRoot = true;
        }

        internal static void ChangeMouseCursorTo(this VisualElement ve, int internalCursorId)
        {
            var cursor = new Cursor();
            cursor.defaultCursorId = internalCursorId;

            ve.elementPanel.cursorManager.SetCursor(cursor);
        }

        public static uint GetControlId(this VisualElement self) => self.controlid;

        public static Vector2 GetMousePosition()
        {
#if UNITY_2022_1_OR_NEWER
            return PointerDeviceState.GetPointerPosition(PointerId.mousePointerId, ContextType.Editor);
#else
            return PointerDeviceState.GetPointerPosition(PointerId.mousePointerId);
#endif
        }

        public static void SetEventPropagationToNormal(this EventBase e)
        {
            e.propagation = EventBase.EventPropagation.TricklesDown | EventBase.EventPropagation.Bubbles | EventBase.EventPropagation.Cancellable;
        }

        public static void SetUpRender(this VisualElement self, Action<Material> onUpdateMaterial)
        {
            if (self.panel is BaseVisualElementPanel p)
            {
                if (s_GraphViewShader == null)
                    s_GraphViewShader = Shader.Find(Shaders.k_GraphView);

                p.standardShader = s_GraphViewShader;
                HostView ownerView = p.ownerObject as HostView;
                if (ownerView != null && ownerView.actualView != null)
                    ownerView.actualView.antiAliasing = 4;

                p.updateMaterial += onUpdateMaterial;
            }

            // Force DefaultCommonDark.uss since GraphView only has a dark style at the moment
            ForceDarkStyleSheet(self);
        }

        // VLadN: Use our own ForceDarkStyleSheet as UIToolkit version also affects parent elements
        // (UIElementsEditorUtility.ForceDarkStyleSheet)
        // This makes it that panels outside of graphview like node inspector and toolbar are also forced in dark incorrectly
        static void ForceDarkStyleSheet(VisualElement ele)
        {
            if (EditorGUIUtility.isProSkin)
                return;
#if UNITY_2020_3_OR_NEWER
            StyleSheet commonLightStyleSheet = UIElementsEditorUtility.GetCommonLightStyleSheet();
            StyleSheet commonDarkStyleSheet = UIElementsEditorUtility.GetCommonDarkStyleSheet();
#else
            StyleSheet commonLightStyleSheet = UIElementsEditorUtility.s_DefaultCommonLightStyleSheet;
            StyleSheet commonDarkStyleSheet = UIElementsEditorUtility.s_DefaultCommonDarkStyleSheet;
#endif
            VisualElement visualElement = ele;
            {
                VisualElementStyleSheetSet styleSheets = visualElement.styleSheets;
                if (styleSheets.Contains(commonLightStyleSheet))
                {
                    styleSheets = visualElement.styleSheets;
                    styleSheets.Swap(commonLightStyleSheet, commonDarkStyleSheet);
                }
            }
        }

        public static void TearDownRender(this VisualElement self, Action<Material> onUpdateMaterial)
        {
            if (self.panel is BaseVisualElementPanel p)
            {
                p.updateMaterial -= onUpdateMaterial;
            }
        }

#if UNITY_2022_2_OR_NEWER
        public static VisualElement CreateEditorToolbar(IEnumerable<string> toolbarElements, EditorWindow containerWindow)
        {
            return new EditorToolbar(toolbarElements, containerWindow).rootVisualElement;
        }

        public static IEnumerable<Overlay> GetAllOverlays(this EditorWindow window)
        {
            return window.overlayCanvas.overlays;
        }

        public static void RebuildOverlays(EditorWindow window)
        {
            foreach (var overlay in window.overlayCanvas.overlays)
            {
                overlay.RebuildContent();
            }
        }

        public static VisualElement GetOverlayRoot(Overlay overlay)
        {
            return overlay.rootVisualElement;
        }
#endif

        public static void ImportStyleSheet(AssetImportContext ctx, StyleSheet asset, string contents)
        {
            var importer = new StyleSheetImporterImpl(ctx);
            importer.Import(asset, contents);
        }
    }

    public abstract class GraphViewToolWindowBridge : EditorWindow
    {
        public abstract void SelectGraphViewFromWindow(EditorWindow window, VisualElement graphView, int graphViewIndexInWindow = 0);
    }
}
