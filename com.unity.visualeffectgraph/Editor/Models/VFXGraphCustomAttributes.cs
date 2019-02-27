//#define USE_SHADER_AS_SUBASSET
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.VFX;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Profiling;
using System.Reflection;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    partial class VFXGraph : VFXModel
    {
        [Serializable]
        public struct CustomAttribute
        {
            public string name;
            public VFXValueType type;
            public VFXSerializableObject defaultValue;
        }

        [SerializeField]
        List<CustomAttribute> m_CustomAttributes;


        public IEnumerable<string> customAttributes
        {
            get { return m_CustomAttributes.Select(t=>t.name); }
        }

        public int GetCustomAttributeCount()
        {
            return m_CustomAttributes != null ? m_CustomAttributes.Count : 0;
        }


        public bool HasCustomAttribute(string name)
        {
            return m_CustomAttributes != null && m_CustomAttributes.Any(t => t.name == name);
        }

        public string GetCustomAttributeName(int index)
        {
            return m_CustomAttributes[index].name;
        }

        public VFXValueType GetCustomAttributeType(string name)
        {
            return m_CustomAttributes.FirstOrDefault(t => t.name == name).type;
        }

        public void GetCustomAttributeInfos(string name, out VFXValueType type, out object defaultValue)
        {
            int index = m_CustomAttributes.FindIndex(t => t.name == name);

            type = GetCustomAttributeType(index);
            defaultValue = GetCustomAttributeDefaultValue(index);
        }

        public VFXValueType GetCustomAttributeType(int index)
        {
            return m_CustomAttributes[index].type;
        }

        public object GetCustomAttributeDefaultValue(int index)
        {
            if (m_CustomAttributes[index].defaultValue == null || m_CustomAttributes[index].defaultValue.Get() == null)
                m_CustomAttributes[index] = new CustomAttribute { name = m_CustomAttributes[index].name, 
                                                                  type = m_CustomAttributes[index].type, 
                                                                  defaultValue = new VFXSerializableObject(VFXExpression.TypeToType(m_CustomAttributes[index].type),
                                                                                                           Activator.CreateInstance(VFXExpression.TypeToType(m_CustomAttributes[index].type)))
                                                                };
            return m_CustomAttributes[index].defaultValue.Get();
        }

        public void SetCustomAttributeDefaultValue(int index,object value)
        {
            if(m_CustomAttributes[index].defaultValue == null)
                m_CustomAttributes[index] = new CustomAttribute
                {
                    name = m_CustomAttributes[index].name,
                    type = m_CustomAttributes[index].type,
                    defaultValue = new VFXSerializableObject(VFXExpression.TypeToType(m_CustomAttributes[index].type))
                };
            m_CustomAttributes[index].defaultValue.Set(value);

            //TODO: What should we notify here ?

            InvalidateAttributeValue(m_CustomAttributes[index].name);

            Invalidate(InvalidationCause.kParamChanged);
        }


        void InvalidateAttributeValue(string attributeName)
        {
            ForEachSettingUsingAttribute((model, setting) =>
            {
                if (attributeName == (string)setting.GetValue(model))
                {
                    VFXOperator ope = model as VFXOperator;

                    if( ope != null)
                    {
                        ope.outputSlots[0].InvalidateExpressionTree();
                        ope.Invalidate(InvalidationCause.kExpressionGraphChanged);
                    }

                }
                return false;
            });
        }

        public void SetCustomAttributeName(int index,string newName, bool notify = true)
        {
            if (m_CustomAttributes == null ||  index >= m_CustomAttributes.Count)
                throw new System.ArgumentException("Invalid Index");

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach(char c in newName)
            {
                if( (c >= 'a' && c<='z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_')
                    sb.Append(c);
            }
            if( sb[0] >= '0' && sb[0] <='9')
                sb.Insert(0,'_');

            newName = sb.ToString();

            if( newName.Length < 1 )
                return;

            if (m_CustomAttributes.Any(t => t.name == newName) || VFXAttribute.AllIncludingVariadic.Any(t => t == newName))
            {
                newName = "Attribute";
                int cpt = 1;
                while (m_CustomAttributes.Select((t, i) => t.name == newName && i != index).Where(t => t).Count() > 0)
                {
                    newName = string.Format("Attribute{0}", cpt++);
                }
            }


            string oldName = m_CustomAttributes[index].name;

            m_CustomAttributes[index] = new CustomAttribute { name = newName, type = m_CustomAttributes[index].type, defaultValue = m_CustomAttributes[index].defaultValue };

            if( notify)
            {
                Invalidate(InvalidationCause.kSettingChanged);

                RenameAttribute(oldName, newName);
            }
        }

        public void SetCustomAttributeType(int index, VFXValueType newType)
        {
            if (index >= m_CustomAttributes.Count)
                throw new System.ArgumentException("Invalid Index");
            //TODO check that newType is an anthorized type for custom attributes.

            VFXValueType oldType = m_CustomAttributes[index].type;

            if ( newType == oldType)
                return;

            var defaultValue = m_CustomAttributes[index].defaultValue;
            object oldValue = defaultValue.Get();
            object newValue;
            if( ! VFXConverter.TryConvertTo(oldValue,VFXExpression.TypeToType(newType),out newValue) )
                newValue = Activator.CreateInstance(VFXExpression.TypeToType(newType));

            var newDefaultValue = new VFXSerializableObject(VFXExpression.TypeToType(newType), newValue);// defaultValue.Set(newValue);

            m_CustomAttributes[index] = new CustomAttribute { name = m_CustomAttributes[index].name, type = newType, defaultValue = newDefaultValue };

            string name = m_CustomAttributes[index].name;
            ForEachSettingUsingAttribute((model, setting) =>
            {
                if (name == (string)setting.GetValue(model))
                    model.Invalidate(InvalidationCause.kSettingChanged);
                return false;
            });

            Invalidate(InvalidationCause.kSettingChanged);
        }

        public void AddCustomAttribute()
        {
            if (m_CustomAttributes == null)
                m_CustomAttributes = new List<CustomAttribute>();
            string name = "Attribute";
            int cpt = 1;
            while (m_CustomAttributes.Any(t => t.name == name))
            {
                name = string.Format("Attribute{0}", cpt++);
            }
            m_CustomAttributes.Add(new CustomAttribute { name = name, type = VFXValueType.Float });
            Invalidate(InvalidationCause.kSettingChanged);
        }


        public int AddCustomAttribute(string name, VFXValueType newType)
        {
            if (m_CustomAttributes == null)
                m_CustomAttributes = new List<CustomAttribute>();
            // DOnt care about the current name, it will be changed
            m_CustomAttributes.Add(new CustomAttribute { name = "", type = newType });
            SetCustomAttributeName(m_CustomAttributes.Count - 1, name, false);
            
            Invalidate(InvalidationCause.kSettingChanged);
            return m_CustomAttributes.Count - 1;
        }

        void CheckCustomAttributes()
        {
            // Restore lost custom attributes that are used somewhere in the graph
            ForEachSettingUsingAttribute((m,f)=>{
                string customAttribute = (string) f.GetValue(m);
                if(!m_CustomAttributes.Any(t => t.name == customAttribute) && ! VFXAttribute.AllIncludingVariadic.Any(t => t == customAttribute))
                {
                    // trying to find the right type
                    var attributeType = VFXValueType.Float;
                    if( m is VFXOperator ) // Assume a get attribute if its an operator
                    {
                        var ope = m as VFXOperator;

                        if( ope.GetNbOutputSlots() > 0)
                            attributeType = (ope).GetOutputSlot(0).valueType;
                    }
                    else if( m is VFXBlock) // Assume a kind of set attribute is its a block
                    {
                        var block = m as VFXBlock;
                        if( block.GetNbInputSlots() > 0)
                            attributeType = block.GetInputSlot(0).valueType;
                    }

                    AddCustomAttribute(customAttribute,attributeType);
                }
                return false;
            }
            );
        }


        //Execute action on each settings used to store an attribute, until one return true;
        public bool ForEachSettingUsingAttributeInModel(VFXModel model, Func<FieldInfo,bool> action)
        {
            var settings = model.GetSettings(true);

            foreach (var setting in settings)
            {
                if (setting.FieldType == typeof(string))
                {
                    var attribute = setting.GetCustomAttributes().OfType<StringProviderAttribute>().FirstOrDefault();
                    if (attribute != null && (typeof(ReadWritableAttributeProvider).IsAssignableFrom(attribute.providerType) || typeof(AttributeProvider).IsAssignableFrom(attribute.providerType)))
                    {
                        if (action(setting))
                            return true;
                    }
                }
            }

            return false;
        }
        bool ForEachSettingUsingAttribute(Func<VFXModel,FieldInfo, bool> action)
        {
            foreach (var child in children)
            {
                if (child is VFXOperator)
                {
                    if (ForEachSettingUsingAttributeInModel(child, s => action(child, s)))
                        return true;
                }
                else if (child is VFXContext)
                {
                    if (ForEachSettingUsingAttributeInModel(child, s => action(child, s)))
                        return true;
                    foreach (var block in (child as VFXContext).children)
                    {
                        if (ForEachSettingUsingAttributeInModel(block, s => action(block, s)))
                            return true;
                    }
                }
            }

            return false;
        }

        public bool HasCustomAttributeUses(string name)
        {
            return ForEachSettingUsingAttribute((model,setting)=> name == (string)setting.GetValue(model));
        }

        public void RenameAttribute(string oldName,string newName)
        {
            ForEachSettingUsingAttribute((model, setting) =>
            {
                if (oldName == (string)setting.GetValue(model))
                {
                    setting.SetValue(model, newName);
                    model.Invalidate(InvalidationCause.kSettingChanged);
                }
                return false;
            });
        }

        public void RemoveCustomAttribute(int index)
        {
            if (index >= m_CustomAttributes.Count)
                throw new System.ArgumentException("Invalid Index");

            var modelUsingAttributes = new List<VFXModel>();

            string name = GetCustomAttributeName(index);

            ForEachSettingUsingAttribute((model, setting) =>
            {
                if (name == (string)setting.GetValue(model))
                    modelUsingAttributes.Add(model);
                return false;
            });

            foreach(var model in modelUsingAttributes)
            {
                model.GetParent().RemoveChild(model);
            }

            m_CustomAttributes.RemoveAt(index);
            Invalidate(InvalidationCause.kSettingChanged);
        }


        public void MoveCustomAttribute(int movedIndex,int destinationIndex)
        {
            if (movedIndex >= m_CustomAttributes.Count || destinationIndex >= m_CustomAttributes.Count)
                throw new System.ArgumentException("Invalid Index");
            
            var attr = m_CustomAttributes[movedIndex];
            m_CustomAttributes.RemoveAt(movedIndex);
            if (movedIndex < destinationIndex)
                movedIndex--;
            m_CustomAttributes.Insert(destinationIndex, attr);
            Invalidate(InvalidationCause.kUIChanged);
        }
    }
}
