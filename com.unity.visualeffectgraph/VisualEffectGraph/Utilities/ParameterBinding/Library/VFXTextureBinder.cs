using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEngine.Experimental.VFX.Utility
{
    [VFXBinder("Rendering/Texture")]
    public class VFXTextureBinder : VFXBinderBase
    {
        [VFXParameterBinding("UnityEngine.Texture2D")]
        public string Parameter = "Texture";
        public Texture Texture;


        public override bool IsValid(VisualEffect component)
        {
            return Texture != null && component.HasTexture(GetParameter(Parameter));
        }

        public override void UpdateBinding(VisualEffect component)
        {
            component.SetTexture(GetParameter(Parameter), Texture);
        }

        public override string ToString()
        {
            return string.Format("Texture : '{0}' -> {1}", Parameter, Texture == null ? "(null)" : Texture.name);
        }
    }
}
