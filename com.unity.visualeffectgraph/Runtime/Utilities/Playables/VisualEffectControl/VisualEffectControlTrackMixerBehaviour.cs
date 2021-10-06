#if VFX_HAS_TIMELINE
using UnityEngine;
using UnityEngine.Playables;
using System.Linq;
using System.Collections.Generic;

namespace UnityEngine.VFX
{
    public class VisualEffectControlTrackMixerBehaviour : PlayableBehaviour
    {
        VisualEffect m_Target;
        bool[] enabledStates;

        class ScrubbingCacheHelper
        {
            struct Event
            {
                public enum Type
                {
                    Play,
                    Stop
                }
                public Type type;
                public double time;
                public VisualEffectControlPlayableBehaviour playable;
            }

            struct Chunk
            {
                public double begin;
                public double end;
                public Event[] events;
            }
            Chunk[] m_Chunks;

            class VisualEffectControlPlayableBehaviourComparer : IComparer<VisualEffectControlPlayableBehaviour>
            {
                public int Compare(VisualEffectControlPlayableBehaviour x, VisualEffectControlPlayableBehaviour y)
                {
                    return x.clipStart.CompareTo(y.clipStart);
                }
            }

            const int kChunkError = int.MinValue;
            private int m_LastChunk = kChunkError;
            //private double m_LastGlobalTime = double.MinValue;

            private void OnEnterChunk(VisualEffect vfx)
            {
                vfx.Reinit();
                vfx.Stop(); //Workaround
            }

            private void OnLeaveChunk(VisualEffect vfx)
            {
                vfx.Reinit();
                vfx.Stop(); //Workaround
            }

            static readonly double epsilon = (double)Mathf.Epsilon;

            public void JumpToFrame(double globalTime, float deltaTime, VisualEffect vfx)
            {
                if (vfx == null)
                    return;

                //Find current chunk (TODOPAUL cache previous state)
                int currentChunkIndex = kChunkError;
                for (int i = 0; i < m_Chunks.Length; ++i)
                {
                    var chunk = m_Chunks[i];
                    if (chunk.begin <= globalTime && globalTime <= chunk.end)
                    {
                        currentChunkIndex = i;
                        break;
                    }
                }

                if (m_LastChunk != currentChunkIndex)
                {
                    if (m_LastChunk == kChunkError)
                        OnEnterChunk(vfx);
                    else
                        OnLeaveChunk(vfx);

                    m_LastChunk = currentChunkIndex;
                }

                if (currentChunkIndex != kChunkError)
                {
                    var chunk = m_Chunks[currentChunkIndex];

                    var expectedTime = globalTime;
                    var currentTime = chunk.begin + vfx.time;

                    var fixedStep = 1.0 / 60.0; //TODOPAUL: Use VFXManager settings
                    vfx.pause = true; //For now, always on pause like shuriken mixer

                    if (expectedTime < currentTime)
                    {
                        currentTime = chunk.begin;
                        OnEnterChunk(vfx);
                    }

                    var eventList = GetEventsIndex(chunk, currentTime, expectedTime);
                    var eventCount = eventList.Count();
                    var nextEvent = 0;
                    while (currentTime < expectedTime)
                    {
                        var currentEvent = default(Event);
                        var currentStepCount = 0u;
                        if (nextEvent < eventCount)
                        {
                            currentEvent = chunk.events[eventList.ElementAt(nextEvent++)];
                            currentStepCount = (uint)((currentEvent.time - currentTime) / fixedStep);
                        }
                        else
                        {
                            currentStepCount = (uint)((expectedTime - currentTime) / fixedStep);
                            if (currentStepCount == 0)
                            {
                                //We reached the maximum precision according to fixedStep, no more event
                                break;
                            }
                        }

                        if (currentStepCount != 0)
                        {
                            vfx.Simulate((float)fixedStep, currentStepCount);
                            currentTime += fixedStep * currentStepCount;
                        }
                        ProcessEvent(currentEvent, vfx);
                    }
                }
            }

            void ProcessEvent(Event currentEvent, VisualEffect vfx)
            {
                if (currentEvent.playable == null)
                    return;

                if (currentEvent.type == Event.Type.Play)
                    vfx.Play();
                else
                    vfx.Stop();
            }

            IEnumerable<int> GetEventsIndex(Chunk chunk, double minTime, double maxTime)
            {
                for (int i=0; i<chunk.events.Length; ++i)
                {
                    var currentEvent = chunk.events[i];

                    if (currentEvent.time > maxTime)
                        break;

                    if (minTime <= currentEvent.time)
                        yield return i;
                }
            }

