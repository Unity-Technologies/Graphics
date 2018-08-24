using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Experimental.VFX;

public class VisualEffectActivationMixerBehaviour : PlayableBehaviour
{
    bool[] states;

    // NOTE: This function is called at runtime and edit time.  Keep that in mind when setting the values of properties.

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        VisualEffect vfxComponent = playerData as VisualEffect;

        if (!vfxComponent)
            return;

        int inputCount = playable.GetInputCount();

        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            bool state = inputWeight != 0.0f;

            var inputPlayable = (ScriptPlayable<VisualEffectActivationBehaviour>)playable.GetInput(i);
            var input = inputPlayable.GetBehaviour();

            // Use the above variables to process each frame of this playable.
            if (states[i] != state)
            {
                if (state)
                    vfxComponent.SendEvent(input.OnClipEnter, input.GetEventAttribute(vfxComponent, input.ClipEnterEventAttributes));
                else
                    vfxComponent.SendEvent(input.OnClipExit, input.GetEventAttribute(vfxComponent, input.ClipExitEventAttributes));

                states[i] = state;
            }
        }
    }

    public override void OnPlayableCreate(Playable playable)
    {
        states = new bool[playable.GetInputCount()];
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        states = null;
    }

}
