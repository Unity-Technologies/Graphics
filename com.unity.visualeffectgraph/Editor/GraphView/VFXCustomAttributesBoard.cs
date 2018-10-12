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

        static readonly Rect defaultRect = new Rect(300, 100, 300, 300);
        public VFXCustomAttributesBoard(VFXView view):base(view,BoardPreferenceHelper.Board.customAttributeBoard,defaultRect)
        {
            title = "Custom Attributes";
            subTitle = "List your own per particle attributes";

            AddStyleSheetPath("VFXCustomAttributesBoard");
            
            SetPosition(BoardPreferenceHelper.LoadPosition(BoardPreferenceHelper.Board.customAttributeBoard, defaultRect));

            clippingOptions = ClippingOptions.ClipContents;

            var header = this.Q("header");


            var add = new Button { name = "addButton", text = "+" };
            add.clickable.clicked += ()=>
            {
                m_Attributes.Add(new Attribute { name = "new attribute", type = VFXValueType.Float });
                SyncParameters();
            };
            header.Add(add);
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
            m_Attributes[index] = new Attribute { name = name, type = m_Attributes[index].type };
            SyncParameters();
        }

        public void SyncParameters()
        {
            while( m_Rows.Count > m_Attributes.Count)
            {
                m_Rows.Last().RemoveFromHierarchy();
                m_Rows.RemoveAt(m_Rows.Count - 1);
            }

            while(m_Rows.Count < m_Attributes.Count)
            {
                var row = new VFXBoardAttribute(m_Rows.Count);

                Add(row);

                m_Rows.Add(row);
            }

            foreach(var row in m_Rows)
            {
                row.Update();
            }

            //Update row
        }
    }
}
