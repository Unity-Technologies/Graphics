using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using RenderPipelineManager = UnityEngine.Rendering.RenderPipelineManager;

namespace UnityEditor.Rendering.LookDev
{
    /// <summary>Interface that must implement the viewer to communicate with the compositor and data management</summary>
    public interface IViewDisplayer
    {
        /// <summary>Get the displayed rect to use</summary>
        /// <param name="index">Index of this view</param>
        /// <returns>The Rect to draw</returns>
        Rect GetRect(ViewCompositionIndex index);
        /// <summary>Set the computed texture in the view</summary>
        /// <param name="index">Index of this view</param>
        /// <param name="texture">The texture used</param>
        void SetTexture(ViewCompositionIndex index, Texture texture);

        /// <summary>Repaint the UI</summary>
        void Repaint();

        /// <summary>Callback on layout changed</summary>
        event Action<Layout, SidePanel> OnLayoutChanged;

        /// <summary>Callback on RenderDoc acquisition is triggered</summary>
        event Action OnRenderDocAcquisitionTriggered;

        /// <summary>Callback on ;ouse events in the view</summary>
        event Action<IMouseEvent> OnMouseEventInView;

        /// <summary>Callback on object changed in the view</summary>
        event Action<GameObject, ViewCompositionIndex, Vector2> OnChangingObjectInView;
        /// <summary>Callback on environment changed in the view</summary>
        event Action<UnityEngine.Object, ViewCompositionIndex, Vector2> OnChangingEnvironmentInView;

        /// <summary>Callback on closed</summary>
        event Action OnClosed;

        /// <summary>Callback on update requested</summary>
        event Action OnUpdateRequested;
    }

    partial class DisplayWindow : EditorWindow, IViewDisplayer
    {
        static partial class Style
        {
            internal const string k_IconFolder = @"Packages/com.unity.render-pipelines.core/Editor/LookDev/Icons/";
            internal const string k_uss = @"Packages/com.unity.render-pipelines.core/Editor/LookDev/DisplayWindow.uss";
            internal const string k_uss_personal_overload = @"Packages/com.unity.render-pipelines.core/Editor/LookDev/DisplayWindow-PersonalSkin.uss";

            internal static readonly GUIContent k_WindowTitleAndIcon = EditorGUIUtility.TrTextContentWithIcon("Look Dev", CoreEditorUtils.LoadIcon(k_IconFolder, "LookDev", forceLowRes: true));

            internal static readonly (Texture2D icon, string tooltip) k_Layout1Icon =
                (CoreEditorUtils.LoadIcon(Style.k_IconFolder, "Layout1", forceLowRes: true),
                "First view");
            internal static readonly (Texture2D icon, string tooltip) k_Layout2Icon =
                (CoreEditorUtils.LoadIcon(Style.k_IconFolder, "Layout2", forceLowRes: true),
                "Second view");
            internal static readonly (Texture2D icon, string tooltip) k_LayoutVerticalIcon =
                (CoreEditorUtils.LoadIcon(Style.k_IconFolder, "LayoutVertical", forceLowRes: true),
                "Both views split vertically");
            internal static readonly (Texture2D icon, string tooltip) k_LayoutHorizontalIcon =
                (CoreEditorUtils.LoadIcon(Style.k_IconFolder, "LayoutHorizontal", forceLowRes: true),
                "Both views split horizontally");
            internal static readonly (Texture2D icon, string tooltip) k_LayoutStackIcon =
                (CoreEditorUtils.LoadIcon(Style.k_IconFolder, "LayoutCustom", forceLowRes: true),
                "Both views stacked");

            internal static readonly Texture2D k_Camera1Icon = CoreEditorUtils.LoadIcon(Style.k_IconFolder, "Camera1", forceLowRes: true);
            internal static readonly Texture2D k_Camera2Icon = CoreEditorUtils.LoadIcon(Style.k_IconFolder, "Camera2", forceLowRes: true);
            internal static readonly Texture2D k_LinkIcon = CoreEditorUtils.LoadIcon(Style.k_IconFolder, "Link", forceLowRes: true);
            internal static readonly Texture2D k_RightIcon = CoreEditorUtils.LoadIcon(Style.k_IconFolder, "RightArrow", forceLowRes: true);
            internal static readonly Texture2D k_LeftIcon = CoreEditorUtils.LoadIcon(Style.k_IconFolder, "LeftArrow", forceLowRes: true);

