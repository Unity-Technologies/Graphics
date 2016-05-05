using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using UnityEditor.Experimental.VFX;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdExposeDataNodeOption : VFXEdNodeOption
    {
        private Color m_Color = new Color(1.0f, 0.5f, 0.0f);

        private VFXDataNodeModel m_Model;  

        internal VFXEdExposeDataNodeOption(VFXDataNodeModel model) : base(false) 
        {
            m_Model = model;
        }

        public override void UpdateModel(UpdateType t)
        {
            m_Model.Exposed = Enabled;
        }

        protected override Color GetColor()
        {
            return m_Color;
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            base.Render(parentRect, canvas);

            Rect iconRect = VFXEditor.styles.NodeOption.padding.Remove(GetDrawableRect());
            iconRect = new RectOffset(4, 4, 4, 4).Remove(iconRect);
            if (!Enabled)
                GUI.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);
            GUI.DrawTexture(iconRect, VFXEditor.styles.GetIcon("Config"),ScaleMode.ScaleToFit,true,0);
            GUI.color = Color.white;
        }
    }
}
