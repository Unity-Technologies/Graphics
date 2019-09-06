using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    public class ReorderableListView : ListView
    {
        /*=========================================================================================
            
            Statics

        ==========================================================================================*/
        
        public static readonly string s_ItemUssPath = "Styles/ReorderableList/View";
        public static readonly string s_ItemUxmlPath = "UXML/ReorderableList/Item";

        public static readonly string s_ReorderableListViewName = "unity-reorderable-list-view";
        public static readonly string s_ListViewName = "unity-reorderable-list-view__list-view";
        public static readonly string s_ItemName = "unity-reorderable-list-view__item";
        public static readonly string s_ItemSelected = "unity-reorderable-list-view__item-selected";
        public static readonly string s_ItemGripperName = "unity-reorderable-list-view__item-gripper";
        public static readonly string s_ItemContentContainerName = "unity-reorderable-list-view__item-content";
        public static readonly string s_ItemSidebar = "unity-reorderable-list-view__item-sidebar";
        public static readonly string s_ItemSidebarSelected = "unity-reorderable-list-view__item-sidebar-selected";
        public static readonly string s_ItemSelectionMarker = "unity-reorderable-list-view__item-selection-marker";

        public static readonly string s_DropMarkerName = "unity-reoderable-list-view__drop-marker";
        public static readonly string s_DragContainerName = "unity-reoderable-list-view__drag-container";

        private static readonly int s_MinDragDistance = 10;
        private static readonly int s_DropMarkerHeight = 4;
        private static readonly Color s_DropMarkerColor = new Color( 218f / 255f, 165f / 255f, 16f / 255f );

        private static readonly float s_DragContainerBorderWidth = 2;
        private static readonly float s_DragContainerPaddingX = 16;
        
        /*=========================================================================================
            
            Factory Classes

        ==========================================================================================*/

        public new class UxmlFactory : UxmlFactory< ReorderableListView, UxmlTraits > {}

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlIntAttributeDescription m_ItemHeight = new UxmlIntAttributeDescription { name = "item-height", defaultValue = 30 };

            public override IEnumerable< UxmlChildElementDescription > uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init( VisualElement ve, IUxmlAttributes bag, CreationContext cc )
            {
                base.Init(ve, bag, cc);
                ( ( ReorderableListView ) ve ).itemHeight = m_ItemHeight.GetValueFromBag( bag, cc );
            }
        }

        /*=========================================================================================
            
            ReorderableListView

        ==========================================================================================*/

        public enum DragMovementType
        {
            FitToParentEdges    = 0,
            ClampToParentEdges  = 1,
            Free                = 2,
        }

        public Action< List< object >, int, List< int > > onCopy;
        public Func< int, List< object > > onPaste;

        private Action< VisualElement, int > m_BindItem;
        public new Action< VisualElement, int > bindItem
        {
            set
            {
                if( m_BindItem == value )
                {
                    return;
                }

                m_BindItem = value;
                Refresh();
            }

            get
            {
                return m_BindItem;
            }
        }

        private Func< VisualElement > m_MakeItem;
        public new Func< VisualElement > makeItem
        {
            set
            {
                if( m_MakeItem == value )
                {
                    return;
                }

                m_MakeItem = value;
                Refresh();
            }

            get
            {
                return m_MakeItem;
            }
        }

        public event Action< List< object > >       onItemRightClick;
        public event Action< int, MouseUpEvent >    onBeforeReorder;
        public event Action< int, List< object > >  onReordered;
        public event Action< int, MouseMoveEvent >  onSelectionDrag;

        public int selectionCount
        {
            get { return m_SelectedIndices.Count; }
        }

        private List< int > m_SelectedIndices;
        public List< int > selectedIndices
        {
            get { return m_SelectedIndices; }
            set
            {
                SetSelection( value );
            }
        }

        private ScrollView m_ScrollView;
        private VisualElement m_DropMarker;
        private VisualElement m_DragContainer;
        private Vector2 m_DragStartPos;
        private bool m_IsMouseDown;
        private bool m_IsPrevMouseDown;
        private bool m_IsDragging;
        private bool m_Scrolling;

        public VisualElement dropMarker => m_DropMarker;
        public VisualElement dragContainer => m_DragContainer;
        
        public bool allowCopyAndPaste { get; set; }
        public Vector2 panSpeed { get; set; }
        private DragMovementType dragMovementType { get; set; }

        public ReorderableListView()
        {
            this.name = s_ReorderableListViewName;
            AddToClassList( s_ReorderableListViewName );
            this.style.backgroundColor = new Color( .25f, .25f, .25f );

            panSpeed = new Vector2( 20, 20 );
            dragMovementType = DragMovementType.FitToParentEdges;
            
            m_SelectedIndices = new List< int >();

            RegisterCallback< GeometryChangedEvent >( OnSizeChanged );
            RegisterCallback< ChangeEvent< object > >( OnChangeEvent );

            base.makeItem = MakeListItem;
            base.bindItem = BindListItem;
            base.onSelectionChanged += OnSelectionChanged;

            m_ScrollView = this.Q< ScrollView >();
            m_ScrollView.contentContainer.RegisterCallback< MouseMoveEvent >( OnDrag );
            m_ScrollView.contentContainer.RegisterCallback< MouseUpEvent >( OnDrop );
            m_ScrollView.contentContainer.RegisterCallback< MouseDownEvent >( OnClick ); //, TrickleDown.TrickleDown );
            m_ScrollView.contentContainer.RegisterCallback< MouseDownEvent >( OnRightClick );
            m_ScrollView.contentContainer.RegisterCallback< WheelEvent >( OnScroll );

            AddToClassList( s_ReorderableListViewName );
            
            RegisterCallback< CustomStyleResolvedEvent >( OnCustomStyleResolved );

            // make the container for dragged elements
            m_DragContainer = new VisualElement()
            {
                name = s_DragContainerName,
                style =
                {
                    flexDirection = FlexDirection.Column,
                    opacity = 1f,
                    borderTopColor = new Color( 0, 255, 255 ),
                    borderBottomColor = new Color( 0, 255, 255 ),
                    borderRightColor = new Color( 0, 255, 255 ),
                    borderLeftColor = new Color( 0, 255, 255 ),
                    backgroundColor = new Color( 0, 255, 255, 125 ),
                    borderTopWidth = s_DragContainerBorderWidth,
                    borderBottomWidth = s_DragContainerBorderWidth,
                    borderLeftWidth = s_DragContainerBorderWidth,
                    borderRightWidth = s_DragContainerBorderWidth,
                    borderTopRightRadius = 2,
                    borderTopLeftRadius = 2,
                    borderBottomRightRadius = 2,
                    borderBottomLeftRadius = 2,
                }
            };
            m_DragContainer.AddToClassList( s_DragContainerName );

            // make the drop marker element
            m_DropMarker = new VisualElement()
            {
                name = s_DropMarkerName
            };
            ResetDropMarker();
            m_DropMarker.AddToClassList( s_DropMarkerName );
            m_DropMarker.focusable = false;
        }

        public ReorderableListView(
            IList items,
            int itemHeight,
            Func< VisualElement > makeItem,
            Action< VisualElement, int > bindItem
        ) : this()
        {
            base.itemsSource = items;
            base.itemHeight = itemHeight;
            base.makeItem = makeItem;
            base.bindItem = bindItem;

            Refresh();
        }

        public new void Refresh()
        {
            base.Refresh();

            if( m_IsDragging )
            {
                m_ScrollView.Add( m_DropMarker );
                m_ScrollView.Add( m_DragContainer );
            }
        }

        public void ResetDropMarker()
        {
            m_DropMarker.style.height = 0;
            m_DropMarker.style.borderTopColor = s_DropMarkerColor;
            m_DropMarker.style.borderBottomColor = s_DropMarkerColor;
            m_DropMarker.style.borderLeftColor = s_DropMarkerColor;
            m_DropMarker.style.borderRightColor = s_DropMarkerColor;
            m_DropMarker.style.borderTopWidth = s_DropMarkerHeight / 2;
            m_DropMarker.style.borderBottomWidth = s_DropMarkerHeight / 2;
            m_DropMarker.style.borderLeftWidth = s_DropMarkerHeight / 2;
            m_DropMarker.style.borderRightWidth = s_DropMarkerHeight / 2;
        }

        private void CreateSelectionLists()
        {
            // need this for after a domain reload so the active ListView
            // selection is maintained by the ReorderableListView as well
            // foreach( var listItem in itemsSource )
            // {
            //     foreach( var itemId in currentSelectionIds )
            //     {
            //         if( listItem.id == itemId )
            //         {
            //             m_CurrentSelection.Add( listItem );
            //         }
            //     }
            // }
        }

        // internal override void OnViewDataReady()
        // {
        //     base.OnViewDataReady();

        //     string key = GetFullHierarchicalViewDataKey();

        //     OverwriteFromViewData(this, key);

        //     Refresh();
        // }

        private void UpdateDebugInfo( Vector2 localMousePos )
        {
            localMousePos = GetLocalMousePos( localMousePos );

            var text =
$@"Debug Info:
NumItems = { ( itemsSource != null ? itemsSource.Count : 0 ) }
MouseDown = Prev: { m_IsPrevMouseDown }, Current: { m_IsMouseDown }
IsDragging = { m_IsDragging }
Selection.Count = { ( m_SelectedIndices == null ? 0 : m_SelectedIndices.Count ) }
LocalMousePos = { GetLocalMousePos( localMousePos ) }
HoveredIndex = { GetIndexUnderPos( localMousePos ) }
HoveredIndex Selected = { m_SelectedIndices.Contains( GetIndexUnderPos( localMousePos ) ) }
ScrollOffset = { m_ScrollView.scrollOffset }
Scrolling = { m_Scrolling }
DropMarkerLocalPos = { m_DropMarker.localBound.y }
DragContainerPos = { m_DragContainer.transform.position.y }";

            Debug.Log( text );
        }

        private void OnSizeChanged( GeometryChangedEvent evt )
        {
            Refresh();
        }

        private void OnChangeEvent( ChangeEvent< object > evt )
        {
            Debug.Log("changed event");

            Refresh();
        }

        public void SetSelection( List< object > selectedItems )
        {
            if( selectedItems == null || selectedItems.Count == 0 )
            {
                ClearSelection();

                return;
            }

            // set single selection first instead of just clearing so that
            // the selection range origin is set in ListView
            var index = itemsSource.IndexOf( selectedItems[ 0 ] );
            SetSelection( index );

            for( int i = 1; i < selectedItems.Count; ++i )
            {
                var item = selectedItems[ i ];
                AddToSelection( itemsSource.IndexOf( item ) );
            }
        }

        public void SetSelection( List< int > selectedIndices )
        {
            if( selectedIndices == null || selectedIndices.Count == 0 )
            {
                ClearSelection();
                
                return;
            }

            SetSelection( selectedIndices[ 0 ] );

            for( int i = 1; i < selectedIndices.Count; ++i )
            {
                AddToSelection( selectedIndices[ i ] );
            }
        }

        /*=========================================================================================
            
            Mouse Callbacks

        ==========================================================================================*/

        private void OnClick( MouseDownEvent evt )
        {
            if( evt.button != 0 )
            {
                return;
            }

            if( evt.clickCount == 2 )
            {
                return; // let the double-click event pass through to the ListView OunClick
            }
            
            if( m_IsDragging )
            {
                evt.StopImmediatePropagation();

                return;
            }

            m_IsPrevMouseDown = m_IsMouseDown;

            Vector2 localMousePos = GetLocalMousePos( evt.localMousePosition );
            int clickedIndex = GetIndexUnderPos( localMousePos );

            if( !m_IsMouseDown && !evt.ctrlKey && !evt.shiftKey )
            {
                m_IsMouseDown = true;
                m_DragStartPos = localMousePos;

                if( m_SelectedIndices.Contains( clickedIndex ) )
                {
                    evt.StopImmediatePropagation();
                }
            }
        }

        private void OnDrag( MouseMoveEvent evt )
        {
            m_Scrolling = false;

            if( evt.button != 0 || !m_IsMouseDown )
            {
                return;
            }

            m_IsPrevMouseDown = m_IsMouseDown;

            Vector2 localMousePos = GetLocalMousePos( evt.localMousePosition );
            int dropIndex = GetIndexUnderPos( localMousePos );

            if( m_IsMouseDown && !m_IsDragging && !evt.ctrlKey && !evt.shiftKey &&
                Mathf.Abs( localMousePos.y - m_DragStartPos.y ) > s_MinDragDistance )
            {
                m_IsDragging = true;

                m_ScrollView.Add( m_DropMarker );

                m_DragContainer.Clear();

                Vector2 cascadeOffset = new Vector2( 5f, 20f );
                int count = Mathf.Min( 5, m_SelectedIndices.Count - 1 );
                float containerWidth = m_ScrollView.contentContainer.localBound.width - s_DragContainerPaddingX * 2;
                float containerHeight = itemHeight + count * cascadeOffset.y + s_DragContainerBorderWidth * 2;
                m_DragContainer.style.width = containerWidth;
                m_DragContainer.style.height = containerHeight;

                // make the invisible dummy elements to replace the dragged selection in the listview
                // grab the selection and add it to the drag container. add them
                // in reverse order so that the top most one renders in front
                for( int i = count; i >= 0; --i )
                {
                    var view = MakeListItem();
                    view.style.height = itemHeight;
                    view.style.backgroundColor = this.style.backgroundColor;
                    view.style.opacity = 1f;
                    view.style.width = containerWidth - s_DragContainerBorderWidth * 2;
                    view.style.position = Position.Absolute;

                    // this kind of acts as a copy of the element
                    BindListItem( view.Q( s_ItemContentContainerName ), m_SelectedIndices[ i ] );
                    
                    m_DragContainer.Add( view );
                    
                    Vector3 pos = view.transform.position;
                    pos.x = 0;
                    pos.y = i * cascadeOffset.y;
                    view.transform.position = pos;
                }

                m_ScrollView.Add( m_DragContainer );

                // this.CaptureMouse();
                // evt.StopImmediatePropagation();
            }

            if( m_IsDragging )
            {
                HandleDragAndScroll( localMousePos );
                UpdateDropMarkerPosition( localMousePos );

                // TODO(wyatt): shuffle the listview around and show the highlighted region
                //              where the current selection will be dropped in the ListView

                UpdateDragContainerPosition( evt.localMousePosition );

                evt.StopImmediatePropagation();

                onSelectionDrag?.Invoke( dropIndex, evt );
            }
        }

        private void UpdateDragContainerPosition( Vector2 localMousePosition )
        {
            Rect dragContainerRect = CalculateDragContainerPosition( localMousePosition.x, localMousePosition.y, 0, 0 );
            m_DragContainer.transform.position = new Vector2( dragContainerRect.x + s_DragContainerPaddingX, dragContainerRect.y - itemHeight / 2 );
            m_DragContainer.BringToFront(); // always need to call this
        }

        private void OnDrop( MouseUpEvent evt )
        {
            if( evt.button != 0 )
            {
                return;
            }

            m_IsPrevMouseDown = m_IsMouseDown;
            m_IsMouseDown = false;

            int dropIndex = GetDropMarkerIndexUnderPos( GetLocalMousePos( evt.localMousePosition ) );

            if( m_IsDragging )
            {
                m_IsDragging = false;

                // remove auxiliary drag n' drop visual elements
                m_ScrollView.Remove( m_DropMarker );
                m_ScrollView.Remove( m_DragContainer );
                m_DragContainer.Clear();

                onBeforeReorder?.Invoke( dropIndex, evt );

                int selectionCount = this.selectionCount;

                m_SelectedIndices.Sort();

                var currentSelection = GetSelectedItems();

                // remove the selected items from the list of item wrappers
                for( int i = 0; i < selectionCount; ++i )
                {
                    itemsSource.Remove( currentSelection[ i ] );
                }

                int offset = 0;
                // user could be dropping the selection in a range between two selected items
                // so count the number of selected items that come before the drop index
                for( int i = 0; i < selectionCount && m_SelectedIndices[ i ] < dropIndex; ++i )
                {
                    offset++;
                }

                dropIndex = dropIndex - offset;
                int endRange = dropIndex + selectionCount;

                for( int i = dropIndex; i < endRange; ++i )
                {
                    itemsSource.Insert( i, currentSelection[ i - dropIndex ] );
                }

                // need to refresh the list so that the new ordering gets built in the ListView
                Refresh();

                // update the selection of the ListView so that the items that were dropped
                // are still considered the active selection
                ClearSelection();

                base.SetSelection( dropIndex );

                for( int index = dropIndex + 1; index < endRange; ++index )
                {
                    // TODO(wyatt): mark the dropped items as being the active selection
                    base.AddToSelection( index );
                }

                onReordered?.Invoke( dropIndex, currentSelection );

                // this.ReleaseMouse();
                evt.StopImmediatePropagation();
            }
            else
            {
                // set the selection if the user clicks and releases the mouse on a single item
                // without dragging. assume they want to select that item
                if( !evt.shiftKey && !evt.ctrlKey && m_SelectedIndices.Contains( dropIndex ) )
                {
                    selectedIndex = dropIndex;
                }
            }
        }

        private void OnScroll( WheelEvent evt )
        {
            UpdateDragContainerPosition( evt.localMousePosition );
            UpdateDropMarkerPosition( GetLocalMousePos( evt.localMousePosition ) );
        }

        private void OnRightClick( MouseDownEvent evt )
        {
            if( evt.button != 1 )
            {
                return;
            }

            if( m_IsDragging )
            {
                evt.StopImmediatePropagation();
                return;
            }

            onItemRightClick?.Invoke( GetSelectedItems() );

            evt.StopImmediatePropagation();
        }

        /*=========================================================================================
            
            Selection Functions

        ==========================================================================================*/

        private List< object > GetSelectedItems()
        {
            List< object > ret = new List<object>( m_SelectedIndices.Count );

            foreach( var index in m_SelectedIndices )
            {
                ret.Add( itemsSource[ index ] );
            }

            return ret;
        }

        // since ListView.currentSelectedIds is internal, get the ids from the new selection list here
        private void OnSelectionChanged( List< object > items )
        {
            m_SelectedIndices.Clear();

            // add to selection
            foreach( var item_ in items )
            {
                var item = item_;

                // int index = GetIndexUnderPos( GetLocalMousePos( item.userView.parent.parent.localBound.min ) );
                int index = itemsSource.IndexOf( item );

                Debug.Assert( index >= 0, "Selected index should not be less than zero" );

                m_SelectedIndices.Add( index );
            }
        }

        public Vector2 GetLocalMousePos( Vector2 pos )
        {
            return pos - Vector2.up * m_ScrollView.scrollOffset.y;
        }

        private Vector2 GetPositionInScrollView( Vector2 pos )
        {
            return pos + Vector2.up * m_ScrollView.scrollOffset.y;
        }

        private int GetIndexUnderPos( Vector2 pos )
        {
            return ( int )( ( float )( pos.y + m_ScrollView.scrollOffset.y ) / ( float )itemHeight );
        }

        private int GetDropMarkerIndexUnderPos( Vector2 pos )
        {
            int index = GetIndexUnderPos( pos );

            float viewPosY = GetDropMarkerPos( index );

            if( pos.y > -m_ScrollView.scrollOffset.y + viewPosY + ( float )itemHeight * .5f )
            {
                index++;
            }

            return index;
        }

        private void HandleDragAndScroll( Vector2 localMousePos )
        {
            bool scrollUp = localMousePos.y < m_ScrollView.localBound.yMin + 20;
            bool scrollDown = localMousePos.y > m_ScrollView.localBound.yMax - 20;
            
            m_Scrolling = scrollUp || scrollDown;
            
            if( m_Scrolling )
            {
                m_ScrollView.scrollOffset = m_ScrollView.scrollOffset + ( scrollUp ? Vector2.down : Vector2.up ) * panSpeed.y;
            }
        }

        /*=========================================================================================
            
            Drop Marker Functions

        ==========================================================================================*/

        private void UpdateDropMarkerPosition( Vector2 pos )
        {
            // copy position to get the x component
            Vector3 dropMarkerPos = m_DropMarker.transform.position;
            
            int index = GetDropMarkerIndexUnderPos( pos );
            dropMarkerPos.y = GetDropMarkerPos( index );
            
            m_DropMarker.transform.position = dropMarkerPos;

            // need to send drag marker to front because of element recycling in listview
            m_DropMarker.BringToFront();
        }

        private float GetDropMarkerPos( int index )
        {
            return index * itemHeight - ( float )s_DropMarkerHeight / 2;
        }

        private Rect CalculateDragContainerPosition( float x, float y, float width, float height )
        {
            var rect = new Rect( x, y, width, height );

            if ( dragMovementType == DragMovementType.ClampToParentEdges )
            {
                Rect contentRect = m_ScrollView.hierarchy.parent.contentRect;
                rect.x = Mathf.Max( rect.x, contentRect.xMin );
                rect.x = Mathf.Min( rect.x, contentRect.xMax - rect.width );
                rect.y = Mathf.Max( rect.y, contentRect.yMin );
                rect.y = Mathf.Min( rect.y, contentRect.yMax - rect.height );
            }
            else if( dragMovementType == DragMovementType.FitToParentEdges )
            {
                Rect contentRect = m_ScrollView.hierarchy.parent.contentRect;
                rect.x = contentRect.xMin;
                rect.y = y;
            }
            else if( dragMovementType == DragMovementType.Free )
            {
                // do nothing?
            }

            // Reset size, we never intended to change them in the first place
            rect.width = width;
            rect.height = height;

            return rect;
        }

        /*=========================================================================================
            
            ReorderableListView Item Functions

        ==========================================================================================*/

        // TODO(wyatt): could probably move this out of the ReorderableListView implementation
        private VisualElement MakeListItemContainer()
        {
            var itemContainer = new VisualElement()
            {
                name = s_ItemName,
                style = 
                {
                    flexDirection = FlexDirection.Row
                }
            };
            itemContainer.styleSheets.Add( Resources.Load< StyleSheet >( s_ItemUssPath ) );

            var sidebar = new VisualElement()
            {
                name = s_ItemSidebar
            };
            sidebar.AddToClassList( s_ItemSidebar );

            var gripperIcon = new VisualElement()
            {
                name = s_ItemGripperName
            };
            gripperIcon.AddToClassList( s_ItemGripperName );
            sidebar.hierarchy.Add( gripperIcon );

            itemContainer.hierarchy.Add( sidebar );

            // now create the user defined visual element, ie. layers
            var userContainer = new VisualElement()
            {
                name = s_ItemContentContainerName,
                style =
                {
                    flexGrow = 1
                }
            };
            userContainer.AddToClassList( s_ItemContentContainerName );

            itemContainer.Add( userContainer );

            return itemContainer;
        }

        private VisualElement MakeListItem()
        {
            var itemContainer = MakeListItemContainer();

            var userContainer = itemContainer.Q( s_ItemContentContainerName );

            if( this.makeItem != null )
            {
                var view = this.makeItem();

                if( view != null )
                {
                    userContainer.Add( this.makeItem() );
                }
            }
            
            return itemContainer;
        }

        private void BindListItem( VisualElement listViewElement, int index )
        {
            if( this.bindItem == null )
            {
                return;
            }

            this.bindItem( listViewElement.Q( s_ItemContentContainerName ), index );
        }

        private void OnCustomStyleResolved( CustomStyleResolvedEvent e )
        {
            var oldHeight = itemHeight;
            int height = 0;
            // if (!m_ListView.m_ItemHeightIsInline && e.customStyle.TryGetValue(ListView.s_ItemHeightProperty, out height))
            //     m_ListView.m_ItemHeight = height;

            // if (m_ListView.m_ItemHeight != oldHeight)
            //     m_ListView.Refresh();

            //if( e.customStyle.TryGetValue( ListView.s_ItemHeightProperty, out height ) )
            {
                if( height != oldHeight )
                {
                    Refresh();
                }
            }
        }
    }
}