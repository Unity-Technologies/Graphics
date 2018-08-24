using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Experimental.VFX;

[Serializable]
public class VisualEffectActivationBehaviour : PlayableBehaviour
{
    [Serializable]
    public enum Attribute
    {
        Position,
        TargetPosition,
        Velocity,
        Color,
        Size,
        LifeTime,
        SpawnCount,
    }

    [Serializable]
    public struct EventState
    {
        public Attribute attribute;
        public float FloatValue;
        public int IntValue;
        public Vector3 VectorValue;
    }

    public string OnClipEnter = "OnPlay";
    public string OnClipExit = "OnStop";

    public EventState[] ClipEnterEventAttributes;
    public EventState[] ClipExitEventAttributes;

    public override void OnPlayableCreate(Playable playable)
    {
    }

    public void SetEventAttribute(VFXEventAttribute evt, EventState state)
    {
        switch(state.attribute)
        {
            case Attribute.Color: evt.SetVector3("color", state.VectorValue); break;
            case Attribute.Position: evt.SetVector3("position", state.VectorValue); break;
            case Attribute.Velocity: evt.SetVector3("velocity", state.VectorValue); break;
            case Attribute.TargetPosition: evt.SetVector3("targetPosition", state.VectorValue); break;
            case Attribute.Size: evt.SetFloat("sizeX", state.FloatValue); break;
            case Attribute.LifeTime: evt.SetFloat("lifetime", state.FloatValue); break;
            case Attribute.SpawnCount: evt.SetInt("spawnCount", state.IntValue); break;
        }
    }

    public VFXEventAttribute GetEventAttribute(VisualEffect component, EventState[] states)
    {
        var evt = component.CreateVFXEventAttribute();
        if (states != null)
        {
            foreach (var state in states)
                SetEventAttribute(evt, state);
        }

        return evt;
    }
}
