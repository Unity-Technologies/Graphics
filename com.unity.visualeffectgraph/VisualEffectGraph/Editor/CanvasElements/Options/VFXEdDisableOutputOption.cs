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
    internal class VFXEdDisableOutputOption : VFXEdNodeOption
    {
        private Color m_Color = VFXEditor.styles.GetContextColor(VFXContextDesc.Type.kTypeOutput);

        internal VFXEdDisableOutputOption(VFXContextModel model) 
            : base(true) 
        {
            // TODO : replace base(true) by model.isEnabled;
            translation = new Vector3(26, 26);
        }

        public override void UpdateModel(UpdateType t)
        {
            // TODO : Disable Here!
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
            {
                GUI.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);
            }
            GUI.DrawTexture(iconRect, Enabled? VFXEditor.styles.VisibilityIcon : VFXEditor.styles.VisibilityIconDisabled,ScaleMode.ScaleToFit,true,0);
            GUI.color = Color.white;
        }
    }
}