            internal static readonly Texture2D k_RenderdocIcon = CoreEditorUtils.LoadIcon(Style.k_IconFolder, "RenderDoc", forceLowRes: true);
            internal const string k_RenderDocLabel = " Content";

            internal const string k_CameraSyncTooltip = "Synchronize camera movement amongst views";
            internal const string k_CameraMenuSync1On2 = "Align Camera 1 with Camera 2";
            internal const string k_CameraMenuSync2On1 = "Align Camera 2 with Camera 1";
            internal const string k_CameraMenuReset = "Reset Cameras";

            internal const string k_EnvironmentSidePanelName = "Environment";
            internal const string k_DebugSidePanelName = "Debug";

            internal const string k_DragAndDropObject = "Drag and drop object here";
            internal const string k_DragAndDropEnvironment = "Drag and drop environment from side panel here";

            // /!\ WARNING:
            //The following const are used in the uss.
            //If you change them, update the uss file too.
            internal const string k_MainContainerName = "mainContainer";
            internal const string k_ViewContainerName = "viewContainer";
            internal const string k_FirstViewName = "firstView";
            internal const string k_SecondViewName = "secondView";
            internal const string k_ToolbarName = "toolbar";
            internal const string k_ToolbarRadioName = "toolbarRadio";
            internal const string k_TabsRadioName = "tabsRadio";
            internal const string k_SideToolbarName = "sideToolbar";
            internal const string k_SharedContainerClass = "container";
            internal const string k_FirstViewClass = "firstView";
            internal const string k_SecondViewsClass = "secondView";
            internal const string k_VerticalViewsClass = "verticalSplit";
            internal const string k_DebugContainerName = "debugContainer";
            internal const string k_ShowDebugPanelClass = "showDebugPanel";

            internal const string k_EnvironmentContainerName = "environmentContainer";
            internal const string k_ShowEnvironmentPanelClass = "showEnvironmentPanel";

            internal const string k_CameraMenuName = "cameraMenu";
            internal const string k_CameraButtonName = "cameraButton";
            internal const string k_CameraSeparatorName = "cameraSeparator";

            internal const string k_RenderDocContentName = "renderdoc-content";
        }

        VisualElement m_MainContainer;
        VisualElement m_ViewContainer;
        VisualElement m_DebugContainer;
        Label m_NoEnvironmentList;
        Label m_NoObject1;
        Label m_NoEnvironment1;
        Label m_NoObject2;
        Label m_NoEnvironment2;

        Image[] m_Views = new Image[2];

        LayoutContext layout
            => LookDev.currentContext.layout;

        Layout viewLayout
        {
            get => layout.viewLayout;
            set
            {
                if (layout.viewLayout != value)
                {
                    OnLayoutChangedInternal?.Invoke(value, sidePanel);
                    ApplyLayout(value);
                }
            }
        }

        SidePanel sidePanel
        {
            get => layout.showedSidePanel;
            set
            {
                if (layout.showedSidePanel != value)
                {
                    OnLayoutChangedInternal?.Invoke(viewLayout, value);
                    ApplySidePanelChange(layout.showedSidePanel);
                }
            }
        }

        event Action<Layout, SidePanel> OnLayoutChangedInternal;
        event Action<Layout, SidePanel> IViewDisplayer.OnLayoutChanged
        {
            add => OnLayoutChangedInternal += value;
            remove => OnLayoutChangedInternal -= value;
        }

        event Action OnRenderDocAcquisitionTriggeredInternal;
        event Action IViewDisplayer.OnRenderDocAcquisitionTriggered
        {
            add => OnRenderDocAcquisitionTriggeredInternal += value;
            remove => OnRenderDocAcquisitionTriggeredInternal -= value;
        }

        event Action<IMouseEvent> OnMouseEventInViewPortInternal;
        event Action<IMouseEvent> IViewDisplayer.OnMouseEventInView
        {
            add => OnMouseEventInViewPortInternal += value;
            remove => OnMouseEventInViewPortInternal -= value;
        }

        event Action<GameObject, ViewCompositionIndex, Vector2> OnChangingObjectInViewInternal;
        event Action<GameObject, ViewCompositionIndex, Vector2> IViewDisplayer.OnChangingObjectInView
        {
            add => OnChangingObjectInViewInternal += value;
            remove => OnChangingObjectInViewInternal -= value;
        }

