using System;
using RMGUI.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class VFXNodeBlockPresenter : GraphElementPresenter
    {
		protected new void OnEnable()
		{
			capabilities |= Capabilities.Selectable | Capabilities.Droppable | Capabilities.Movable;


		}

        public VFXBlock Model
        {
            get { return m_Model; }
            set {

                if (m_Model != value)
                {
                    m_Model = value;
                }
            }
        }

        public Type GetPropertiesType()
        {
            return m_Model.Desc.GetPropertiesType();
        }

        public object GetCurrentPropertiesValues()
        {
            return m_Model.GetCurrentPropertiesValue();
        }


        public struct PropertyInfo
        {
            public string name;
            public object value;
            public Type type;
            public bool expandable;
            public bool expanded;
            public int depth;
            public string parentPath;

            public string path { get { return !string.IsNullOrEmpty(parentPath)?parentPath + "." + name : name; } }
        }

        public void PropertyValueChanged(ref PropertyInfo info)
        {
            //TODO apply change

            string[] fields = info.parentPath.Split(new char[] { '.' },StringSplitOptions.RemoveEmptyEntries);

            object buffer = GetCurrentPropertiesValues();
            Type type = GetPropertiesType();

            List<object> stack= new List<object>();
            stack.Add(buffer);

            for (int i = 0; i < fields.Length; ++i)
            {
                object current = stack[i];
                FieldInfo fi = current.GetType().GetField(fields[i]);

                stack.Add(fi.GetValue(current));
            }
            FieldInfo fieldInfo = stack[stack.Count - 1].GetType().GetField(info.name);
            fieldInfo.SetValue(stack[stack.Count - 1], info.value);

            for (int i = fields.Length-1; i > 0 ; --i)
            {
                object current = stack[i];
                object prev = stack[i - 1];

                FieldInfo fi = prev.GetType().GetField(fields[i-1]);

                fi.SetValue(prev, current);
            }

        }
        public void ExpandPath(string fieldPath)
        {
            m_Model.ExpandPath(fieldPath);
            
        }

        public void RetractPath(string fieldPath)
        {
            m_Model.RetractPath(fieldPath);
        }

        public IEnumerable<PropertyInfo> GetProperties()
        {
            foreach (var prop in GetProperties(m_Model.Desc.GetPropertiesType(), GetCurrentPropertiesValues(), "", 0))
            {
                yield return prop;
            }
        }

        private IEnumerable<PropertyInfo> GetProperties(Type type, object value, string prefix, int depth)
        {
            FieldInfo[] infos = type.GetFields(BindingFlags.Public|BindingFlags.Instance);

            foreach (var field in infos)
            {
                object fieldValue = field.GetValue(value);


                string fieldPath = string.IsNullOrEmpty(prefix)? field.Name:prefix + "." + field.Name;
                bool expanded = m_Model.IsPathExpanded(fieldPath);

                yield return new PropertyInfo()
                {
                    name = field.Name,
                    value = fieldValue,
                    type = field.FieldType,
                    expandable = !field.FieldType.IsPrimitive && ! typeof(Object).IsAssignableFrom(field.FieldType),
                    expanded = expanded,
                    depth = depth,
                    parentPath = prefix
                };
                if (expanded)
                {
                    foreach (var subField in GetProperties(field.FieldType, fieldValue, fieldPath, depth + 1))
                    {
                        yield return subField;
                    }
                }
            }
        }

        [SerializeField]
        private VFXBlock m_Model;
    }
}
