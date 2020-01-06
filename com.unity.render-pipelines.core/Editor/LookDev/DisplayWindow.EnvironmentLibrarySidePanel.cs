using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.LookDev
{
    /// <summary>Interface that must implement the EnvironmentLibrary view to communicate with the data management</summary>
    public interface IEnvironmentDisplayer
    {
        /// <summary>Repaint the UI</summary>
        void Repaint();

        /// <summary>Callback on Environment change in the Library</summary>
        event Action<EnvironmentLibrary> OnChangingEnvironmentLibrary;
    }

    partial class DisplayWindow : IEnvironmentDisplayer
    {
        static partial class Style
        {
            internal static readonly Texture2D k_AddIcon = CoreEditorUtils.LoadIcon(Style.k_IconFolder, "Add", forceLowRes: true);
            internal static readonly Texture2D k_RemoveIcon = CoreEditorUtils.LoadIcon(Style.k_IconFolder, "Remove", forceLowRes: true);
            internal static readonly Texture2D k_DuplicateIcon = CoreEditorUtils.LoadIcon(Style.k_IconFolder, "Duplicate", forceLowRes: true);

            internal const string k_DragAndDropLibrary = "Drag and drop EnvironmentLibrary here";
        }

        VisualElement m_EnvironmentContainer;
        ListView m_EnvironmentList;
        EnvironmentElement m_EnvironmentInspector;
        Toolbar m_EnvironmentListToolbar;
        
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

        static int FirstVisibleIndex(ListView listView)
            => (int)(listView.Q<ScrollView>().scrollOffset.y / listView.itemHeight);

        void CreateEnvironment()
        {
            if (m_MainContainer == null || m_MainContainer.Equals(null))
                throw new System.MemberAccessException("m_MainContainer should be assigned prior CreateEnvironment()");

            m_EnvironmentContainer = new VisualElement() { name = Style.k_EnvironmentContainerName };
            m_MainContainer.Add(m_EnvironmentContainer);
            if (sidePanel == SidePanel.Environment)
                m_MainContainer.AddToClassList(Style.k_ShowEnvironmentPanelClass);

            m_EnvironmentInspector = new EnvironmentElement(withPreview: false, () =>
            {
                LookDev.SaveContextChangeAndApply(ViewIndex.First);
                LookDev.SaveContextChangeAndApply(ViewIndex.Second);
            });
            m_EnvironmentList = new ListView();
            m_EnvironmentList.AddToClassList("list-environment");
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
#if UNITY_2020_1_OR_NEWER
            m_EnvironmentList.onSelectionChange += objects =>
            {
                bool empty = !objects.GetEnumerator().MoveNext();
                if (empty || (LookDev.currentContext.environmentLibrary?.Count ?? 0) == 0)
#else
            m_EnvironmentList.onSelectionChanged += objects =>

            {
                if (objects.Count == 0 || (LookDev.currentContext.environmentLibrary?.Count ?? 0) == 0)
#endif
                {
                    m_EnvironmentInspector.style.visibility = Visibility.Hidden;
                    m_EnvironmentInspector.style.height = 0;
                }
                else
                {
                    m_EnvironmentInspector.style.visibility = Visibility.Visible;
                    m_EnvironmentInspector.style.height = new StyleLength(StyleKeyword.Auto);
                    int firstVisibleIndex = FirstVisibleIndex(m_EnvironmentList);
                    Environment environment = LookDev.currentContext.environmentLibrary[m_EnvironmentList.selectedIndex];
                    var container = m_EnvironmentList.Q("unity-content-container");
                    if (m_EnvironmentList.selectedIndex - firstVisibleIndex >= container.childCount || m_EnvironmentList.selectedIndex < firstVisibleIndex)
                    {
                        m_EnvironmentList.ScrollToItem(m_EnvironmentList.selectedIndex);
                        firstVisibleIndex = FirstVisibleIndex(m_EnvironmentList);
                    }
                    Image deportedLatLong = container[m_EnvironmentList.selectedIndex - firstVisibleIndex] as Image;
                    m_EnvironmentInspector.Bind(environment, deportedLatLong);
                }
            };
#if UNITY_2020_1_OR_NEWER
            m_EnvironmentList.onItemsChosen += objCollection =>
            {
                foreach(var obj in objCollection)
                    EditorGUIUtility.PingObject(LookDev.currentContext.environmentLibrary[(int)obj]);
            };
#else
            m_EnvironmentList.onItemChosen += obj =>
                EditorGUIUtility.PingObject(LookDev.currentContext.environmentLibrary[(int)obj]);
#endif
            m_NoEnvironmentList = new Label(Style.k_DragAndDropLibrary);
            m_NoEnvironmentList.style.flexGrow = 1;
            m_NoEnvironmentList.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_EnvironmentContainer.Add(m_EnvironmentInspector);

            m_EnvironmentListToolbar = new Toolbar();
            ToolbarButton addEnvironment = new ToolbarButton(() =>
            {
                LookDev.currentContext.environmentLibrary.Add();
                RefreshLibraryDisplay();
                m_EnvironmentList.ScrollToItem(-1); //-1: scroll to end
                m_EnvironmentList.selectedIndex = LookDev.currentContext.environmentLibrary.Count - 1;
                ScrollToEnd();
            })
            {
                name = "add",
                tooltip = "Add new empty environment"
            };
            addEnvironment.Add(new Image() { image = Style.k_AddIcon });
            ToolbarButton removeEnvironment = new ToolbarButton(() =>
            {
                if (m_EnvironmentList.selectedIndex == -1)
                    return;
                LookDev.currentContext.environmentLibrary.Remove(m_EnvironmentList.selectedIndex);
                RefreshLibraryDisplay();
                m_EnvironmentList.selectedIndex = -1;
            })
            {
                name = "remove",
                tooltip = "Remove environment currently selected"
            };
            removeEnvironment.Add(new Image() { image = Style.k_RemoveIcon });
            ToolbarButton duplicateEnvironment = new ToolbarButton(() =>
            {
                if (m_EnvironmentList.selectedIndex == -1)
                    return;
                LookDev.currentContext.environmentLibrary.Duplicate(m_EnvironmentList.selectedIndex);
                RefreshLibraryDisplay();
                m_EnvironmentList.ScrollToItem(-1); //-1: scroll to end
                m_EnvironmentList.selectedIndex = LookDev.currentContext.environmentLibrary.Count - 1;
                ScrollToEnd();
            })
            {
                name = "duplicate",
                tooltip = "Duplicate environment currently selected"
            };
            duplicateEnvironment.Add(new Image() { image = Style.k_DuplicateIcon });
            m_EnvironmentListToolbar.Add(addEnvironment);
            m_EnvironmentListToolbar.Add(removeEnvironment);
            m_EnvironmentListToolbar.Add(duplicateEnvironment);
            m_EnvironmentListToolbar.AddToClassList("list-environment-overlay");

            var m_EnvironmentInspectorSeparator = new VisualElement() { name = "separator-line" };
            m_EnvironmentInspectorSeparator.Add(new VisualElement() { name = "separator" });
            m_EnvironmentContainer.Add(m_EnvironmentInspectorSeparator);

            VisualElement listContainer = new VisualElement();
            listContainer.AddToClassList("list-environment");
            listContainer.Add(m_EnvironmentList);
            listContainer.Add(m_EnvironmentListToolbar);


            var libraryField = new ObjectField("Library")
            {
                tooltip = "The currently used library"
            };
            libraryField.allowSceneObjects = false;
            libraryField.objectType = typeof(EnvironmentLibrary);
            libraryField.SetValueWithoutNotify(LookDev.currentContext.environmentLibrary);
            libraryField.RegisterValueChangedCallback(evt =>
            {
                m_EnvironmentList.selectedIndex = -1;
                OnChangingEnvironmentLibraryInternal?.Invoke(evt.newValue as EnvironmentLibrary);
                RefreshLibraryDisplay();
            });

            var environmentListCreationToolbar = new Toolbar()
            {
                name = "environmentListCreationToolbar"
            };
            environmentListCreationToolbar.Add(libraryField);
            environmentListCreationToolbar.Add(new ToolbarButton(()
                => EnvironmentLibraryCreator.CreateAndAssignTo(libraryField))
            {
                text = "New",
                tooltip = "Create a new EnvironmentLibrary"
            });

            m_EnvironmentContainer.Add(listContainer);
            m_EnvironmentContainer.Add(m_NoEnvironmentList);
            m_EnvironmentContainer.Add(environmentListCreationToolbar);

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

        //necessary as the scrollview need to be updated which take some editor frames.
        void ScrollToEnd(int attemptRemaining = 5)
        {
            m_EnvironmentList.ScrollToItem(-1); //-1: scroll to end
            if (attemptRemaining > 0)
                EditorApplication.delayCall += () => ScrollToEnd(--attemptRemaining);
        }

        void RefreshLibraryDisplay()
        {
            int itemMax = LookDev.currentContext.environmentLibrary?.Count ?? 0;
            if (itemMax == 0 || m_EnvironmentList.selectedIndex == -1)
            {
                m_EnvironmentInspector.style.visibility = Visibility.Hidden;
                m_EnvironmentInspector.style.height = 0;
            }
            else
            {
                m_EnvironmentInspector.style.visibility = Visibility.Visible;
                m_EnvironmentInspector.style.height = new StyleLength(StyleKeyword.Auto);
            }
            var items = new List<int>(itemMax);
            for (int i = 0; i < itemMax; i++)
                items.Add(i);
            m_EnvironmentList.itemsSource = items;
            if (LookDev.currentContext.environmentLibrary == null)
            {
                m_EnvironmentList
                    .Q(className: "unity-scroll-view__vertical-scroller")
                    .Q("unity-dragger")
                    .style.visibility = Visibility.Hidden;
                m_EnvironmentListToolbar.style.visibility = Visibility.Hidden;
                m_NoEnvironmentList.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_EnvironmentList
                    .Q(className: "unity-scroll-view__vertical-scroller")
                    .Q("unity-dragger")
                    .style.visibility = itemMax == 0
                        ? Visibility.Hidden
                        : Visibility.Visible;
                m_EnvironmentListToolbar.style.visibility = Visibility.Visible;
                m_NoEnvironmentList.style.display = DisplayStyle.None;
            }
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
                if (viewLayout == Layout.CustomSplit)
                    OnChangingEnvironmentInViewInternal?.Invoke(environment, ViewCompositionIndex.Composite, mouseWorldPosition);
                else
                    OnChangingEnvironmentInViewInternal?.Invoke(environment, ViewCompositionIndex.First, mouseWorldPosition);
                m_NoEnvironment1.style.visibility = environment == null || environment.Equals(null) ? Visibility.Visible : Visibility.Hidden;
            }
            else
            {
                OnChangingEnvironmentInViewInternal?.Invoke(environment, ViewCompositionIndex.Second, mouseWorldPosition);
                m_NoEnvironment2.style.visibility = environment == null || environment.Equals(null) ? Visibility.Visible : Visibility.Hidden;
            }
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
                s_Context?.Dispose();
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
        
        void IEnvironmentDisplayer.Repaint()
            => RefreshLibraryDisplay();
    }
}