            public void Init(Playable playable)
            {
                var chunks = new Stack<Chunk>();
                int inputCount = playable.GetInputCount();

                var playableBehaviors = new List<VisualEffectControlPlayableBehaviour>();
                for (int i= 0; i < inputCount; ++i)
                {
                    var inputPlayable = (ScriptPlayable<VisualEffectControlPlayableBehaviour>)playable.GetInput(i);
                    var inputBehavior = inputPlayable.GetBehaviour();
                    if (inputBehavior != null)
                        playableBehaviors.Add(inputBehavior);
                }

                playableBehaviors.Sort(new VisualEffectControlPlayableBehaviourComparer());
                foreach (var inputBehavior in playableBehaviors)
                {
                    if (   !chunks.Any()
                        || inputBehavior.clipStart > chunks.Peek().end)
                    {
                        chunks.Push(new Chunk()
                        {
                            begin = inputBehavior.clipStart,
                            events = new Event[0]
                        });
                    }

                    var currentChunk = chunks.Peek();
                    currentChunk.end = inputBehavior.clipEnd;

                    currentChunk.events = currentChunk.events.Concat(
                    new Event[]
                    {
                        new Event { playable = inputBehavior, type = Event.Type.Play, time = inputBehavior.easeIn },
                        new Event { playable = inputBehavior, type = Event.Type.Stop, time = inputBehavior.easeOut }
                    }).ToArray();

                    chunks.Pop();
                    chunks.Push(currentChunk);
                }
                m_Chunks = chunks.Reverse().ToArray();
            }
        }

        ScrubbingCacheHelper m_ScrubbingCacheHelper;

        public override void PrepareFrame(Playable playable, FrameData data)
        {
            /*
            if (m_ScrubbingCacheHelper == null)
            {
                m_ScrubbingCacheHelper = new ScrubbingCacheHelper();
                m_ScrubbingCacheHelper.Init(playable);
            }

            var globalTime = playable.GetTime();
            var deltaTime = data.deltaTime;
            m_ScrubbingCacheHelper.JumpToFrame(globalTime, deltaTime, m_Target);

            */
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            base.OnBehaviourPause(playable, info);
            if (m_Target != null)
                m_Target.pause = true;
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            base.OnBehaviourPlay(playable, info);
            if (m_Target != null)
                m_Target.pause = false;
        }

        //TODOPAUL: Temp
        static bool ignoreProcessFrame = true;

        // Called every frame that the timeline is evaluated. ProcessFrame is invoked after its' inputs.
        public override void ProcessFrame(Playable playable, FrameData data, object playerData)
        {
            SetDefaults(playerData as VisualEffect);

            if (m_ScrubbingCacheHelper == null)
            {
                m_ScrubbingCacheHelper = new ScrubbingCacheHelper();
                m_ScrubbingCacheHelper.Init(playable);
            }

            var globalTime = playable.GetTime();
            var deltaTime = data.deltaTime;
            m_ScrubbingCacheHelper.JumpToFrame(globalTime, deltaTime, m_Target);


            //TODOPAUL : Remove following code
            if (ignoreProcessFrame)
                return;

            if (m_Target == null)
                return;

            int inputCount = playable.GetInputCount();

            float totalWeight = 0f;
            float greatestWeight = 0f;

            //TODOPAUL : Focus a bit more on this code
            int playableIndex = 0;
            for (int i = 0; i < inputCount; i++)
            {
                var inputWeight = playable.GetInputWeight(i);
                var inputPlayable = (ScriptPlayable<VisualEffectControlPlayableBehaviour>)playable.GetInput(i);
                VisualEffectControlPlayableBehaviour input = inputPlayable.GetBehaviour();

                totalWeight += inputWeight;

                // use the text with the highest weight
                if (inputWeight > greatestWeight)
                {
                    greatestWeight = inputWeight;
                    playableIndex = 0;
                }
            }

            if (greatestWeight > 0.0f)
            {
                if (m_Target.enabled != true)
                {
                    //Workaround to avoid the play event by default -_-'
                    m_Target.enabled = true;
                    m_Target.Stop();
                }
            }
            else
            {
                m_Target.enabled = false;
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
            //see m_ScrubbingCacheHelper  in /CinemachineMixer.cs?L174:25
            //var test = (ScriptPlayable<VisualEffectControlPlayableBehaviour>)playable.GetInput(0);
            //var test2 = test.GetBehaviour();
            //var test3 = PlayableExtensions.GetDuration(playable.GetInput(0));

            enabledStates = new bool[playable.GetInputCount()];
            m_ScrubbingCacheHelper = null;
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            RestoreDefaults();
            enabledStates = null;
            m_ScrubbingCacheHelper = null;
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

            m_Target.pause = false;
        }
    }
}
#endif
