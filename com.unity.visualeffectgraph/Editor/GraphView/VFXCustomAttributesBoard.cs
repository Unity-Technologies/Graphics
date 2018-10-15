using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements.GraphView;
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

        public VFXBoardAttribute(int index)
        {
            m_Index = index;

            var tpl = Resources.Load<VisualTreeAsset>("uxml/VFXBoard-attribute");
            this.AddStyleSheetPathWithSkinVariant("VFXControls");
            AddStyleSheetPath("VFXBoardAttribute");

            tpl.CloneTree(this, new Dictionary<string, VisualElement>());

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
        List<Attribute> m_Attributes = new List<Attribute>();

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
                string name = "Attribute";
                int cpt = 1;
                while (m_Board.m_Attributes.Any(t=>t.name == name))
                {
                    name = string.Format("Attribute{0}",cpt++);
                }
                m_Board.m_Attributes.Add(new Attribute() { name = name ,type = VFXValueType.Float});

                m_Board.SyncParameters();
            }

            public override void OnRemove(int index)
            {
                m_Board.m_Attributes.RemoveAt(index);
                m_Board.SyncParameters();
            }

            protected override void OnMoved(int movedIndex, int targetIndex)
            {
                Attribute attr = m_Board.m_Attributes[movedIndex];
                m_Board.m_Attributes.RemoveAt(movedIndex);
                if (movedIndex < targetIndex)
                    movedIndex--;
                m_Board.m_Attributes.Insert(targetIndex, attr);
                base.OnMoved(movedIndex, targetIndex);
                m_Board.SyncParameters();
            }
        }

        ReorderableList m_List;

        static readonly Rect defaultRect = new Rect(300, 100, 300, 300);
        public VFXCustomAttributesBoard(VFXView view):base(view,BoardPreferenceHelper.Board.customAttributeBoard,defaultRect)
        {
            title = "Custom Attributes";
            subTitle = "List your own per particle attributes";

            AddStyleSheetPath("VFXCustomAttributesBoard");
            
            SetPosition(BoardPreferenceHelper.LoadPosition(BoardPreferenceHelper.Board.customAttributeBoard, defaultRect));

            clippingOptions = ClippingOptions.ClipContents;

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
            m_Attributes.Clear();
        }


        public VFXValueType GetAttributeType(int index)
        {
            return m_Attributes[index].type;
        }
        public string GetAttributeName(int index)
        {
            return m_Attributes[index].name;
        }

        public void SetAttributeType(int index,VFXValueType type)
        {
            m_Attributes[index] = new Attribute {name = m_Attributes[index].name, type = type };
            SyncParameters();
        }

        public void SetAttributeName(int index,string name)
        {
            if( m_Attributes.Any(t=>t.name == name))
            {
                name = "Attribute";
                int cpt = 1;
                while (m_Attributes.Select((t,i) => t.name == name && i != index).Where(t=>t).Count() > 0)
                {
                    name = string.Format("Attribute{0}", cpt++);
                }
            }

            m_Attributes[index] = new Attribute { name = name, type = m_Attributes[index].type };
            SyncParameters();
        }

        public void SyncParameters()
        {
            while( m_Rows.Count > m_Attributes.Count)
            {
                m_List.RemoveItemAt(m_Rows.Count - 1);
                m_Rows.RemoveAt(m_Rows.Count - 1);
            }

            while(m_Rows.Count < m_Attributes.Count)
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
