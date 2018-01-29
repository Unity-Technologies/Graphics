using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Graphing;
using System.Collections.ObjectModel;

namespace UnityEditor.VFX
{
    class VFXParameter : VFXSlotContainerModel<VFXModel, VFXModel>
    {
        protected VFXParameter()
        {
            m_exposedName = "exposedName";
            m_exposed = false;
            m_UICollapsed = false;
        }

        [VFXSetting, SerializeField]
        private string m_exposedName;
        [VFXSetting, SerializeField]
        private bool m_exposed;
        [VFXSetting, SerializeField]
        private int m_order;
        [VFXSetting, SerializeField]
        public VFXSerializableObject m_Min;
        [VFXSetting, SerializeField]
        public VFXSerializableObject m_Max;


        [System.Serializable]
        public class ParamInfo
        {
            public ParamInfo(int id)
            {
                this.id = id;
            }

            public readonly int id;
            public List<VFXSlot> linkedSlots;
            public Vector2 position;
        }

        [SerializeField]
        protected List<ParamInfo> m_ParamInfos;

        [NonSerialized]
        int m_IDCounter = 0;

        public string exposedName
        {
            get
            {
                return m_exposedName;
            }
        }

        public bool exposed
        {
            get
            {
                return m_exposed;
            }
        }

        public int order
        {
            get { return m_order; }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                yield return "m_order";
                yield return "m_Min";
                yield return "m_Max";
            }
        }

        public Type type
        {
            get { return outputSlots[0].property.type; }
        }

        public object value
        {
            get { return outputSlots[0].value; }
            set { outputSlots[0].value = value; }
        }


        public ReadOnlyCollection<ParamInfo> paramInfos
        {
            get
            {
                if (m_ParamInfos == null)
                {
                    m_ParamInfos = new List<ParamInfo>();
                }
                return m_ParamInfos.AsReadOnly();
            }
        }

        protected sealed override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            base.OnInvalidate(model, cause);

            if (cause == InvalidationCause.kSettingChanged)
            {
                m_ValueExpr = m_ExprSlots.Select(t => t.DefaultExpression(valueMode)).ToArray();
                outputSlots[0].InvalidateExpressionTree();
                Invalidate(InvalidationCause.kExpressionGraphChanged); // As we need to update exposed list event if not connected to a compilable context
            }
            if (cause == InvalidationCause.kParamChanged)
            {
                for (int i = 0; i < m_ExprSlots.Length; ++i)
                {
                    m_ValueExpr[i].SetContent(m_ExprSlots[i].value);
                }
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties { get { return PropertiesFromSlotsOrDefaultFromClass(VFXSlot.Direction.kOutput); } }

        public void Init(Type _type)
        {
            if (_type != null && outputSlots.Count == 0)
            {
                VFXSlot slot = VFXSlot.Create(new VFXProperty(_type, "o"), VFXSlot.Direction.kOutput);
                AddSlot(slot);

                if (!typeof(UnityEngine.Object).IsAssignableFrom(_type))
                    slot.value = System.Activator.CreateInstance(_type);
            }
            else
            {
                throw new InvalidOperationException("Cannot init VFXParameter");
            }
            m_ExprSlots = outputSlots[0].GetVFXValueTypeSlots().ToArray();
            m_ValueExpr = m_ExprSlots.Select(t => t.DefaultExpression(valueMode)).ToArray();
        }

        public override void OnEnable()
        {
            base.OnEnable();
            if (outputSlots.Count != 0)
            {
                m_ExprSlots = outputSlots[0].GetVFXValueTypeSlots().ToArray();
                m_ValueExpr = m_ExprSlots.Select(t => t.DefaultExpression(valueMode)).ToArray();
            }
            else
            {
                m_ExprSlots = new VFXSlot[0];
                m_ValueExpr = new VFXValue[0];
            }

            if (m_ParamInfos != null)
            {
                foreach (var param in paramInfos)
                {
                    if (m_IDCounter < param.id + 1)
                    {
                        m_IDCounter = param.id + 1;
                    }
                }
            }
        }

        ParamInfo CreateParamInfo()
        {
            return new ParamInfo(m_IDCounter++);
        }

        public void ValidateParamInfos()
        {
            // Case of the old VFXParameter we create a new one on the same place with all the Links
            if (position != Vector2.zero && paramInfos.Count == 0)
            {
                var newInfos = CreateParamInfo();
                newInfos.position = position;
                newInfos.linkedSlots = new List<VFXSlot>(outputSlots[0].LinkedSlots);
                m_ParamInfos.Add(newInfos);
            }
            else
            {
                // the linked slot of the outSlot decides so make sure that all appear once and only once in all the paramInfos
                HashSet<VFXSlot> links = new HashSet<VFXSlot>(outputSlots[0].LinkedSlots);

                foreach (var info in paramInfos)
                {
                    // first remove linkedSlots that are not existing
                    info.linkedSlots = info.linkedSlots.Intersect(links).ToList();

                    foreach (var slot in info.linkedSlots)
                    {
                        links.Remove(slot);
                    }
                }
                // if there are some links n the output slots that are in none of the infos, create a default param with them
                if (links.Count > 0)
                {
                    var newInfos = CreateParamInfo();
                    newInfos.position = Vector2.zero;
                    newInfos.linkedSlots = new List<VFXSlot>(links);
                    m_ParamInfos.Add(newInfos);
                }
            }
            position = Vector2.zero; // Set that as a marker that the parameter has been touched by the new code.
        }

        public override void UpdateOutputExpressions()
        {
            for (int i = 0; i < m_ExprSlots.Length; ++i)
            {
                m_ValueExpr[i].SetContent(m_ExprSlots[i].value);
                m_ExprSlots[i].SetExpression(m_ValueExpr[i]);
            }
        }

        private VFXValue.Mode valueMode
        {
            get
            {
                return exposed ? VFXValue.Mode.Variable : VFXValue.Mode.FoldableVariable;
            }
        }

        [NonSerialized]
        private VFXSlot[] m_ExprSlots;

        [NonSerialized]
        private VFXValue[] m_ValueExpr;
    }
}