        //event Action<Material, ViewCompositionIndex, Vector2> OnChangingMaterialInViewInternal;
        //event Action<Material, ViewCompositionIndex, Vector2> IViewDisplayer.OnChangingMaterialInView
        //{
        //    add => OnChangingMaterialInViewInternal += value;
        //    remove => OnChangingMaterialInViewInternal -= value;
        //}

        event Action<UnityEngine.Object, ViewCompositionIndex, Vector2> OnChangingEnvironmentInViewInternal;
        event Action<UnityEngine.Object, ViewCompositionIndex, Vector2> IViewDisplayer.OnChangingEnvironmentInView
        {
            add => OnChangingEnvironmentInViewInternal += value;
            remove => OnChangingEnvironmentInViewInternal -= value;
        }

        event Action OnClosedInternal;
        event Action IViewDisplayer.OnClosed
        {
            add => OnClosedInternal += value;
            remove => OnClosedInternal -= value;
        }

        event Action OnUpdateRequestedInternal;
        event Action IViewDisplayer.OnUpdateRequested
        {
            add => OnUpdateRequestedInternal += value;
            remove => OnUpdateRequestedInternal -= value;
        }

        StyleSheet styleSheet = null;
        StyleSheet styleSheetLight = null;

        void ReloadStyleSheets()
        {
            if(styleSheet == null || styleSheet.Equals(null))
            {
                styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(Style.k_uss);
                if(styleSheet == null || styleSheet.Equals(null))
                {
                    //Debug.LogWarning("[LookDev] Could not load Stylesheet.");
                    return;
                }
            }

            if(!rootVisualElement.styleSheets.Contains(styleSheet))
                rootVisualElement.styleSheets.Add(styleSheet);

            //Additively load Light Skin
            if(!EditorGUIUtility.isProSkin)
            {
                if(styleSheetLight == null || styleSheetLight.Equals(null))
                {
                    styleSheetLight = AssetDatabase.LoadAssetAtPath<StyleSheet>(Style.k_uss_personal_overload);
                    if(styleSheetLight == null || styleSheetLight.Equals(null))
                    {
                        //Debug.LogWarning("[LookDev] Could not load Light skin.");
                        return;
                    }
                }
                 
                if(!rootVisualElement.styleSheets.Contains(styleSheetLight))
                    rootVisualElement.styleSheets.Add(styleSheetLight);
            }
        }

        void OnEnable()
        {
            //Stylesheet
            // Try to load stylesheet. Timing can be odd while upgrading packages (case 1219692).
            // In this case, it will be fixed in OnGUI. Though it can spawn error while reimporting assets.
            // Waiting for filter on stylesheet (case 1228706) to remove last error.
            // On Editor Skin change, OnEnable is called and stylesheets need to be reloaded (case 1278802).
            if(EditorApplication.isUpdating)
                ReloadStyleSheets();

            //Call the open function to configure LookDev
            // in case the window where open when last editor session finished.
            // (Else it will open at start and has nothing to display).
            if (!LookDev.open)
                LookDev.Open();

            titleContent = Style.k_WindowTitleAndIcon;

            // /!\ be sure to have a minSize that will allow a non negative sized viewport even with side panel open
            this.minSize = new Vector2(600, 400);

            CreateToolbar();

            m_MainContainer = new VisualElement() { name = Style.k_MainContainerName };
            m_MainContainer.AddToClassList(Style.k_SharedContainerClass);
            rootVisualElement.Add(m_MainContainer);

            CreateViews();
            CreateEnvironment();
            CreateDebug();
            CreateDropAreas();

            ApplyLayout(viewLayout);
            ApplySidePanelChange(layout.showedSidePanel);

            Undo.undoRedoPerformed += FullRefreshEnvironmentList;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= FullRefreshEnvironmentList;
            OnClosedInternal?.Invoke();
        }

