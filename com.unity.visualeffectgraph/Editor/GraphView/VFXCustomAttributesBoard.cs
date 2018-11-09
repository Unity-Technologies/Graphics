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
    public class VFXBoardAttribute : VisualElement
    {
        int             m_Index;

        public TextField m_NameField;
        public Label m_TypeButton;


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

        struct Attribute
        {
            public string name;
            public VFXValueType type;
        }

        public new void Clear()
        {
            //m_List.Clear();
        }

        public VFXValueType GetAttributeType(int index)
        {
            return controller.graph.GetCustomAttributeType(index);
        }
        public string GetAttributeName(int index)
        {
            return controller.graph.GetCustomAttributeName(index);
        }

        public void SetAttributeType(int index,VFXValueType type)
        {
            controller.graph.SetCustomAttributeType(index, type);
        }

        public void SetAttributeName(int index,string name)
        {
            controller.graph.SetCustomAttributeName(index, name);
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
