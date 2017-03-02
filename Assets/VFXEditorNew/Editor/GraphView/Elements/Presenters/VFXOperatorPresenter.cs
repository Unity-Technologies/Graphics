using RMGUI.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityEditor.VFX.UI
{
    class VFXOperatorPresenter : NodePresenter, IVFXPresenter
    {
        private VFXViewPresenter m_View;

        [SerializeField]
        private VFXOperator m_Operator;
        public VFXOperator Operator     { get { return m_Operator; } }
        public virtual VFXModel model   { get { return m_Operator; } }

        [SerializeField]
        public int m_dirtyHack;

        [SerializeField]
        private object m_settings;
        public object settings { get { return m_settings; } set { m_settings = value; m_dirtyHack++; } }

        private NodeAnchorPresenter CreateAnchorPresenter(VFXOperator.VFXMitoSlot slot, Direction direction)
        {
            var inAnchor = CreateInstance<VFXOperatorAnchorPresenter>();
            var expression = slot.expression;
            inAnchor.anchorType = expression == null ? typeof(float) : VFXExpression.TypeToType(expression.ValueType);
            if (expression == null)
            {
                inAnchor.name = "Empty";
            }

            inAnchor.Init(this, slot.slotID, direction);
            return inAnchor;
        }

        public static bool SequenceEqual<T1, T2>(IEnumerable<T1> first, IEnumerable<T2> second, Func<T1, T2, bool> comparer)
        {
            using (IEnumerator<T1> e1 = first.GetEnumerator())
            using (IEnumerator<T2> e2 = second.GetEnumerator())
            {
                while (e1.MoveNext())
                {
                    if (!(e2.MoveNext() && comparer(e1.Current, e2.Current)))
                        return false;
                }

                if (e2.MoveNext())
                    return false;
            }

            return true;
        }

        private void Reset()
        {
            if (m_Operator != null)
            {
                position = new Rect(model.position.x, model.position.y, position.width, position.height);
                expanded = !model.collapsed;
                title = Operator.name;
                settings = Operator.settings;

                var newinputAnchors = Operator.InputSlots.Select(s => CreateAnchorPresenter(s, Direction.Input)).ToArray();
                var newoutputAnchors = Operator.OutputSlots.Select(s => CreateAnchorPresenter(s, Direction.Output)).ToArray();

                Func<NodeAnchorPresenter, NodeAnchorPresenter, bool> fnComparer = delegate (NodeAnchorPresenter x, NodeAnchorPresenter y)
                {
                    var X = x as VFXOperatorAnchorPresenter;
                    var Y = y as VFXOperatorAnchorPresenter;
                    return      X.slotID == Y.slotID 
                            &&  X.name == Y.name
                            &&  X.anchorType == Y.anchorType;
                };

                if (    !SequenceEqual(newinputAnchors, inputAnchors, fnComparer)
                    ||  !SequenceEqual(newoutputAnchors, outputAnchors, fnComparer))
                {
                    inputAnchors.Clear();
                    outputAnchors.Clear();
                    inputAnchors.AddRange(Operator.InputSlots.Select(s => CreateAnchorPresenter(s, Direction.Input)));
                    outputAnchors.AddRange(Operator.OutputSlots.Select(s => CreateAnchorPresenter(s, Direction.Output)));
                    m_View.RecreateOperatorEdges();
                }
            }
            else
            {
                inputAnchors.Clear();
                outputAnchors.Clear();
            }
        }

        void OnOperatorInvalidate(VFXModel model, VFXModel.InvalidationCause cause)
        {
            if (model == m_Operator && cause != VFXModel.InvalidationCause.kUIChanged)
            {
                Reset();
            }
        }

        public virtual void Init(VFXModel model, VFXViewPresenter viewPresenter)
        {
            m_View = viewPresenter;

            if (m_Operator != model)
            {
                if (m_Operator != null)
                {
                    m_Operator.onInvalidateDelegate -= OnOperatorInvalidate;
                }

                m_Operator = (VFXOperator)model;
                m_Operator.onInvalidateDelegate += OnOperatorInvalidate;
            }
            Reset();
        }
    }
}