        void CreateToolbar()
        {
            // Layout swapper part
            var layoutRadio = new ToolbarRadio() { name = Style.k_ToolbarRadioName };
            layoutRadio.AddRadios(new[] {
                Style.k_Layout1Icon,
                Style.k_Layout2Icon,
                Style.k_LayoutVerticalIcon,
                Style.k_LayoutHorizontalIcon,
                Style.k_LayoutStackIcon,
                });
            layoutRadio.RegisterCallback((ChangeEvent<int> evt)
                => viewLayout = (Layout)evt.newValue);
            layoutRadio.SetValueWithoutNotify((int)viewLayout);

            var cameraMenu = new ToolbarMenu() { name = Style.k_CameraMenuName };
            cameraMenu.variant = ToolbarMenu.Variant.Popup;
            var cameraToggle = new ToolbarToggle() { name = Style.k_CameraButtonName };
            cameraToggle.value = LookDev.currentContext.cameraSynced;
            cameraToggle.tooltip = Style.k_CameraSyncTooltip;

            //Note: when having Image on top of the Toggle nested in the Menu, RegisterValueChangedCallback is not called
            //cameraToggle.RegisterValueChangedCallback(evt => LookDev.currentContext.cameraSynced = evt.newValue);
            cameraToggle.RegisterCallback<MouseUpEvent>(evt =>
            {
                LookDev.currentContext.cameraSynced ^= true;
                cameraToggle.SetValueWithoutNotify(LookDev.currentContext.cameraSynced);
            });

            var cameraSeparator = new ToolbarToggle() { name = Style.k_CameraSeparatorName };
            cameraToggle.Add(new Image() { image = Style.k_Camera1Icon });
            cameraToggle.Add(new Image() { image = Style.k_LinkIcon });
            cameraToggle.Add(new Image() { image = Style.k_Camera2Icon });
            cameraMenu.Add(cameraSeparator);
            cameraMenu.Add(cameraToggle);
            cameraMenu.menu.AppendAction(Style.k_CameraMenuSync1On2,
                (DropdownMenuAction a) => LookDev.currentContext.SynchronizeCameraStates(ViewIndex.Second),
                DropdownMenuAction.AlwaysEnabled);
            cameraMenu.menu.AppendAction(Style.k_CameraMenuSync2On1,
                (DropdownMenuAction a) => LookDev.currentContext.SynchronizeCameraStates(ViewIndex.First),
                DropdownMenuAction.AlwaysEnabled);
            cameraMenu.menu.AppendAction(Style.k_CameraMenuReset,
                (DropdownMenuAction a) =>
                {
                    LookDev.currentContext.GetViewContent(ViewIndex.First).ResetCameraState();
                    LookDev.currentContext.GetViewContent(ViewIndex.Second).ResetCameraState();
                },
                DropdownMenuAction.AlwaysEnabled);

            // Side part
            var sideRadio = new ToolbarRadio(canDeselectAll: true)
            {
                name = Style.k_TabsRadioName
            };
            sideRadio.AddRadios(new[] {
                Style.k_EnvironmentSidePanelName,
                Style.k_DebugSidePanelName,
                });
            sideRadio.SetValueWithoutNotify((int)sidePanel);
            sideRadio.RegisterCallback((ChangeEvent<int> evt)
                => sidePanel = (SidePanel)evt.newValue);

            // Aggregate parts
            var toolbar = new UIElements.Toolbar() { name = Style.k_ToolbarName };
            toolbar.Add(layoutRadio);
            toolbar.Add(new ToolbarSpacer());
            toolbar.Add(cameraMenu);

            toolbar.Add(new ToolbarSpacer() { flex = true });
            if (UnityEditorInternal.RenderDoc.IsInstalled() && UnityEditorInternal.RenderDoc.IsLoaded())
            {
                var renderDocButton = new ToolbarButton(() => OnRenderDocAcquisitionTriggeredInternal?.Invoke())
                {
                    name = Style.k_RenderDocContentName
                };
                renderDocButton.Add(new Image() { image = Style.k_RenderdocIcon });
                renderDocButton.Add(new Label() { text = Style.k_RenderDocLabel });
                toolbar.Add(renderDocButton);
                toolbar.Add(new ToolbarSpacer());
            }
            toolbar.Add(sideRadio);
            rootVisualElement.Add(toolbar);
        }

