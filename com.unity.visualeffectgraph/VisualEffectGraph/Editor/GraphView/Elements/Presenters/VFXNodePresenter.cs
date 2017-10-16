using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class VFXNodePresenter : NodePresenter
    {
        public VFXModel model { get { return m_Model; } }
        public VFXViewPresenter viewPresenter { get { return m_ViewPresenter; } }

        [SerializeField]
        VFXModel m_Model;
        VFXViewPresenter m_ViewPresenter;

        public virtual void Init(VFXModel model, VFXViewPresenter viewPresenter)
        {
            m_Model = model;
            m_ViewPresenter = viewPresenter;

            base.position = new Rect(model.position, Vector2.one);
            UpdateTitle();
        }

        public virtual void UpdateTitle()
        {
            title = model.name;
        }

        public override Rect position
        {
            get
            {
                return base.position;
            }

            set
            {
                base.position = value;
                model.position = position.position;
            }
        }

        public override UnityEngine.Object[] GetObjectsToWatch()
        {
            return new UnityEngine.Object[] { this, model };
        }
    }
}
