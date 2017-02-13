using System;
using RMGUI.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class VFXNodeBlockPresenter : GraphElementPresenter
    {
		protected new void OnEnable()
		{
			capabilities |= Capabilities.Selectable | Capabilities.Droppable | Capabilities.Movable;

            // Most initialization will be done in Init
		}

        void AddDataAnchor(PropertyInfo prop)
        {
            VFXDataInputAnchorPresenter anchorPresenter = CreateInstance<VFXDataInputAnchorPresenter>();
            anchorPresenter.Init(Model, prop.type);
            m_Anchors.Add(prop.path, anchorPresenter);
            ContextPresenter.ViewPresenter.RegisterDataAnchorPresenter(anchorPresenter);
        }

        public void Init(VFXBlock model,VFXContextPresenter contextPresenter)
        {
            m_Model = model;
            m_ContextPresenter = contextPresenter;

            foreach (var prop in GetProperties())
            {
                AddDataAnchor(prop);
            }
        }

        public VFXBlock Model
        {
            get { return m_Model; }
        }

        public VFXContextPresenter ContextPresenter
        {
            get { return m_ContextPresenter; }
        }

        public Type GetPropertiesType()
        {
            return m_Model.Desc.GetPropertiesType();
        }

        public object GetCurrentPropertiesValues()
        {
            return m_Model.GetCurrentPropertiesValue();
        }


        public VFXDataInputAnchorPresenter GetPropertyPresenter(ref PropertyInfo info)
        {
            VFXDataInputAnchorPresenter result = null;

            m_Anchors.TryGetValue(info.path, out result);

            return result;
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
            //TODO undo/redo

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

            m_DirtyHack = !m_DirtyHack;

        }
        public void ExpandPath(string fieldPath)
        {
            //TODO undo/redo
            m_Model.ExpandPath(fieldPath);

            foreach( var prop in GetProperties(fieldPath))
            {
                AddDataAnchor(prop);
            }
            m_DirtyHack = !m_DirtyHack;
        }

        public void RetractPath(string fieldPath)
        {
            //TODO undo/redo
            m_Model.RetractPath(fieldPath);


            var toRemove = m_Anchors.Keys.Where(t => t != fieldPath && t.StartsWith(fieldPath)).ToArray();

            foreach(var remove in toRemove)
            {
                m_Anchors.Remove(remove);
            }

            m_DirtyHack = !m_DirtyHack;
        }

        public IEnumerable<PropertyInfo> GetProperties()
        {
            return GetProperties(m_Model.Desc.GetPropertiesType(), GetCurrentPropertiesValues(), "", 0);
        }

        public IEnumerable<PropertyInfo> GetProperties(string fieldPath)
        {
            string[] fields = fieldPath.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            object buffer = GetCurrentPropertiesValues();
            Type type = GetPropertiesType();

            Type current = type;
            for (int i = 0; i < fields.Length; ++i)
            {
                FieldInfo fi = current.GetField(fields[i]);

                current = fi.FieldType;
                buffer = fi.GetValue(buffer);
            }

            return GetProperties(current, buffer, fieldPath, fields.Length);
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



        public override IEnumerable<GraphElementPresenter> allChildren
        {
            get {
                foreach (var kv in m_Anchors)
                {
                    yield return kv.Value;
                }
            }
        }
        [SerializeField]
        bool m_DirtyHack;//because serialization doesn't work with the below dictionary

        [SerializeField]
        private Dictionary<string, VFXDataInputAnchorPresenter> m_Anchors = new Dictionary<string, VFXDataInputAnchorPresenter>();

        [SerializeField]
        private VFXBlock m_Model;


        VFXContextPresenter m_ContextPresenter;
    }
}
