#if VFX_HAS_TIMELINE
using UnityEngine;
using UnityEngine.Playables;

namespace UnityEngine.VFX
{
    public class VisualEffectControlTrackMixerBehaviour : PlayableBehaviour
    {
        string m_DefaultText;
        VisualEffect m_Target;
        bool[] enabledStates;

        // Called every frame that the timeline is evaluated. ProcessFrame is invoked after its' inputs.
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            SetDefaults(playerData as VisualEffect);
            if (m_Target == null)
                return;

            int inputCount = playable.GetInputCount();

            float totalWeight = 0f;
            float greatestWeight = 0f;
            string text = m_DefaultText;

            //TODOPAUL : Focus a bit more on this code
            int playableIndex = 0;
            for (int i = 0; i < inputCount; i++)
            {
                float inputWeight = playable.GetInputWeight(i);
                ScriptPlayable<VisualEffectControlPlayableBehaviour> inputPlayable = (ScriptPlayable<VisualEffectControlPlayableBehaviour>)playable.GetInput(i);
                VisualEffectControlPlayableBehaviour input = inputPlayable.GetBehaviour();

                totalWeight += inputWeight;

                // use the text with the highest weight
                if (inputWeight > greatestWeight)
                {
                    text = input.text;
                    greatestWeight = inputWeight;
                    playableIndex = 0;
                }
            }

            bool wasEnabled = m_Target.enabled;
            m_Target.enabled = greatestWeight > 0.0f;
            if (!wasEnabled && m_Target.enabled)
            {
                //Workaround to avoid the play event by default -_-'
                m_Target.Stop();
            }


            bool playingState = greatestWeight == 1.0f;
            if (enabledStates[playableIndex] != playingState)
            {
                if (playingState)
                    m_Target.Play();
                else
                    m_Target.Stop();

                enabledStates[playableIndex] = playingState;
            }

            // blend to the default values
            //TODOPAUL: Clean
            //m_TrackBinding.color = Color.Lerp(m_DefaultColor, blendedColor, totalWeight);
            //m_TrackBinding.fontSize = Mathf.RoundToInt(Mathf.Lerp(m_DefaultFontSize, blendedFontSize, totalWeight));
            //m_TrackBinding.text = text;
        }

        public override void OnPlayableCreate(Playable playable)
        {
            enabledStates = new bool[playable.GetInputCount()];
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            RestoreDefaults();
            enabledStates = null;
        }

        void SetDefaults(VisualEffect vfx)
        {
            if (m_Target == vfx)
                return;

            RestoreDefaults();

            m_Target = vfx;
            if (m_Target != null)
            {
                //TODOPAUL: Clean
            }
        }

        void RestoreDefaults()
        {
            if (m_Target == null)
                return;

            //TODOPAUL: Clean
        }
    }
}
#endif
