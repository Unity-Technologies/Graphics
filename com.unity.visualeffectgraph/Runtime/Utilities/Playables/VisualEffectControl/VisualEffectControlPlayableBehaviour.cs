#if VFX_HAS_TIMELINE
using System;
using UnityEngine;
using UnityEngine.Playables;

namespace UnityEngine.VFX
{
    // Runtime representation of a TextClip.
    [Serializable]
    public class VisualEffectControlPlayableBehaviour : PlayableBehaviour
    {
        [Tooltip("The text to display (dummy)")]
        public string text = "";

        public double clipStart { get; set; }
        public double clipEnd { get; set; }
        public double easeIn { get; set; }
        public double easeOut { get; set; }
    }
}
#endif
