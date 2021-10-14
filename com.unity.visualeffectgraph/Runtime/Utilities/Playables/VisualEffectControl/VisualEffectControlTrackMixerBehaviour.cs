#if VFX_HAS_TIMELINE
using UnityEngine;
using UnityEngine.Playables;
using System.Linq;
using System.Collections.Generic;

namespace UnityEngine.VFX
{
    public class VisualEffectControlTrackMixerBehaviour : PlayableBehaviour
    {
        class ScrubbingCacheHelper
        {
            struct Event
            {
                public int nameId;
                public VFXEventAttribute attribute;
                public double time;
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

            const int kErrorIndex = int.MinValue;
            private int m_LastChunk = kErrorIndex;
            private int m_LastEvent = kErrorIndex;
            private double m_LastPlayableTime = double.MinValue;

            private void OnEnterChunk(VisualEffect vfx)
            {
                vfx.Reinit(false);
            }

            private void OnLeaveChunk(VisualEffect vfx)
            {
                vfx.Reinit(false);
            }

            //Debug only, will be removed int the end
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

            public void Update(double playableTime, float deltaTime, VisualEffect vfx)
            {
                if (vfx == null)
                    return;

                var paused = deltaTime == 0.0;
                var playingBackward = playableTime < m_LastPlayableTime;
                var dbg = dbg_state.OutChunk;

                //Find current chunk (TODOPAUL cache previous state to speed up)
                var currentChunkIndex = kErrorIndex;
                for (int i = 0; i < m_Chunks.Length; ++i)
                {
                    var chunk = m_Chunks[i];
                    if (chunk.begin <= playableTime && playableTime <= chunk.end)
                    {
                        currentChunkIndex = i;
                        break;
                    }
                }

                if (m_LastChunk != currentChunkIndex)
                {
                    if (m_LastChunk == kErrorIndex)
                        OnEnterChunk(vfx);
                    else
                        OnLeaveChunk(vfx);

                    m_LastChunk = currentChunkIndex;
                }

                vfx.pause = paused;
                if (currentChunkIndex != kErrorIndex)
                {
                    dbg = dbg_state.Playing;

                    var chunk = m_Chunks[currentChunkIndex];
                    var actualCurrentTime = chunk.begin + vfx.time;
                    if (!playingBackward)
                    {
                        if (Abs(m_LastPlayableTime - actualCurrentTime) < VFXManager.maxDeltaTime)
                        {
                            //Remove the float part from VFX and only keep double precision
                            actualCurrentTime = m_LastPlayableTime;
                        }
                        else
                        {
                            //VFX is too late on timeline, we will have to launch simulate
                            //Warning, in that case, event could have been already sent
                            //m_LastEvent status prevents sending twice the same event
                        }
                    }
                    else
                    {
                        dbg = dbg_state.ScrubbingBackward;
                        actualCurrentTime = chunk.begin;
                        m_LastPlayableTime = actualCurrentTime;
                        m_LastEvent = kErrorIndex;
                        OnEnterChunk(vfx);
                    }

                    double expectedCurrentTime;
                    if (paused)
                        expectedCurrentTime = playableTime;
                    else
                        expectedCurrentTime = playableTime - VFXManager.fixedTimeStep;

                    {
                        //1. Process adjustment if actualCurrentTime < expectedCurrentTime
                        var eventList = GetEventsIndex(chunk, actualCurrentTime, expectedCurrentTime, m_LastEvent);
                        var eventCount = eventList.Count();
                        var nextEvent = 0;

                        var fixedStep = VFXManager.maxDeltaTime; //TODOPAUL, reduce the interval in case of paused ?
                        while (actualCurrentTime < expectedCurrentTime)
                        {
                            var currentEventIndex = kErrorIndex;
                            uint currentStepCount;
                            if (nextEvent < eventCount)
                            {
                                currentEventIndex = eventList.ElementAt(nextEvent++);
                                var currentEvent = chunk.events[currentEventIndex];
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
                            ProcessEvent(currentEventIndex, chunk, vfx);
                        }
                    }

                    //Sending incoming event
                    {
                        var eventList = GetEventsIndex(chunk, actualCurrentTime, playableTime, m_LastEvent);
                        foreach (var itEvent in eventList)
                            ProcessEvent(itEvent, chunk, vfx);
                    }
                }

                UpdateScrubbingState(vfx, dbg);
                m_LastPlayableTime = playableTime;
            }

            void ProcessEvent(int eventIndex, Chunk currentChunk, VisualEffect vfx)
            {
                if (eventIndex == kErrorIndex)
                    return;

                m_LastEvent = eventIndex;
                var currentEvent = currentChunk.events[eventIndex];
                vfx.SendEvent(currentEvent.nameId);
            }

            static IEnumerable<int> GetEventsIndex(Chunk chunk, double minTime, double maxTime, int lastIndex)
            {
                var startIndex = lastIndex == kErrorIndex ? 0 : lastIndex + 1;
                for (int i = startIndex; i < chunk.events.Length; ++i)
                {
                    var currentEvent = chunk.events[i];
                    if (currentEvent.time > maxTime)
                        break;

                    if (minTime <= currentEvent.time)
                        yield return i;
                }
            }

            static IEnumerable<Event> ComputeRuntimeEvent(VisualEffectControlPlayableBehaviour behavior, VisualEffect vfx)
            {
                foreach (var itEvent in behavior.events)
                {
                    double absoluteTime = VisualEffectPlayableSerializedEvent.GetAbsoluteTime(itEvent, behavior);

                    //TODOPAUL: Should not be there but in UX
                    if (absoluteTime > behavior.clipEnd)
                        absoluteTime = behavior.clipEnd;
                    if (absoluteTime < behavior.clipStart)
                        absoluteTime = behavior.clipStart;

                    yield return new Event()
                    {
                        attribute = null,
                        nameId = Shader.PropertyToID(itEvent.name),
                        time = absoluteTime
                    };
                }
            }

            public void Init(Playable playable, VisualEffect vfx)
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
                    var currentsEvents = ComputeRuntimeEvent(inputBehavior, vfx).OrderBy(o => o.time).ToArray();
                    currentChunk.events = currentChunk.events.Concat(currentsEvents).ToArray();

                    chunks.Pop();
                    chunks.Push(currentChunk);
                }
                 m_Chunks = chunks.Reverse().ToArray();
            }
        }

        ScrubbingCacheHelper m_ScrubbingCacheHelper;
        VisualEffect m_Target;

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

        public override void ProcessFrame(Playable playable, FrameData data, object playerData)
        {
            SetDefaults(playerData as VisualEffect);
            if (m_Target == null)
                return;

            if (m_ScrubbingCacheHelper == null)
            {
                m_ScrubbingCacheHelper = new ScrubbingCacheHelper();
                m_ScrubbingCacheHelper.Init(playable, m_Target);
            }

            var globalTime = playable.GetTime();
            var deltaTime = data.deltaTime;
            m_ScrubbingCacheHelper.Update(globalTime, deltaTime, m_Target);
        }

        public override void OnPlayableCreate(Playable playable)
        {
            m_ScrubbingCacheHelper = null;
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (m_Target != null)
                m_Target.pause = false;

            m_Target = null;
            m_ScrubbingCacheHelper = null;
        }

        void SetDefaults(VisualEffect vfx)
        {
            if (m_Target == vfx)
                return;
            m_Target = vfx;
            m_ScrubbingCacheHelper = null;
        }
    }
}
#endif