        void CreateViews()
        {
            if (m_MainContainer == null || m_MainContainer.Equals(null))
                throw new System.MemberAccessException("m_MainContainer should be assigned prior CreateViews()");

            m_ViewContainer = new VisualElement() { name = Style.k_ViewContainerName };
            m_ViewContainer.AddToClassList(LookDev.currentContext.layout.isMultiView ? Style.k_SecondViewsClass : Style.k_FirstViewClass);
            m_ViewContainer.AddToClassList(Style.k_SharedContainerClass);
            m_MainContainer.Add(m_ViewContainer);
            m_ViewContainer.RegisterCallback<MouseDownEvent>(evt => OnMouseEventInViewPortInternal?.Invoke(evt));
            m_ViewContainer.RegisterCallback<MouseUpEvent>(evt => OnMouseEventInViewPortInternal?.Invoke(evt));
            m_ViewContainer.RegisterCallback<MouseMoveEvent>(evt => OnMouseEventInViewPortInternal?.Invoke(evt));

            m_Views[(int)ViewIndex.First] = new Image() { name = Style.k_FirstViewName, image = Texture2D.blackTexture };
            m_ViewContainer.Add(m_Views[(int)ViewIndex.First]);
            m_Views[(int)ViewIndex.Second] = new Image() { name = Style.k_SecondViewName, image = Texture2D.blackTexture };
            m_ViewContainer.Add(m_Views[(int)ViewIndex.Second]);

            var firstOrCompositeManipulator = new SwitchableCameraController(
                LookDev.currentContext.GetViewContent(ViewIndex.First).camera,
                LookDev.currentContext.GetViewContent(ViewIndex.Second).camera,
                this,
                index =>
                {
                    LookDev.currentContext.SetFocusedCamera(index);
                    var environment = LookDev.currentContext.GetViewContent(index).environment;
                    if (sidePanel == SidePanel.Environment && environment != null && LookDev.currentContext.environmentLibrary != null)
                        m_EnvironmentList.selectedIndex = LookDev.currentContext.environmentLibrary.IndexOf(environment);
                });
            var secondManipulator = new CameraController(
                LookDev.currentContext.GetViewContent(ViewIndex.Second).camera,
                this,
                () =>
                {
                    LookDev.currentContext.SetFocusedCamera(ViewIndex.Second);
                    var environment = LookDev.currentContext.GetViewContent(ViewIndex.Second).environment;
                    if (sidePanel == SidePanel.Environment && environment != null && LookDev.currentContext.environmentLibrary != null)
                        m_EnvironmentList.selectedIndex = LookDev.currentContext.environmentLibrary.IndexOf(environment);
                });
            var gizmoManipulator = new ComparisonGizmoController(LookDev.currentContext.layout.gizmoState, firstOrCompositeManipulator);
            m_Views[(int)ViewIndex.First].AddManipulator(gizmoManipulator); //must take event first to switch the firstOrCompositeManipulator
            m_Views[(int)ViewIndex.First].AddManipulator(firstOrCompositeManipulator);
            m_Views[(int)ViewIndex.Second].AddManipulator(secondManipulator);

            m_NoObject1 = new Label(Style.k_DragAndDropObject);
            m_NoObject1.style.flexGrow = 1;
            m_NoObject1.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_NoObject2 = new Label(Style.k_DragAndDropObject);
            m_NoObject2.style.flexGrow = 1;
            m_NoObject2.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_NoEnvironment1 = new Label(Style.k_DragAndDropEnvironment);
            m_NoEnvironment1.style.flexGrow = 1;
            m_NoEnvironment1.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_NoEnvironment1.style.whiteSpace = WhiteSpace.Normal;
            m_NoEnvironment2 = new Label(Style.k_DragAndDropEnvironment);
            m_NoEnvironment2.style.flexGrow = 1;
            m_NoEnvironment2.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_NoEnvironment2.style.whiteSpace = WhiteSpace.Normal;
            m_Views[(int)ViewIndex.First].Add(m_NoObject1);
            m_Views[(int)ViewIndex.First].Add(m_NoEnvironment1);
            m_Views[(int)ViewIndex.Second].Add(m_NoObject2);
            m_Views[(int)ViewIndex.Second].Add(m_NoEnvironment2);
        }

