using UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityEditor.VFX.UI
{
    class VFXNodePresenter : NodePresenter, IVFXPresenter
    {
        private VFXViewPresenter m_View;
        public VFXModel model { get { return m_Node; } }

        [SerializeField]
        private VFXSlotContainerModel<VFXModel, VFXModel> m_Node;
        public VFXSlotContainerModel<VFXModel, VFXModel> node { get { return m_Node; } }

        protected virtual NodeAnchorPresenter CreateAnchorPresenter(VFXSlot slot, Direction direction)
        {
            var inAnchor = CreateInstance<VFXNodeAnchorPresenter>();
            inAnchor.Init(this, slot.id, direction);
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

        virtual protected void Reset()
        {
            if (m_Node != null)
            {
                position = new Rect(model.position.x, model.position.y, position.width, position.height);
                expanded = !model.collapsed;
                title = node.name + " " + node.m_OnEnabledCount;

                var newinputAnchors = node.inputSlots.Select(s => CreateAnchorPresenter(s, Direction.Input)).ToArray();
                var newoutputAnchors = node.outputSlots.Select(s => CreateAnchorPresenter(s, Direction.Output)).ToArray();

                Func<NodeAnchorPresenter, NodeAnchorPresenter, bool> fnComparer = delegate (NodeAnchorPresenter x, NodeAnchorPresenter y)
                {
                    var X = x as VFXNodeAnchorPresenter;
                    var Y = y as VFXNodeAnchorPresenter;
                    return X.slotID == Y.slotID
                            && X.name == Y.name
                            && X.anchorType == Y.anchorType;
                };

                if (!SequenceEqual(newinputAnchors, inputAnchors, fnComparer)
                    || !SequenceEqual(newoutputAnchors, outputAnchors, fnComparer))
                {
                    inputAnchors.Clear();
                    outputAnchors.Clear();
                    inputAnchors.AddRange(node.inputSlots.Select(s => CreateAnchorPresenter(s, Direction.Input)));
                    outputAnchors.AddRange(node.outputSlots.Select(s => CreateAnchorPresenter(s, Direction.Output)));
                    m_View.RecreateNodeEdges();
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
            if (model == m_Node && cause != VFXModel.InvalidationCause.kUIChanged)
            {
                Reset();
            }
        }

        public virtual void Init(VFXModel model, VFXViewPresenter viewPresenter)
        {
            m_View = viewPresenter;

            if (m_Node != model)
            {
                if (m_Node != null)
                {
                    m_Node.onInvalidateDelegate -= OnOperatorInvalidate;
                }

                m_Node = model as VFXSlotContainerModel<VFXModel, VFXModel>;
                m_Node.onInvalidateDelegate += OnOperatorInvalidate;
            }
            Reset();
        }
    }
}

