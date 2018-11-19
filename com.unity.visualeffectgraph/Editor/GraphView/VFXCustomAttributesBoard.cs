using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.UI
{
    public class VFXBoardAttributeUIFactory : UxmlFactory<VFXBoardAttribute>
    { }
    public class VFXBoardAttribute : VisualElement, IPropertyRMProvider
    {
        int             m_Index;

        public TextField m_NameField;
        public Label m_TypeButton;

        VisualElement m_ValueContainer;
        PropertyRM m_DefaultValue;

        public void SetIndex(int index)
        {
            m_Index = index;
        }

        public VFXBoardAttribute(int index)
        {
            m_Index = index;

            var tpl = Resources.Load<VisualTreeAsset>("uxml/VFXBoard-attribute");
            this.AddStyleSheetPathWithSkinVariant("VFXControls");
            this.AddStyleSheetPath("VFXBoardAttribute");

            tpl.CloneTree(this);

            m_NameField = this.Q<TextField>("name");
            m_NameField.RegisterCallback<ChangeEvent<string>>(OnNameChange);
            m_NameField.isDelayed = true;
            m_TypeButton = this.Q<Label>("type-dropdown");
            m_TypeButton.AddManipulator(new DownClickable(ShowMenu));
            m_ValueContainer = this.Q("value-container");
        }
        void ShowMenu()
        {
            VFXCustomAttributesBoard attributes = GetFirstAncestorOfType<VFXCustomAttributesBoard>();
            if (attributes == null)
                return;

            var genericMenu = new GenericMenu();
            int selectedIndex = -1;

            VFXValueType type = attributes.GetAttributeType(m_Index);


            for(VFXValueType t = VFXValueType.Float; t <= VFXValueType.Uint32; ++t)
            {
                genericMenu.AddItem(new GUIContent(t.ToString()), t == type, OnSelectType, t);
                if (type == t)
                    selectedIndex = genericMenu.GetItemCount() - 1;
            }

            genericMenu.Popup(m_TypeButton.worldBound, selectedIndex);
        }

        void OnNameChange(ChangeEvent<string> e)
        {
            VFXCustomAttributesBoard attributes = GetFirstAncestorOfType<VFXCustomAttributesBoard>();
            if (attributes == null)
                return;
            attributes.SetAttributeName(m_Index, m_NameField.value);
        }

        void OnSelectType(object t)
        {
            VFXCustomAttributesBoard attributes = GetFirstAncestorOfType<VFXCustomAttributesBoard>();
            if (attributes == null)
                return;
            attributes.SetAttributeType(m_Index, (VFXValueType)t);



        }

        public void Update()
        {
            VFXCustomAttributesBoard attributes = GetFirstAncestorOfType<VFXCustomAttributesBoard>();
            if (attributes == null)
                return;
            m_NameField.SetValueWithoutNotify(attributes.GetAttributeName(m_Index));
            m_TypeButton.text = attributes.GetAttributeType(m_Index).ToString();


            if(m_DefaultValue == null || ! m_DefaultValue.IsCompatible(this) )
            {
                if( m_DefaultValue != null )
                    m_DefaultValue.RemoveFromHierarchy();

                m_DefaultValue = PropertyRM.Create(this,30);
                m_DefaultValue.isDelayed = true;
                m_ValueContainer.Add(m_DefaultValue);
                m_DefaultValue.Update();
            }
            else
            {
                m_DefaultValue.UpdateValue();
            }
        }


        bool IPropertyRMProvider.expanded {get {return false;}}

        bool IPropertyRMProvider.expandable { get { return false; } }

        object IPropertyRMProvider.value { 
            get
            {
                VFXCustomAttributesBoard attributes = GetFirstAncestorOfType<VFXCustomAttributesBoard>();
                return attributes.GetAttributeDefaultValue(m_Index);
            }
            set
            {
                VFXCustomAttributesBoard attributes = GetFirstAncestorOfType<VFXCustomAttributesBoard>();
                attributes.SetAttributeDefaultValue(m_Index,value);
            }
        }

        bool IPropertyRMProvider.spaceableAndMasterOfSpace { get {return false;} }

        VFXCoordinateSpace IPropertyRMProvider.space { get {throw new NotImplementedException();} set {throw new NotImplementedException();} }

        string IPropertyRMProvider.name {get {return "Value";} }

        VFXPropertyAttribute[] IPropertyRMProvider.attributes {get{return null;}}

        object[] IPropertyRMProvider.customAttributes { get { return null; } }

        Type IPropertyRMProvider.portType
        {
            get
            {
                VFXCustomAttributesBoard attributes = GetFirstAncestorOfType<VFXCustomAttributesBoard>();
                return VFXExpression.TypeToType(attributes.GetAttributeType(m_Index));
            }
        }

        int IPropertyRMProvider.depth { get{return 0;} }

        bool IPropertyRMProvider.editable { get { return true; } }

        VFXGraph IPropertyRMProvider.graph
        {
            get
            {
                VFXCustomAttributesBoard attributes = GetFirstAncestorOfType<VFXCustomAttributesBoard>();
                return attributes.controller.graph;
            }
        }

        bool IPropertyRMProvider.IsSpaceInherited()
        {
            return false;
        }

        void IPropertyRMProvider.RetractPath()
        {
            throw new NotImplementedException();
        }

        void IPropertyRMProvider.ExpandPath()
        {
            throw new NotImplementedException();
        }
    }

    class VFXCustomAttributesBoard : VFXBoard
    {
        List<VFXBoardAttribute> m_Rows = new List<VFXBoardAttribute>();

        class ReorderableList : VFXReorderableList
        {
            VFXCustomAttributesBoard m_Board;
            public ReorderableList(VFXCustomAttributesBoard board)
            {
                m_Board = board;
            }

            public override void OnAdd()
            {
                m_Board.controller.graph.AddCustomAttribute();
            }

            public override void OnRemove(int index)
            {
                string name = m_Board.controller.graph.GetCustomAttributeName(index);
                if (m_Board.controller.graph.HasCustomAttributeUses(name) && !EditorUtility.DisplayDialog("Deleting Custom Attribute","Are you sure you want to remove the custom attribute+'" + name + "' currently in use ?", "Ok", "Cancel"))
                {
                    return;
                }
                m_Board.controller.graph.RemoveCustomAttribute(index);
            }

            protected override void OnMoved(int movedIndex, int targetIndex)
            {
                // the reorderable list reorders the elements, lets put them back in the right order.
                var prev = m_Board.m_Rows[0];
                prev.SendToBack();
                for (int i = 1; i < m_Board.m_Rows.Count; ++i)
                {
                    m_Board.m_Rows[i].PlaceInFront(prev);
                    prev = m_Board.m_Rows[i];
                }
                m_Board.controller.graph.MoveCustomAttribute(movedIndex,targetIndex);
                Select(targetIndex);
            }
        }

        ReorderableList m_List;

        static readonly Rect defaultRect = new Rect(300, 100, 300, 300);
        public VFXCustomAttributesBoard(VFXView view):base(view,BoardPreferenceHelper.Board.customAttributeBoard,defaultRect)
        {
            title = "Custom Attributes";
            subTitle = "List your own per particle attributes";

            this.AddStyleSheetPath("VFXCustomAttributesBoard");
            
            SetPosition(BoardPreferenceHelper.LoadPosition(BoardPreferenceHelper.Board.customAttributeBoard, defaultRect));

            style.overflow = Overflow.Hidden;
            cacheAsBitmap = false;

            var header = this.Q("header");

            m_List = new ReorderableList(this);
            Add(m_List);
        }

        public new void Clear()
        {
            //m_List.Clear();
        }

        public VFXValueType GetAttributeType(int index)
        {
            return controller.graph.GetCustomAttributeType(index);
        }

        public object GetAttributeDefaultValue(int index)
        {
            return controller.graph.GetCustomAttributeDefaultValue(index);
        }

        public string GetAttributeName(int index)
        {
            return controller.graph.GetCustomAttributeName(index);
        }

        public void SetAttributeType(int index,VFXValueType type)
        {
            controller.graph.SetCustomAttributeType(index, type);
        }

        public void SetAttributeName(int index, string name)
        {
            controller.graph.SetCustomAttributeName(index, name);
        }

        public void SetAttributeDefaultValue(int index, object name)
        {
            controller.graph.SetCustomAttributeDefaultValue(index, name);
        }

        public override void OnControllerChanged(ref ControllerChangedEvent e)
        {
            if(e.controller == controller)
            {
                SyncParameters();
            }
        }

        public void SyncParameters()
        {
            int attributeCount = controller.graph.GetCustomAttributeCount();
            while( m_Rows.Count > attributeCount)
            {
                m_List.RemoveItemAt(m_Rows.Count - 1);
                m_Rows.RemoveAt(m_Rows.Count - 1);
            }

            while(m_Rows.Count < attributeCount)
            {
                var row = new VFXBoardAttribute(m_Rows.Count);

                m_List.AddItem(row);

                m_Rows.Add(row);

                m_List.Select(row);
            }

            foreach(var row in m_Rows)
            {
                row.Update();
            }

            //Update row
        }
    }
}