        void CreateDropAreas()
        {
            // GameObject or Prefab in view
            new DropArea(new[] { typeof(GameObject) }, m_Views[(int)ViewIndex.First], (obj, localPos) =>
            {
                if (viewLayout == Layout.CustomSplit)
                    OnChangingObjectInViewInternal?.Invoke(obj as GameObject, ViewCompositionIndex.Composite, localPos);
                else
                    OnChangingObjectInViewInternal?.Invoke(obj as GameObject, ViewCompositionIndex.First, localPos);
                m_NoObject1.style.visibility = obj == null || obj.Equals(null) ? Visibility.Visible : Visibility.Hidden;
            });
            new DropArea(new[] { typeof(GameObject) }, m_Views[(int)ViewIndex.Second], (obj, localPos) =>
            {
                OnChangingObjectInViewInternal?.Invoke(obj as GameObject, ViewCompositionIndex.Second, localPos);
                m_NoObject2.style.visibility = obj == null || obj.Equals(null) ? Visibility.Visible : Visibility.Hidden;
            });

            // Material in view
            //new DropArea(new[] { typeof(GameObject) }, m_Views[(int)ViewIndex.First], (obj, localPos) =>
            //{
            //    if (layout == Layout.CustomSplit || layout == Layout.CustomCircular)
            //        OnChangingMaterialInViewInternal?.Invoke(obj as Material, ViewCompositionIndex.Composite, localPos);
            //    else
            //        OnChangingMaterialInViewInternal?.Invoke(obj as Material, ViewCompositionIndex.First, localPos);
            //});
            //new DropArea(new[] { typeof(Material) }, m_Views[(int)ViewIndex.Second], (obj, localPos)
            //    => OnChangingMaterialInViewInternal?.Invoke(obj as Material, ViewCompositionIndex.Second, localPos));

            // Environment in view
            new DropArea(new[] { typeof(Environment), typeof(Cubemap) }, m_Views[(int)ViewIndex.First], (obj, localPos) =>
            {
                if (viewLayout == Layout.CustomSplit)
                    OnChangingEnvironmentInViewInternal?.Invoke(obj, ViewCompositionIndex.Composite, localPos);
                else
                    OnChangingEnvironmentInViewInternal?.Invoke(obj, ViewCompositionIndex.First, localPos);
                m_NoEnvironment1.style.visibility = obj == null || obj.Equals(null) ? Visibility.Visible : Visibility.Hidden;
            });
            new DropArea(new[] { typeof(Environment), typeof(Cubemap) }, m_Views[(int)ViewIndex.Second], (obj, localPos) =>
            {
                OnChangingEnvironmentInViewInternal?.Invoke(obj, ViewCompositionIndex.Second, localPos);
                m_NoEnvironment2.style.visibility = obj == null || obj.Equals(null) ? Visibility.Visible : Visibility.Hidden;
            });

            // Environment in library
            //new DropArea(new[] { typeof(Environment), typeof(Cubemap) }, m_EnvironmentContainer, (obj, localPos) =>
            //{
            //    //[TODO: check if this come from outside of library]
            //    OnAddingEnvironmentInternal?.Invoke(obj);
            //});
            new DropArea(new[] { typeof(EnvironmentLibrary) }, m_EnvironmentContainer, (obj, localPos) =>
            {
                OnChangingEnvironmentLibraryInternal?.Invoke(obj as EnvironmentLibrary);
                RefreshLibraryDisplay();
            });
        }

        Rect IViewDisplayer.GetRect(ViewCompositionIndex index)
        {
            switch (index)
            {
                case ViewCompositionIndex.First:
                case ViewCompositionIndex.Composite:    //display composition on first rect
                    return m_Views[(int)ViewIndex.First].contentRect;
                case ViewCompositionIndex.Second:
                    return m_Views[(int)ViewIndex.Second].contentRect;
                default:
                    throw new ArgumentException("Unknown ViewCompositionIndex: " + index);
            }
        }

        Vector2 m_LastFirstViewSize = new Vector2();
        Vector2 m_LastSecondViewSize = new Vector2();
        void IViewDisplayer.SetTexture(ViewCompositionIndex index, Texture texture)
        {
            if (texture == null)
                return;

            bool updated = false;
            switch (index)
            {
                case ViewCompositionIndex.First:
                case ViewCompositionIndex.Composite:    //display composition on first rect
                    if (updated |= m_Views[(int)ViewIndex.First].image != texture)
                        m_Views[(int)ViewIndex.First].image = texture;
                    else if (updated |= (m_LastFirstViewSize.x != texture.width
                                      || m_LastFirstViewSize.y != texture.height))
                    {
                        m_Views[(int)ViewIndex.First].image = null; //force refresh else it will appear zoomed
                        m_Views[(int)ViewIndex.First].image = texture;
                    }
                    if (updated)
                    {
                        m_LastFirstViewSize.x = texture?.width ?? 0;
                        m_LastFirstViewSize.y = texture?.height ?? 0;
                    }
                    break;
                case ViewCompositionIndex.Second:
                    if (m_Views[(int)ViewIndex.Second].image != texture)
                        m_Views[(int)ViewIndex.Second].image = texture;
                    else if (updated |= (m_LastSecondViewSize.x != texture.width
                                      || m_LastSecondViewSize.y != texture.height))
                    {
                        m_Views[(int)ViewIndex.Second].image = null; //force refresh else it will appear zoomed
                        m_Views[(int)ViewIndex.Second].image = texture;
                    }
                    if (updated)
                    {
                        m_LastSecondViewSize.x = texture?.width ?? 0;
                        m_LastSecondViewSize.y = texture?.height ?? 0;
                    }
                    break;
                default:
                    throw new ArgumentException("Unknown ViewCompositionIndex: " + index);
            }
        }

