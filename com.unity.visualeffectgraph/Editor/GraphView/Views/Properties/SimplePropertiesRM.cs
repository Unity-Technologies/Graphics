using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using EnumField = UnityEditor.VFX.UIElements.VFXEnumField;

namespace UnityEditor.VFX.UI
{
    abstract class ListPropertyRM<T, U> : PropertyRM<List<T>> where U : PropertyRM<T>
    {
        public ListPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_List = new VFXReorderableList();
        }

        VFXReorderableList m_List;

        public override float GetPreferredControlWidth()
        {
            return 150;
        }
        public override void UpdateGUI(bool force)
        {
            List<T> list = (List<T>)m_Provider.value;

            while(m_List.itemCount < list.Count)
            {
                m_List.AddItem(CreateNewItem(m_List.itemCount));
            }
            while(m_List.itemCount > list.Count)
            {
                m_List.RemoveItemAt(m_List.itemCount - 1);
            }

            for(int i = 0; i < list.Count; ++i)
            {
                (m_List.ItemAt(i) as U).UpdateGUI(force);
            }
        }


        class ItemProvider : IPropertyRMProvider
        {
            PropertyRM<List<T>> m_List;
            int m_Index;

            public ItemProvider(PropertyRM<List<T>> list, int index)
            {
                m_List = list;
                m_Index = index;
            }

            bool IPropertyRMProvider.expanded => false;

            bool IPropertyRMProvider.expandable => false;

            bool IPropertyRMProvider.expandableIfShowsEverything => false;

            object IPropertyRMProvider.value { get => ((List<T>)m_List.GetValue())[m_Index]; set => ((List<T>)m_List.GetValue())[m_Index] = (T)value; }

            bool IPropertyRMProvider.spaceableAndMasterOfSpace => false;

            VFXCoordinateSpace IPropertyRMProvider.space { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            string IPropertyRMProvider.name => m_Index.ToString();

            VFXPropertyAttribute[] IPropertyRMProvider.attributes => null;

            object[] IPropertyRMProvider.customAttributes => null;

            Type IPropertyRMProvider.portType => typeof(T);

            int IPropertyRMProvider.depth => 0;

            bool IPropertyRMProvider.editable => m_List.provider.editable;

            void IPropertyRMProvider.ExpandPath()
            {
                throw new NotImplementedException();
            }

            bool IPropertyRMProvider.IsSpaceInherited()
            {
                return false;
            }

            void IPropertyRMProvider.RetractPath()
            {
                throw new NotImplementedException();
            }
        }

        protected abstract U CreateField(IPropertyRMProvider provider);

        protected U CreateNewItem(int index)
        {
            U item = CreateField(new ItemProvider(this,index));

            return item;
        }

        protected override void UpdateEnabled()
        {
        }

        protected override void UpdateIndeterminate()
        {
        }

        public override bool showsEverything { get { return true; } }
    }




        class EnumPropertyRM : SimplePropertyRM<int>
    {
        public EnumPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            int min = 120;
            foreach (var str in Enum.GetNames(provider.portType))
            {
                Vector2 size = m_Field.Q<TextElement>().MeasureTextSize(str, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);

                size.x += 60;
                if (min < size.x)
                    min = (int)size.x;
            }
            if (min > 200)
                min = 200;


            return min;
        }

        public override ValueControl<int> CreateField()
        {
            var field = new EnumField(m_Label, m_Provider.portType);
            field.OnDisplayMenu = OnDisplayMenu;

            return field;
        }

        void OnDisplayMenu(EnumField field)
        {
            field.filteredOutValues = provider.filteredOutEnumerators;
        }
    }

    class Vector4PropertyRM : SimpleVFXUIPropertyRM<VFXVector4Field, Vector4>
    {
        public Vector4PropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            return 224;
        }
    }

    class Matrix4x4PropertyRM : SimpleVFXUIPropertyRM<VFXMatrix4x4Field, Matrix4x4>
    {
        public Matrix4x4PropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_FieldParent.style.flexDirection = FlexDirection.Row;
        }

        public override float GetPreferredControlWidth()
        {
            return 260;
        }
    }

    class Vector2PropertyRM : SimpleVFXUIPropertyRM<VFXVector2Field, Vector2>
    {
        public Vector2PropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            return 120;
        }
    }

    class FlipBookPropertyRM : SimpleVFXUIPropertyRM<VFXFlipBookField, FlipBook>
    {
        public FlipBookPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            return 100;
        }
    }
}
