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

        //TODOPAUL: Rename
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
            private double m_LastPlayableTime = double.MinValue;
            private double m_LastParticleTime = double.MinValue;

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

            //Debug only, will be removed at the end
            enum dbg_state
            {
                Playing,
                ScrubbingForward,
                ScrubbingBackward,
                OutChunk
            }

            private int scrubbingID = Shader.PropertyToID("scrubbing");
            private void UpdateScrubbingState(VisualEffect vfx, dbg_state scrubbing)
            {
                if (vfx.HasUInt(scrubbingID))
                    vfx.SetUInt(scrubbingID, (uint)scrubbing);
            }

            private double Min(double a, double b)
            {
                return a < b ? a : b;
            }
            private double Max(double a, double b)
            {
                return a < b ? a : b;
            }

            private double Abs(double a)
            {
                return a < 0 ? -a : a;
            }

            public void Update(double globalTime, float deltaTime, VisualEffect vfx)
            {
                if (vfx == null)
                    return;

                var paused = deltaTime == 0.0;
                var playingBackward = globalTime < m_LastPlayableTime;
                var dbg = dbg_state.OutChunk;

                //Find current chunk (TODOPAUL cache previous state to speed up)
                var currentChunkIndex = kChunkError;
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

                vfx.pause = paused;
                if (currentChunkIndex != kChunkError)
                {
                    dbg = dbg_state.Playing;

                    var chunk = m_Chunks[currentChunkIndex];

                    var expectedNextTime = globalTime;
                    var actualCurrentTime = chunk.begin + vfx.time;

                    if (!playingBackward)
                    {
                        if (Abs(m_LastParticleTime - actualCurrentTime) < VFXManager.maxDeltaTime)
                        {
                            //Remove the float part and only keep double precision
                            actualCurrentTime = m_LastParticleTime;
                        }
                        else
                        {
                            //VFX is too late on timeline
                            //TODOPAUL: In that case, we could have already consume event...
                        }
                    }
                    else
                    {
                        dbg = dbg_state.ScrubbingBackward;
                        actualCurrentTime = chunk.begin;
                        m_LastParticleTime = actualCurrentTime;
                        OnEnterChunk(vfx);
                    }

                    double expectedCurrentTime;
                    if (paused)
                        expectedCurrentTime = expectedNextTime;
                    else
                        expectedCurrentTime = expectedNextTime - VFXManager.fixedTimeStep;

                    var eventList = GetEventsIndex(chunk, actualCurrentTime, expectedCurrentTime);
                    var eventCount = eventList.Count();
                    var nextEvent = 0;

                    var fixedStep = VFXManager.maxDeltaTime; //TODOPAUL, reduce the interval in case of paused ?
                    while (actualCurrentTime < expectedCurrentTime)
                    {
                        var currentEvent = default(Event);
                        var currentStepCount = 0u;
                        if (nextEvent < eventCount)
                        {
                            currentEvent = chunk.events[eventList.ElementAt(nextEvent++)];
                            currentStepCount = (uint)((currentEvent.time - actualCurrentTime) / fixedStep);
                        }
                        else
                        {
                            currentStepCount = (uint)((expectedCurrentTime - actualCurrentTime) / fixedStep);
                            if (currentStepCount == 0)
                            {
                                //We reached the maximum precision according to fixedStep & no more event
                                break;
                            }
                        }

                        if (currentStepCount != 0)
                        {
                            if (dbg != dbg_state.ScrubbingBackward)
                                dbg = dbg_state.ScrubbingForward;

                            vfx.Simulate((float)fixedStep, currentStepCount);
                            actualCurrentTime += fixedStep * currentStepCount;
                        }
                        ProcessEvent(currentEvent, vfx);
                    }

                    eventList = GetEventsIndex(chunk, actualCurrentTime, expectedNextTime);
                    foreach (var itEvent in eventList)
                    {
                        ProcessEvent(chunk.events[itEvent], vfx);
                    }
                    m_LastParticleTime = expectedNextTime;
                }

                UpdateScrubbingState(vfx, dbg);
                m_LastPlayableTime = globalTime;
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
                for (int i = 0; i < chunk.events.Length; ++i)
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
                for (int i = 0; i < inputCount; ++i)
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
            m_ScrubbingCacheHelper.Update(globalTime, deltaTime, m_Target);


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