        void IViewDisplayer.Repaint() => Repaint();

        void ApplyLayout(Layout value)
        {
            m_NoObject1.style.visibility = LookDev.currentContext.GetViewContent(ViewIndex.First).hasViewedObject ? Visibility.Hidden : Visibility.Visible;
            m_NoObject2.style.visibility = LookDev.currentContext.GetViewContent(ViewIndex.Second).hasViewedObject ? Visibility.Hidden : Visibility.Visible;
            m_NoEnvironment1.style.visibility = LookDev.currentContext.GetViewContent(ViewIndex.First).hasEnvironment ? Visibility.Hidden : Visibility.Visible;
            m_NoEnvironment2.style.visibility = LookDev.currentContext.GetViewContent(ViewIndex.Second).hasEnvironment ? Visibility.Hidden : Visibility.Visible;

            switch (value)
            {
                case Layout.HorizontalSplit:
                case Layout.VerticalSplit:
                    if (!m_ViewContainer.ClassListContains(Style.k_FirstViewClass))
                        m_ViewContainer.AddToClassList(Style.k_FirstViewClass);
                    if (!m_ViewContainer.ClassListContains(Style.k_SecondViewsClass))
                        m_ViewContainer.AddToClassList(Style.k_SecondViewsClass);
                    if (value == Layout.VerticalSplit)
                    {
                        m_ViewContainer.AddToClassList(Style.k_VerticalViewsClass);
                        if (!m_ViewContainer.ClassListContains(Style.k_VerticalViewsClass))
                            m_ViewContainer.AddToClassList(Style.k_FirstViewClass);
                    }
                    for (int i = 0; i < 2; ++i)
                        m_Views[i].style.display = DisplayStyle.Flex;
                    break;
                case Layout.FullFirstView:
                case Layout.CustomSplit:       //display composition on first rect
                    if (!m_ViewContainer.ClassListContains(Style.k_FirstViewClass))
                        m_ViewContainer.AddToClassList(Style.k_FirstViewClass);
                    if (m_ViewContainer.ClassListContains(Style.k_SecondViewsClass))
                        m_ViewContainer.RemoveFromClassList(Style.k_SecondViewsClass);
                    m_Views[0].style.display = DisplayStyle.Flex;
                    m_Views[1].style.display = DisplayStyle.None;
                    break;
                case Layout.FullSecondView:
                    if (m_ViewContainer.ClassListContains(Style.k_FirstViewClass))
                        m_ViewContainer.RemoveFromClassList(Style.k_FirstViewClass);
                    if (!m_ViewContainer.ClassListContains(Style.k_SecondViewsClass))
                        m_ViewContainer.AddToClassList(Style.k_SecondViewsClass);
                    m_Views[0].style.display = DisplayStyle.None;
                    m_Views[1].style.display = DisplayStyle.Flex;
                    break;
                default:
                    throw new ArgumentException("Unknown Layout");
            }

            //Add flex direction here
            if (value == Layout.VerticalSplit)
                m_ViewContainer.AddToClassList(Style.k_VerticalViewsClass);
            else if (m_ViewContainer.ClassListContains(Style.k_VerticalViewsClass))
                m_ViewContainer.RemoveFromClassList(Style.k_VerticalViewsClass);
        }

        void ApplySidePanelChange(SidePanel sidePanel)
        {
            IStyle GetEnvironmentContenairDraggerStyle()
                => m_EnvironmentContainer.Q(className: "unity-base-slider--vertical").Q("unity-dragger").style;

            if (sidePanel == SidePanel.Environment)
            {
                if (!m_MainContainer.ClassListContains(Style.k_ShowEnvironmentPanelClass))
                    m_MainContainer.AddToClassList(Style.k_ShowEnvironmentPanelClass);

                if (m_EnvironmentList.selectedIndex != -1)
                    m_EnvironmentContainer.Q<EnvironmentElement>().style.visibility = Visibility.Visible;
                GetEnvironmentContenairDraggerStyle().display = DisplayStyle.Flex;
                m_EnvironmentContainer.style.display = DisplayStyle.Flex;
            }
            else
            {
                if (m_MainContainer.ClassListContains(Style.k_ShowEnvironmentPanelClass))
                    m_MainContainer.RemoveFromClassList(Style.k_ShowEnvironmentPanelClass);

                m_EnvironmentContainer.Q<EnvironmentElement>().style.visibility = Visibility.Hidden;
                GetEnvironmentContenairDraggerStyle().display = DisplayStyle.None;
                m_EnvironmentContainer.style.display = DisplayStyle.None;
            }

            if (sidePanel == SidePanel.Debug)
            {
                if (!m_MainContainer.ClassListContains(Style.k_ShowDebugPanelClass))
                    m_MainContainer.AddToClassList(Style.k_ShowDebugPanelClass);
                UpdateSideDebugPanelProperties();
            }
            else
            {
                if (m_MainContainer.ClassListContains(Style.k_ShowDebugPanelClass))
                    m_MainContainer.RemoveFromClassList(Style.k_ShowDebugPanelClass);
            }
        }

