#if VFX_HAS_TIMELINE
using System;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

[Serializable]
class VisualEffectActivationBehaviour : PlayableBehaviour
{
    [Serializable]
    public enum AttributeType
    {
        //Actually enum values are synchronized with VFXValueType
        Float = 1,
        Float2 = 2,
        Float3 = 3,
        Float4 = 4,
        Int32 = 5,
        Uint32 = 6,
        Boolean = 17
    }

    [Serializable]
    public struct EventState
    {
#pragma warning disable 649
        public ExposedProperty attribute;
        public AttributeType type;
        public float[] values; //double could cover precision of integer and float within the same container, but not needed for now
#pragma warning restore 649
    }

    [SerializeField]
    public ExposedProperty onClipEnter = "OnPlay";
    [SerializeField]
    public ExposedProperty onClipExit = "OnStop";
    [SerializeField]
    public EventState[] clipEnterEventAttributes = null;
    [SerializeField]
    public EventState[] clipExitEventAttributes = null;
}
#endif
