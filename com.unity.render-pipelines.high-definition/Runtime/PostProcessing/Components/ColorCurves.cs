using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable, VolumeComponentMenu("Post-processing/Color Curves")]
    public sealed class ColorCurves : VolumeComponent, IPostProcessComponent
    {
        public TextureCurveParameter master   = new TextureCurveParameter(new TextureCurve(new[] { new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f) }, 0f, false, new Vector2(0f, 1f)));
        public TextureCurveParameter red      = new TextureCurveParameter(new TextureCurve(new[] { new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f) }, 0f, false, new Vector2(0f, 1f)));
        public TextureCurveParameter green    = new TextureCurveParameter(new TextureCurve(new[] { new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f) }, 0f, false, new Vector2(0f, 1f)));
        public TextureCurveParameter blue     = new TextureCurveParameter(new TextureCurve(new[] { new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f) }, 0f, false, new Vector2(0f, 1f)));

        public TextureCurveParameter hueVsHue = new TextureCurveParameter(new TextureCurve(new Keyframe[] {}, 0.5f, true,  new Vector2(0f, 1f)));
        public TextureCurveParameter hueVsSat = new TextureCurveParameter(new TextureCurve(new Keyframe[] {}, 0.5f, true,  new Vector2(0f, 1f)));
        public TextureCurveParameter satVsSat = new TextureCurveParameter(new TextureCurve(new Keyframe[] {}, 0.5f, false, new Vector2(0f, 1f)));
        public TextureCurveParameter lumVsSat = new TextureCurveParameter(new TextureCurve(new Keyframe[] {}, 0.5f, false, new Vector2(0f, 1f)));

#pragma warning disable 414
        [SerializeField]
        int m_SelectedCurve = 0; // Only used to track the currently selected curve in the UI
#pragma warning restore 414

        public bool IsActive()
        {
            return true;
        }
    }
}