        void OnGUI()
        {
            //Stylesheet
            // [case 1219692] if LookDev is open while reimporting CoreRP package,
            // stylesheet can be null. In this case, we can have a null stylesheet
            // registered as it got destroyed. Reloading it. As we cannot just
            // remove a null entry, we must filter and reconstruct the while list.
            if (styleSheet == null || styleSheet.Equals(null)
                || (!EditorGUIUtility.isProSkin && (styleSheetLight == null || styleSheetLight.Equals(null))))
            {
                // While (case 1228706) is still on going, we sill close and reopen the look dev.
                // This will prevent spawning error at frame.
                // Note 2: This actually causes the lookdev to break completely with light theme.
                // Until the actual issue is fixed, we'll comment this fix out as it only concerns an upgrade problem.
                //LookDev.Close();
                //LookDev.Open();
                //return;

                // Following lines is the correct fix if UIElement filter garbage collected Stylesheet.

                //System.Collections.Generic.List<StyleSheet> usedStyleSheets = new System.Collections.Generic.List<StyleSheet>();
                //int currentCount = rootVisualElement.styleSheets.count;
                //for (int i = 0; i < currentCount; ++i)
                //{
                //    StyleSheet sheet = rootVisualElement.styleSheets[i];
                //    if (sheet != null && !sheet.Equals(null))
                //        usedStyleSheets.Add(sheet);
                //}
                //rootVisualElement.styleSheets.Clear();
                //foreach (StyleSheet sheet in usedStyleSheets)
                //    rootVisualElement.styleSheets.Add(sheet);

                //styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(Style.k_uss);
                //if (styleSheet != null && !styleSheet.Equals(null))
                //{
                //    rootVisualElement.styleSheets.Add(styleSheet);
                //    if (!EditorGUIUtility.isProSkin)
                //    {
                //        rootVisualElement.styleSheets.Add(
                //            AssetDatabase.LoadAssetAtPath<StyleSheet>(Style.k_uss_personal_overload));
                //    }
                //}

                //if (styleSheet == null || styleSheet.Equals(null))
                //{
                //    styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(Style.k_uss);
                //    if (styleSheet != null && !styleSheet.Equals(null))
                //        rootVisualElement.styleSheets.Add(styleSheet);
                //}
                //if (!EditorGUIUtility.isProSkin && styleSheetLight != null && !styleSheetLight.Equals(null))
                //{
                //    styleSheetLight = AssetDatabase.LoadAssetAtPath<StyleSheet>(Style.k_uss_personal_overload);
                //    if (styleSheetLight != null && !styleSheetLight.Equals(null))
                //        rootVisualElement.styleSheets.Add(styleSheetLight);
                //}
            }
            if(EditorApplication.isUpdating)
                return;
           
            //deal with missing style on domain reload...
            ReloadStyleSheets();

            // [case 1245086] Guard in case the SRP asset is set to null (or to a not supported SRP) when the lookdev window is already open
            // Note: After an editor reload, we might get a null OnUpdateRequestedInternal and null SRP for a couple of frames, hence the check.
            if (!LookDev.supported && OnUpdateRequestedInternal !=null)
            {
                // Print an error and close the Lookdev window (to avoid spamming the console)
                Debug.LogError($"LookDev is not supported by this Scriptable Render Pipeline: "
                    + (RenderPipelineManager.currentPipeline == null ? "No SRP in use" : RenderPipelineManager.currentPipeline.ToString()));
                LookDev.Close();
                return;
            }

            OnUpdateRequestedInternal?.Invoke();
        }
    }
}
