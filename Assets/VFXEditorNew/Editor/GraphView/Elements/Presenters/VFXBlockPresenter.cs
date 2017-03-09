using System;
using RMGUI.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class VFXBlockPresenter : NodePresenter
    {
		protected new void OnEnable()
		{
			capabilities |= Capabilities.Selectable | Capabilities.Droppable | Capabilities.Movable;

            // Most initialization will be done in Init
		}

        VFXDataInputAnchorPresenter AddDataAnchor(PropertyInfo prop)
        {
            VFXDataInputAnchorPresenter anchorPresenter = CreateInstance<VFXDataInputAnchorPresenter>();
            anchorPresenter.Init(Model, this, prop);
            ContextPresenter.ViewPresenter.RegisterDataAnchorPresenter(anchorPresenter);

            return anchorPresenter;
        }

        public void Init(VFXBlock model,VFXContextPresenter contextPresenter)
        {
            m_Model = model;
            m_ContextPresenter = contextPresenter;


            //TODO unregister when the block is destroyed
            model.onInvalidateDelegate += OnInvalidate;

            OnInvalidate(model, VFXModel.InvalidationCause.kParamChanged);
        }

        private void OnInvalidate(VFXModel model, VFXModel.InvalidationCause cause)
        {
            if( model is VFXBlock)
            {
                VFXBlock block = model as VFXBlock;


                var inputs = inputAnchors;
                inputs.Clear();
                Dictionary<string, VFXDataInputAnchorPresenter> newAnchors = new Dictionary<string, VFXDataInputAnchorPresenter>();

                foreach ( var property in GetProperties())
                {
                    var prop = property;
                    VFXDataInputAnchorPresenter propPresenter = GetPropertyPresenter(ref prop);

                    if( propPresenter == null)
                    {
                        propPresenter = AddDataAnchor(property);
                    }
                    newAnchors[property.path] = propPresenter;

                    propPresenter.UpdateInfos(ref prop);
                    inputs.Add(propPresenter);
                }
                m_Anchors = newAnchors;
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


        public VFXDataInputAnchorPresenter GetPropertyPresenter(ref PropertyInfo info)
        {
            VFXDataInputAnchorPresenter result = null;

            m_Anchors.TryGetValue(info.path, out result);

            return result;
        }

        [SerializeField]
        public struct PropertyInfo
        {
            public string name;
            public object value;
            public System.Type type;
            public bool expandable;
            public bool expanded;
            public int depth;
            public string parentPath;

            public string path { get { return !string.IsNullOrEmpty(parentPath)?parentPath + "." + name : name; } }
        }

        public void PropertyValueChanged(VFXDataAnchorPresenter presenter,object newValue)
        {
            //TODO undo/redo

            string[] fields = presenter.path.Split(new char[] { '.' },StringSplitOptions.RemoveEmptyEntries);


            var properties = m_Model.GetProperties();
            var buffers = m_Model.GetCurrentPropertiesValues();


            int index = System.Array.FindIndex(properties, t => t.name == fields[0]);
            var prop = properties[index];
            var buffer = buffers[index];

            List<object> stack= new List<object>();

            stack.Add(buffer);

            for (int i = 1; i < fields.Length; ++i)
            {
                object current = stack[i-1];
                FieldInfo fi = current.GetType().GetField(fields[i]);

                stack.Add(fi.GetValue(current));
            }
            stack[stack.Count - 1] = newValue;

            for (int i = stack.Count -1 ; i > 0 ; --i)
            {
                object current = stack[i];
                object prev = stack[i - 1];

                FieldInfo fi = prev.GetType().GetField(fields[i]);

                fi.SetValue(prev, current);
            }

            buffers[index] = stack[0];

            m_Model.Invalidate(VFXModel.InvalidationCause.kParamChanged);


            foreach(var anchorPresenter in m_Anchors.Values)
            {
                // update child and parents.
                if( anchorPresenter.path.StartsWith(presenter.path) || presenter.path.StartsWith(anchorPresenter.path))
                {
                    anchorPresenter.Dirty();
                }
            }

        }


        public event System.Action<VFXBlockPresenter> OnParamChanged;

        public void ExpandPath(string fieldPath)
        {
            //TODO undo/redo
            m_Model.ExpandPath(fieldPath);
            
            m_DirtyHack = !m_DirtyHack;
        }

        public void RetractPath(string fieldPath)
        {
            //TODO undo/redo
            m_Model.RetractPath(fieldPath);

            m_DirtyHack = !m_DirtyHack;
        }

        public IEnumerable<PropertyInfo> GetProperties()
        {
            var properties = m_Model.GetProperties();
            var values = m_Model.GetCurrentPropertiesValues();

            for(int i = 0; i < properties.Count(); ++i)
            {
                PropertyInfo info = new PropertyInfo()
                {
                    type = properties[i].type,
                    name = properties[i].name,
                    value = values[i],
                    parentPath = "",
                    expandable = IsTypeExpandable(properties[i].type),
                    expanded = m_Model.IsPathExpanded(properties[i].name),
                    depth = 0
                };

                yield return info;

                if (info.expanded)
                {
                    foreach (var subField in GetProperties(info.type, info.value, info.name, 1))
                    {
                        yield return subField;
                    }
                }
            }
        }

        public IEnumerable<PropertyInfo> GetProperties(string fieldPath)
        {
            string[] fields = fieldPath.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            var properties = m_Model.GetProperties();
            var buffers = m_Model.GetCurrentPropertiesValues();


            int index = System.Array.FindIndex(properties, t => t.name == fields[0]);
            var prop = properties[index];
            var buffer = buffers[index];

            Type current = prop.type;
            for (int i = 1; i < fields.Length; ++i)
            {
                FieldInfo fi = current.GetField(fields[i]);

                current = fi.FieldType;
                buffer = fi.GetValue(buffer);
            }

            return GetProperties(current, buffer, fieldPath, fields.Length);
        }


        bool IsTypeExpandable(System.Type type)
        {
            return !type.IsPrimitive && !typeof(Object).IsAssignableFrom(type) && type != typeof(AnimationCurve) && ! type.IsEnum;
        }



        bool ShouldSkipLevel(Type type)
        {
            return typeof(Spaceable).IsAssignableFrom(type) && type.GetFields().Length == 2; // spaceable having only one member plus their space member.
        }


        bool ShouldIgnoreMember(Type type, FieldInfo field)
        {
            return typeof(Spaceable).IsAssignableFrom(type) && field.Name == "space";
        }

        private IEnumerable<PropertyInfo> GetProperties(Type type, object value, string prefix, int depth)
        {
            if (type == null)
                yield break;

            FieldInfo[] infos = type.GetFields(BindingFlags.Public|BindingFlags.Instance);


            if( ShouldSkipLevel(type) )
            {
                foreach (var field in infos)
                {
                    if( ShouldIgnoreMember(type,field))
                        continue;

                    object fieldValue = field.GetValue(value);
                    string fieldPath = string.IsNullOrEmpty(prefix)? field.Name:prefix + "." + field.Name;

                    foreach (var subField in GetProperties(field.FieldType, fieldValue, fieldPath, depth + 1))
                    {
                        yield return subField;
                    }
                }
                yield break;
            }

            foreach (var field in infos)
            {
                if( ShouldIgnoreMember(type,field))
                    continue;

                object fieldValue = field.GetValue(value);


                string fieldPath = string.IsNullOrEmpty(prefix)? field.Name:prefix + "." + field.Name;
                bool expanded = m_Model.IsPathExpanded(fieldPath);

                yield return new PropertyInfo()
                {
                    name = field.Name,
                    value = fieldValue,
                    type = field.FieldType,
                    expandable = IsTypeExpandable(field.FieldType),
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
