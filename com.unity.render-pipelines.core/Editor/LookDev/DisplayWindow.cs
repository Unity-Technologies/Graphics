using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Experimental.LookDev
{
    public interface IViewDisplayer
    {
        Rect GetRect(ViewCompositionIndex index);
        void SetTexture(ViewCompositionIndex index, Texture texture);

        void Repaint();

        event Action<Layout, SidePanel> OnLayoutChanged;
        
        event Action<IMouseEvent> OnMouseEventInView;

        event Action<GameObject, ViewCompositionIndex, Vector2> OnChangingObjectInView;
        //event Action<Material, ViewCompositionIndex, Vector2> OnChangingMaterialInView;
        event Action<UnityEngine.Object, ViewCompositionIndex, Vector2> OnChangingEnvironmentInView;
        
        event Action OnClosed;
    }
    
    public interface IEnvironmentDisplayer
    {
        void Repaint();

        //event Action<UnityEngine.Object> OnAddingEnvironment;
        //event Action<int> OnRemovingEnvironment;
        event Action<EnvironmentLibrary> OnChangingEnvironmentLibrary;
    }
    
    /// <summary>
    /// Displayer and User Interaction 
    /// </summary>
    internal class DisplayWindow : EditorWindow, IViewDisplayer, IEnvironmentDisplayer
    {
        static class Style
        {
            internal const string k_IconFolder = @"Packages/com.unity.render-pipelines.core/Editor/LookDev/Icons/";
            internal const string k_uss = @"Packages/com.unity.render-pipelines.core/Editor/LookDev/DisplayWindow.uss";

            public static readonly GUIContent WindowTitleAndIcon = EditorGUIUtility.TrTextContentWithIcon("Look Dev", CoreEditorUtils.LoadIcon(k_IconFolder, "LookDevMainIcon"));
        }
        
        // /!\ WARNING:
        //The following const are used in the uss.
        //If you change them, update the uss file too.
        const string k_MainContainerName = "mainContainer";
        const string k_EnvironmentContainerName = "environmentContainer";
        const string k_ShowEnvironmentPanelClass = "showEnvironmentPanel";
        const string k_ViewContainerName = "viewContainer";
        const string k_FirstViewName = "firstView";
        const string k_SecondViewName = "secondView";
        const string k_ToolbarName = "toolbar";
        const string k_ToolbarRadioName = "toolbarRadio";
        const string k_TabsRadioName = "tabsRadio";
        const string k_SideToolbarName = "sideToolbar";
        const string k_SharedContainerClass = "container";
        const string k_FirstViewClass = "firstView";
        const string k_SecondViewsClass = "secondView";
        const string k_VerticalViewsClass = "verticalSplit";
        const string k_DebugContainerName = "debugContainer";
        const string k_ShowDebugPanelClass = "showDebugPanel";

        VisualElement m_MainContainer;
        VisualElement m_ViewContainer;
        VisualElement m_DebugContainer;
        VisualElement m_EnvironmentContainer;
        ListView m_EnvironmentList;
        EnvironmentElement m_EnvironmentInspector;
        Label m_NoEnvironmentList;
        Label m_NoObject1;
        Label m_NoEnvironment1;
        Label m_NoObject2;
        Label m_NoEnvironment2;

        Image[] m_Views = new Image[2];

        Layout layout
        {
            get => LookDev.currentContext.layout.viewLayout;
            set
            {
                if (LookDev.currentContext.layout.viewLayout != value)
                {
                    OnLayoutChangedInternal?.Invoke(value, sidePanel);
                    ApplyLayout(value);
                }
            }
        }
        
        SidePanel sidePanel
        {
            get => LookDev.currentContext.layout.showedSidePanel;
            set
            {
                if (LookDev.currentContext.layout.showedSidePanel != value)
                {
                    OnLayoutChangedInternal?.Invoke(layout, value);
                    ApplySidePanelChange(value);
                }
            }
        }

        event Action<Layout, SidePanel> OnLayoutChangedInternal;
        event Action<Layout, SidePanel> IViewDisplayer.OnLayoutChanged
        {
            add => OnLayoutChangedInternal += value;
            remove => OnLayoutChangedInternal -= value;
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

        //event Action<UnityEngine.Object> OnAddingEnvironmentInternal;
        //event Action<UnityEngine.Object> IEnvironmentDisplayer.OnAddingEnvironment
        //{
        //    add => OnAddingEnvironmentInternal += value;
        //    remove => OnAddingEnvironmentInternal -= value;
        //}

        //event Action<int> OnRemovingEnvironmentInternal;
        //event Action<int> IEnvironmentDisplayer.OnRemovingEnvironment
        //{
        //    add => OnRemovingEnvironmentInternal += value;
        //    remove => OnRemovingEnvironmentInternal -= value;
        //}

        event Action<EnvironmentLibrary> OnChangingEnvironmentLibraryInternal;
        event Action<EnvironmentLibrary> IEnvironmentDisplayer.OnChangingEnvironmentLibrary
        {
            add => OnChangingEnvironmentLibraryInternal += value;
            remove => OnChangingEnvironmentLibraryInternal -= value;
        }

        void OnEnable()
        {
            //Call the open function to configure LookDev
            // in case the window where open when last editor session finished.
            // (Else it will open at start and has nothing to display).
            if (!LookDev.open)
                LookDev.Open();

            titleContent = Style.WindowTitleAndIcon;

            rootVisualElement.styleSheets.Add(
                AssetDatabase.LoadAssetAtPath<StyleSheet>(Style.k_uss));

            CreateToolbar();
            
            m_MainContainer = new VisualElement() { name = k_MainContainerName };
            m_MainContainer.AddToClassList(k_SharedContainerClass);
            rootVisualElement.Add(m_MainContainer);

            CreateViews();
            CreateEnvironment();
            CreateDebug();
            CreateDropAreas();

            ApplyLayout(layout);
            ApplySidePanelChange(sidePanel);
        }

        void OnDisable() => OnClosedInternal?.Invoke();

        void CreateToolbar()
        {
            // Layout swapper part
            var layoutRadio = new ToolbarRadio("Layout:") { name = k_ToolbarRadioName };
            layoutRadio.AddRadios(new[] {
                CoreEditorUtils.LoadIcon(Style.k_IconFolder, "LookDevSingle1"),
                CoreEditorUtils.LoadIcon(Style.k_IconFolder, "LookDevSingle2"),
                CoreEditorUtils.LoadIcon(Style.k_IconFolder, "LookDevSideBySideVertical"),
                CoreEditorUtils.LoadIcon(Style.k_IconFolder, "LookDevSideBySideHorizontal"),
                CoreEditorUtils.LoadIcon(Style.k_IconFolder, "LookDevSplit"),
                //CoreEditorUtils.LoadIcon(Style.k_IconFolder, "LookDevZone"),
                });
            layoutRadio.RegisterCallback((ChangeEvent<int> evt)
                => layout = (Layout)evt.newValue);
            layoutRadio.SetValueWithoutNotify((int)layout);

            // Environment part
            var sideRadio = new ToolbarRadio(canDeselectAll: true) { name = k_TabsRadioName };
            sideRadio.AddRadios(new[] {
                "Environment",
                "Debug"
            });
            sideRadio.RegisterCallback((ChangeEvent<int> evt)
                => sidePanel = (SidePanel)evt.newValue);
            sideRadio.SetValueWithoutNotify((int)sidePanel);

            var sideToolbar = new Toolbar() { name = k_SideToolbarName };
            sideToolbar.Add(sideRadio);

            // Aggregate parts
            var toolbar = new Toolbar() { name = k_ToolbarName };
            toolbar.Add(layoutRadio);
            toolbar.Add(new ToolbarSpacer());
            //to complete

            toolbar.Add(new ToolbarSpacer() { flex = true });
            toolbar.Add(sideToolbar);
            rootVisualElement.Add(toolbar);
        }

        void CreateViews()
        {
            if (m_MainContainer == null || m_MainContainer.Equals(null))
                throw new System.MemberAccessException("m_MainContainer should be assigned prior CreateViews()");

            m_ViewContainer = new VisualElement() { name = k_ViewContainerName };
            m_ViewContainer.AddToClassList(LookDev.currentContext.layout.isMultiView ? k_SecondViewsClass : k_FirstViewClass);
            m_ViewContainer.AddToClassList(k_SharedContainerClass);
            m_MainContainer.Add(m_ViewContainer);
            m_ViewContainer.RegisterCallback<MouseDownEvent>(evt => OnMouseEventInViewPortInternal?.Invoke(evt));
            m_ViewContainer.RegisterCallback<MouseUpEvent>(evt => OnMouseEventInViewPortInternal?.Invoke(evt));
            m_ViewContainer.RegisterCallback<MouseMoveEvent>(evt => OnMouseEventInViewPortInternal?.Invoke(evt));

            m_Views[(int)ViewIndex.First] = new Image() { name = k_FirstViewName, image = Texture2D.blackTexture };
            m_ViewContainer.Add(m_Views[(int)ViewIndex.First]);
            m_Views[(int)ViewIndex.Second] = new Image() { name = k_SecondViewName, image = Texture2D.blackTexture };
            m_ViewContainer.Add(m_Views[(int)ViewIndex.Second]);

            var firstOrCompositeManipulator = new SwitchableCameraController(LookDev.currentContext.GetViewContent(ViewIndex.First).camera, LookDev.currentContext.GetViewContent(ViewIndex.Second).camera, this);
            var secondManipulator = new CameraController(LookDev.currentContext.GetViewContent(ViewIndex.Second).camera, this);
            var gizmoManipulator = new ComparisonGizmoController(LookDev.currentContext.layout.gizmoState, firstOrCompositeManipulator);
            m_Views[(int)ViewIndex.First].AddManipulator(gizmoManipulator); //must take event first to switch the firstOrCompositeManipulator
            m_Views[(int)ViewIndex.First].AddManipulator(firstOrCompositeManipulator);
            m_Views[(int)ViewIndex.Second].AddManipulator(secondManipulator);

            m_NoObject1 = new Label("Drag'n'drop object here");
            m_NoObject1.style.flexGrow = 1;
            m_NoObject1.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_NoObject2 = new Label("Drag'n'drop object here");
            m_NoObject2.style.flexGrow = 1;
            m_NoObject2.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_NoEnvironment1 = new Label("Drag'n'drop environment from side panel here");
            m_NoEnvironment1.style.flexGrow = 1;
            m_NoEnvironment1.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_NoEnvironment2 = new Label("Drag'n'drop environment from side panel here");
            m_NoEnvironment2.style.flexGrow = 1;
            m_NoEnvironment2.style.unityTextAlign = TextAnchor.MiddleCenter;
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
                if (layout == Layout.CustomSplit || layout == Layout.CustomCircular)
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
                if (layout == Layout.CustomSplit || layout == Layout.CustomCircular)
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

        void CreateDebug()
        {
            if (m_MainContainer == null || m_MainContainer.Equals(null))
                throw new System.MemberAccessException("m_MainContainer should be assigned prior CreateEnvironment()");

            m_DebugContainer = new VisualElement() { name = k_DebugContainerName };
            m_MainContainer.Add(m_DebugContainer);
            if (sidePanel == SidePanel.Debug)
                m_MainContainer.AddToClassList(k_ShowDebugPanelClass);


            //[TODO: finish]
            //Toggle greyBalls = new Toggle("Grey balls");
            //greyBalls.SetValueWithoutNotify(LookDev.currentContext.GetViewContent(LookDev.currentContext.layout.lastFocusedView).debug.greyBalls);
            //greyBalls.RegisterValueChangedCallback(evt =>
            //{
            //    LookDev.currentContext.GetViewContent(LookDev.currentContext.layout.lastFocusedView).debug.greyBalls = evt.newValue;
            //});
            //m_DebugContainer.Add(greyBalls);

            //[TODO: debug why list sometimes empty on resource reloading]
            //[TODO: display only per view]
            List<string> list = new List<string>(LookDev.dataProvider?.supportedDebugModes ?? Enumerable.Empty<string>());
            if (list.Count == 0)
                Debug.LogWarning("DebugMode are wrongly reset on resource loading. Close and reopen the windows.");
            list.Insert(0, "None");
            PopupField<string> debugView = new PopupField<string>("Debug view mode", list, 0);
            debugView.RegisterValueChangedCallback(evt
                => LookDev.dataProvider.UpdateDebugMode(list.IndexOf(evt.newValue) - 1));
            m_DebugContainer.Add(debugView);
        }

        void CreateEnvironment()
        {
            if (m_MainContainer == null || m_MainContainer.Equals(null))
                throw new System.MemberAccessException("m_MainContainer should be assigned prior CreateEnvironment()");

            m_EnvironmentContainer = new VisualElement() { name = k_EnvironmentContainerName };
            m_MainContainer.Add(m_EnvironmentContainer);
            if (sidePanel == SidePanel.Environment)
                m_MainContainer.AddToClassList(k_ShowEnvironmentPanelClass);

            m_EnvironmentInspector = new EnvironmentElement(withPreview: false, () =>
            {
                LookDev.SaveContextChangeAndApply(ViewIndex.First);
                LookDev.SaveContextChangeAndApply(ViewIndex.Second);
            });
            m_EnvironmentList = new ListView();
            m_EnvironmentList.selectionType = SelectionType.Single;
            m_EnvironmentList.itemHeight = EnvironmentElement.k_SkyThumbnailHeight;
            m_EnvironmentList.makeItem = () =>
            {
                var preview = new Image();
                preview.AddManipulator(new EnvironmentPreviewDragger(this, m_ViewContainer));
                return preview;
            };
            m_EnvironmentList.bindItem = (e, i) =>
            {
                (e as Image).image = EnvironmentElement.GetLatLongThumbnailTexture(
                    LookDev.currentContext.environmentLibrary[i],
                    EnvironmentElement.k_SkyThumbnailWidth);
            };
            m_EnvironmentList.onSelectionChanged += objects =>
            {
                if (objects.Count == 0 || (LookDev.currentContext.environmentLibrary?.Count ?? 0) == 0)
                {
                    m_EnvironmentInspector.style.visibility = Visibility.Hidden;
                    m_EnvironmentInspector.style.height = 0;
                }
                else
                {
                    m_EnvironmentInspector.style.visibility = Visibility.Visible;
                    m_EnvironmentInspector.style.height = new StyleLength(StyleKeyword.Auto);
                    m_EnvironmentInspector.Bind(
                        LookDev.currentContext.environmentLibrary[m_EnvironmentList.selectedIndex],
                        m_EnvironmentList.Q("unity-content-container")[m_EnvironmentList.selectedIndex] as Image);
                }
            };
            m_EnvironmentList.onItemChosen += obj =>
                EditorGUIUtility.PingObject(LookDev.currentContext.environmentLibrary[(int)obj]);
            m_NoEnvironmentList = new Label("Drag'n'drop EnvironmentLibrary here");
            m_NoEnvironmentList.style.flexGrow = 1;
            m_NoEnvironmentList.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_EnvironmentContainer.Add(m_EnvironmentInspector);
            m_EnvironmentContainer.Add(m_EnvironmentList);
            m_EnvironmentContainer.Add(m_NoEnvironmentList);

            //add ability to unselect
            m_EnvironmentList.RegisterCallback<MouseDownEvent>(evt =>
            {
                var clickedIndex = (int)(evt.localMousePosition.y / m_EnvironmentList.itemHeight);
                if (clickedIndex >= m_EnvironmentList.itemsSource.Count)
                {
                    m_EnvironmentList.selectedIndex = -1;
                    evt.StopPropagation();
                }
            });

            RefreshLibraryDisplay();
        }

        void RefreshLibraryDisplay()
        {
            int itemMax = LookDev.currentContext.environmentLibrary?.Count ?? 0;
            var items = new List<int>(itemMax);
            for (int i = 0; i < itemMax; i++)
                items.Add(i);
            m_EnvironmentList.itemsSource = items;
            m_EnvironmentInspector.style.visibility = (itemMax == 0 || m_EnvironmentList.selectedIndex == -1)
                ? Visibility.Hidden
                : Visibility.Visible;
            m_EnvironmentList
                .Q(className: "unity-scroll-view__vertical-scroller")
                .Q("unity-dragger")
                .style.visibility = itemMax == 0
                    ? Visibility.Hidden
                    : Visibility.Visible;
            m_NoEnvironmentList.style.display = itemMax == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        DraggingContext StartDragging(VisualElement item, Vector2 worldPosition)
            => new DraggingContext(
                rootVisualElement,
                item as Image,
                //note: this even can come before the selection event of the
                //ListView. Reconstruct index by looking at target of the event.
                (int)item.layout.y / m_EnvironmentList.itemHeight,
                worldPosition);
        
        void EndDragging(DraggingContext context, Vector2 mouseWorldPosition)
        {
            Environment environment = LookDev.currentContext.environmentLibrary[context.draggedIndex];
            if (m_Views[(int)ViewIndex.First].ContainsPoint(mouseWorldPosition))
            {
                if (layout == Layout.CustomSplit || layout == Layout.CustomCircular)
                    OnChangingEnvironmentInViewInternal?.Invoke(environment, ViewCompositionIndex.Composite, mouseWorldPosition);
                else
                    OnChangingEnvironmentInViewInternal?.Invoke(environment, ViewCompositionIndex.First, mouseWorldPosition);
            }
            else
                OnChangingEnvironmentInViewInternal?.Invoke(environment, ViewCompositionIndex.Second, mouseWorldPosition);
        }

        class DraggingContext : IDisposable
        {
            const string k_CursorFollowerName = "cursorFollower";
            public readonly int draggedIndex;
            readonly Image cursorFollower;
            readonly Vector2 cursorOffset;
            readonly VisualElement windowContent;

            public DraggingContext(VisualElement windowContent, Image draggedElement, int draggedIndex, Vector2 worldPosition)
            {
                this.windowContent = windowContent;
                this.draggedIndex = draggedIndex;
                cursorFollower = new Image()
                {
                    name = k_CursorFollowerName,
                    image = draggedElement.image
                };
                cursorFollower.tintColor = new Color(1f, 1f, 1f, .5f);
                windowContent.Add(cursorFollower);
                cursorOffset = draggedElement.WorldToLocal(worldPosition);

                cursorFollower.style.position = Position.Absolute;
                UpdateCursorFollower(worldPosition);
            }

            public void UpdateCursorFollower(Vector2 mouseWorldPosition)
            {
                Vector2 windowLocalPosition = windowContent.WorldToLocal(mouseWorldPosition);
                cursorFollower.style.left = windowLocalPosition.x - cursorOffset.x;
                cursorFollower.style.top = windowLocalPosition.y - cursorOffset.y;
            }

            public void Dispose()
            {
                if (windowContent.Contains(cursorFollower))
                    windowContent.Remove(cursorFollower);
            }
        }

        class EnvironmentPreviewDragger : Manipulator
        {
            VisualElement m_DropArea;
            DisplayWindow m_Window;

            //Note: static as only one drag'n'drop at a time
            static DraggingContext s_Context;

            public EnvironmentPreviewDragger(DisplayWindow window, VisualElement dropArea)
            {
                m_Window = window;
                m_DropArea = dropArea;
            }

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<MouseDownEvent>(OnMouseDown);
                target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
                target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            }

            void Release()
            {
                target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                s_Context.Dispose();
                target.ReleaseMouse();
                s_Context = null;
            }

            void OnMouseDown(MouseDownEvent evt)
            {
                if (evt.button == 0)
                {
                    target.CaptureMouse();
                    target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                    s_Context = m_Window.StartDragging(target, evt.mousePosition);
                    //do not stop event as we still need to propagate it to the ListView for selection
                }
            }

            void OnMouseUp(MouseUpEvent evt)
            {
                if (evt.button != 0)
                    return;
                if (m_DropArea.ContainsPoint(m_DropArea.WorldToLocal(Event.current.mousePosition)))
                {
                    m_Window.EndDragging(s_Context, evt.mousePosition);
                    evt.StopPropagation();
                }
                Release();
            }

            void OnMouseMove(MouseMoveEvent evt)
            {
                evt.StopPropagation();
                s_Context.UpdateCursorFollower(evt.mousePosition);
            }
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

        void IViewDisplayer.SetTexture(ViewCompositionIndex index, Texture texture)
        {
            switch (index)
            {
                case ViewCompositionIndex.First:
                case ViewCompositionIndex.Composite:    //display composition on first rect
                    if (m_Views[(int)ViewIndex.First].image != texture)
                        m_Views[(int)ViewIndex.First].image = texture;
                    break;
                case ViewCompositionIndex.Second:
                    if (m_Views[(int)ViewIndex.Second].image != texture)
                        m_Views[(int)ViewIndex.Second].image = texture;
                    break;
                default:
                    throw new ArgumentException("Unknown ViewCompositionIndex: " + index);
            }
        }

        void IViewDisplayer.Repaint() => Repaint();

        //[TODO]
        void IEnvironmentDisplayer.Repaint()
        {
            throw new NotImplementedException();
        }

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
                    if (!m_ViewContainer.ClassListContains(k_FirstViewClass))
                        m_ViewContainer.AddToClassList(k_FirstViewClass);
                    if (!m_ViewContainer.ClassListContains(k_SecondViewsClass))
                        m_ViewContainer.AddToClassList(k_SecondViewsClass);
                    if (value == Layout.VerticalSplit)
                    {
                        m_ViewContainer.AddToClassList(k_VerticalViewsClass);
                        if (!m_ViewContainer.ClassListContains(k_VerticalViewsClass))
                            m_ViewContainer.AddToClassList(k_FirstViewClass);
                    }
                    for (int i = 0; i < 2; ++i)
                        m_Views[i].style.display = DisplayStyle.Flex;
                    break;
                case Layout.FullFirstView:
                case Layout.CustomSplit:       //display composition on first rect
                case Layout.CustomCircular:    //display composition on first rect
                    if (!m_ViewContainer.ClassListContains(k_FirstViewClass))
                        m_ViewContainer.AddToClassList(k_FirstViewClass);
                    if (m_ViewContainer.ClassListContains(k_SecondViewsClass))
                        m_ViewContainer.RemoveFromClassList(k_SecondViewsClass);
                    m_Views[0].style.display = DisplayStyle.Flex;
                    m_Views[1].style.display = DisplayStyle.None;
                    break;
                case Layout.FullSecondView:
                    if (m_ViewContainer.ClassListContains(k_FirstViewClass))
                        m_ViewContainer.RemoveFromClassList(k_FirstViewClass);
                    if (!m_ViewContainer.ClassListContains(k_SecondViewsClass))
                        m_ViewContainer.AddToClassList(k_SecondViewsClass);
                    m_Views[0].style.display = DisplayStyle.None;
                    m_Views[1].style.display = DisplayStyle.Flex;
                    break;
                default:
                    throw new ArgumentException("Unknown Layout");
            }

            //Add flex direction here
            if (value == Layout.VerticalSplit)
                m_ViewContainer.AddToClassList(k_VerticalViewsClass);
            else if (m_ViewContainer.ClassListContains(k_VerticalViewsClass))
                m_ViewContainer.RemoveFromClassList(k_VerticalViewsClass);
        }

        void ApplySidePanelChange(SidePanel sidePanel)
        {
            switch (sidePanel)
            {
                case SidePanel.None:
                    if (m_MainContainer.ClassListContains(k_ShowEnvironmentPanelClass))
                        m_MainContainer.RemoveFromClassList(k_ShowEnvironmentPanelClass);
                    if (m_MainContainer.ClassListContains(k_ShowDebugPanelClass))
                        m_MainContainer.RemoveFromClassList(k_ShowDebugPanelClass);
                    m_EnvironmentContainer.Q<EnvironmentElement>().style.visibility = Visibility.Hidden;
                    m_EnvironmentContainer.Q(className: "unity-base-slider--vertical").Q("unity-dragger").style.display = DisplayStyle.None;
                    m_EnvironmentContainer.style.display = DisplayStyle.None;
                    break;
                case SidePanel.Environment:
                    if (!m_MainContainer.ClassListContains(k_ShowEnvironmentPanelClass))
                        m_MainContainer.AddToClassList(k_ShowEnvironmentPanelClass);
                    if (m_MainContainer.ClassListContains(k_ShowDebugPanelClass))
                        m_MainContainer.RemoveFromClassList(k_ShowDebugPanelClass);
                    if (m_EnvironmentList.selectedIndex != -1)
                        m_EnvironmentContainer.Q<EnvironmentElement>().style.visibility = Visibility.Visible;
                    m_EnvironmentContainer.Q(className: "unity-base-slider--vertical").Q("unity-dragger").style.display = DisplayStyle.Flex;
                    m_EnvironmentContainer.style.display = DisplayStyle.Flex;
                    break;
                case SidePanel.Debug:
                    if (m_MainContainer.ClassListContains(k_ShowEnvironmentPanelClass))
                        m_MainContainer.RemoveFromClassList(k_ShowEnvironmentPanelClass);
                    if (!m_MainContainer.ClassListContains(k_ShowDebugPanelClass))
                        m_MainContainer.AddToClassList(k_ShowDebugPanelClass);
                    m_EnvironmentContainer.Q<EnvironmentElement>().style.visibility = Visibility.Hidden;
                    m_EnvironmentContainer.Q(className: "unity-base-slider--vertical").Q("unity-dragger").style.display = DisplayStyle.None;
                    m_EnvironmentContainer.style.display = DisplayStyle.None;
                    break;
                default:
                    throw new ArgumentException("Unknown SidePanel");
            }
        }
    }
}
