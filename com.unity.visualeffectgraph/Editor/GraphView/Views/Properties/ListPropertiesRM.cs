using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.VFX;
using UnityEditor.VFX.UIElements;
using Object = UnityEngine.Object;
using Type = System.Type;
using EnumField = UnityEditor.VFX.UIElements.VFXEnumField;
using VFXVector2Field = UnityEditor.VFX.UI.VFXVector2Field;
using VFXVector4Field = UnityEditor.VFX.UI.VFXVector4Field;

namespace UnityEditor.VFX.UI
{
    abstract class ListPropertyRM<T, U> : PropertyRM<List<T>> where U : PropertyRM<T>
    {
        public ListPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            AddToClassList("ListPropertyRM");
            m_List = new ReorderableList(this);
            Add(m_List);
        }


        protected class ReorderableList : VFXReorderableList
        {
            ListPropertyRM<T, U> m_List;

            public ReorderableList(ListPropertyRM<T, U> list)
            {
                m_List = list;
            }
            public override void OnAdd()
            {
                m_List.OnAdd();
            }

            public override void OnRemove(int index)
            {
                m_List.OnRemove(index);
            }
        }

        void OnAdd()
        {
            T value = CreateItem();

            ((List<T>)m_Provider.value).Add(value);
            NotifyValueChanged();
            Update();
        }

        void OnRemove(int index)
        {
            ((List<T>)m_Provider.value).RemoveAt(index);
            NotifyValueChanged();
            Update();
        }

        protected ReorderableList m_List;

        public override float GetPreferredControlWidth()
        {
            return 150;
        }
        public override void UpdateGUI(bool force)
        {
            List<T> list = (List<T>)m_Provider.value;
            int itemCount = 0;
            if(list != null )
            {
                itemCount = list.Count;
            }
            while (m_List.itemCount < itemCount)
            {
                m_List.AddItem(CreateNewField(m_List.itemCount));
            }
            while (m_List.itemCount > itemCount)
            {
                m_List.RemoveItemAt(m_List.itemCount - 1);
            }

            for (int i = 0; i < itemCount; ++i)
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

        protected abstract T CreateItem();

        protected U CreateNewField(int index)
        {
            U item = CreateField(new ItemProvider(this, index));

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
}
