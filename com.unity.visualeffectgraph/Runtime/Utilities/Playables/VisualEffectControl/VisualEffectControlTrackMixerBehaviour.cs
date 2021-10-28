#if VFX_HAS_TIMELINE
using UnityEngine;
using UnityEngine.Playables;
using System.Linq;
using System.Collections.Generic;
using System;

namespace UnityEngine.VFX
{
    class VisualEffectControlTrackMixerBehaviour : PlayableBehaviour
    {
        public class ScrubbingCacheHelper
        {
            struct Event
            {
                public int nameId;
                public VFXEventAttribute attribute;
                public double time;

                public enum ClipType
                {
                    None,
                    Enter,
                    Exit
                }
                public int clipIndex;
                public ClipType clipType;
            }

            struct Clip
            {
                public int enter;
                public int exit;
            }

            struct Chunk
            {
                public bool scrubbing;
                public uint startSeed;

                public double begin;
                public double end;

                public uint prewarmCount;
                public float prewarmDeltaTime;
                public double prewarmOffset;
                public int prewarmEvent;

                public Event[] events;
                public Clip[] clips;
            }

            const int kErrorIndex = int.MinValue;
            int m_LastChunk = kErrorIndex;
            int m_LastEvent = kErrorIndex;
            bool[] m_ClipState; //TODOPAUL: Actually, it's only useful for debug
            double m_LastPlayableTime = double.MinValue;

            VisualEffect m_Target;
            bool m_BackupReseedOnPlay;
            uint m_BackupStartSeed;

            Chunk[] m_Chunks;

            public struct Debug
            {
                public enum State
                {
                    Playing,
                    ScrubbingForward,
                    ScrubbingBackward,
                    OutChunk
                }

                public State state;
                public int lastChunk;
                public int lastEvent;
                public double lastPlayableTime;
                public double lastDeltaTime;
                public float vfxTime;
                public bool[] clipState;
            }

            //TODOPAUL if right thing todo => store it in VFXManager
            public static float s_MaximumScrubbingTime = 30.0f;
            public Queue<Debug> m_DebugFrame = new Queue<Debug>();

            private void OnEnterChunk(int currentChunk)
            {
                var chunk = m_Chunks[currentChunk];

                if (!chunk.scrubbing)
                {
                    m_ClipState = new bool[chunk.clips.Length];
                }
                else
                {
                    m_Target.resetSeedOnPlay = false;
                    m_Target.startSeed = chunk.startSeed;
                    m_Target.Reinit(false);
                    if (chunk.prewarmCount != 0u)
                    {
                        m_Target.SendEvent(chunk.prewarmEvent);
                        m_Target.Simulate(chunk.prewarmDeltaTime, chunk.prewarmCount);
                    }
                }
            }

            private void OnLeaveChunk(int previousChunk)
            {
                if (m_Chunks[previousChunk].scrubbing)
                {
                    m_Target.Reinit(false);
                    RestoreVFXState();
                }
                m_ClipState = null;
            }

            private void PushDebugState(Debug.State scrubbing, double deltaTime)
            {
                var current = new Debug()
                {
                    state = scrubbing,
                    lastChunk = m_LastChunk,
                    lastEvent = m_LastEvent,
                    lastPlayableTime = m_LastPlayableTime,
                    lastDeltaTime = deltaTime,
                    vfxTime = m_Target.time,
                    clipState = m_ClipState?.ToArray(),
                };

                if (m_DebugFrame.Count > 5)
                {
                    m_DebugFrame.Dequeue();
                }
                m_DebugFrame.Enqueue(current);
            }

            private static double Abs(double a)
            {
                return a < 0.0 ? -a : a;
            }

            bool IsTimeInChunk(double time, int index)
            {
                var chunk = m_Chunks[index];
                return chunk.begin <= time && time < chunk.end;
            }

            public void Update(double playableTime, float deltaTime)
            {
                var paused = deltaTime == 0.0;
                var playingBackward = playableTime < m_LastPlayableTime;
                var dbg = Debug.State.OutChunk;

                var currentChunkIndex = kErrorIndex;
                if (m_LastChunk != currentChunkIndex)
                {
                    if (IsTimeInChunk(playableTime, m_LastChunk))
                        currentChunkIndex = m_LastChunk;
                }

                if (currentChunkIndex == kErrorIndex)
                {
                    var startIndex = m_LastChunk != kErrorIndex ? (uint)m_LastEvent : 0u;
                    for (uint i = startIndex; i < startIndex + m_Chunks.Length; i++)
                    {
                        var actualIndex = (int)(i % m_Chunks.Length);
                        if (IsTimeInChunk(playableTime, actualIndex))
                        {
                            currentChunkIndex = actualIndex;
                            break;
                        }
                    }
                }

                var firstFrameOfChunk = false;

                if (m_LastChunk != currentChunkIndex)
                {
                    if (m_LastChunk != kErrorIndex)
                    {
                        OnLeaveChunk(m_LastChunk);
                    }
                    if (currentChunkIndex != kErrorIndex)
                    {
                        OnEnterChunk(currentChunkIndex);
                        firstFrameOfChunk = true;
                    }

                    m_LastChunk = currentChunkIndex;
                    m_LastEvent = kErrorIndex;
                }

                if (currentChunkIndex != kErrorIndex)
                {
                    dbg = Debug.State.Playing;

                    var chunk = m_Chunks[currentChunkIndex];
                    if (chunk.scrubbing)
                    {
                        m_Target.pause = paused;
                        var actualCurrentTime = chunk.begin + m_Target.time;
                        if (!firstFrameOfChunk)
                            actualCurrentTime -= chunk.prewarmOffset;

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
                            dbg = Debug.State.ScrubbingBackward;
                            actualCurrentTime = chunk.begin;
                            m_LastEvent = kErrorIndex;
                            OnEnterChunk(m_LastChunk);
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

                            var fixedStep = VFXManager.maxDeltaTime;
                            if (actualCurrentTime < expectedCurrentTime
                                && expectedCurrentTime - actualCurrentTime > s_MaximumScrubbingTime)
                            {
                                //Choose a bigger time step to reach the actual expected time
                                fixedStep = (float)((expectedCurrentTime - actualCurrentTime) * (double)VFXManager.maxDeltaTime / (double)s_MaximumScrubbingTime);
                            }

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
                                    if (dbg != Debug.State.ScrubbingBackward)
                                        dbg = Debug.State.ScrubbingForward;

                                    m_Target.Simulate((float)fixedStep, currentStepCount);
                                    actualCurrentTime += fixedStep * currentStepCount;
                                }
                                ProcessEvent(currentEventIndex, chunk);
                            }
                        }

                        //Sending incoming event
                        {
                            var eventList = GetEventsIndex(chunk, actualCurrentTime, playableTime, m_LastEvent);
                            foreach (var itEvent in eventList)
                                ProcessEvent(itEvent, chunk);
                        }
                    }
                    else //No scrubbing
                    {
                        m_Target.pause = false;
                        if (playingBackward)
                        {
                            var eventBehind = GetEventsIndex(chunk, playableTime, m_LastPlayableTime, kErrorIndex);
                            foreach (var itEvent in eventBehind)
                            {
                                var currentEvent = chunk.events[itEvent];
                                if (currentEvent.clipType == Event.ClipType.Enter)
                                {
                                    ProcessEvent(chunk.clips[currentEvent.clipIndex].exit, chunk);
                                    dbg = Debug.State.ScrubbingBackward;
                                }
                                else if (currentEvent.clipType == Event.ClipType.Exit)
                                {
                                    ProcessEvent(chunk.clips[currentEvent.clipIndex].enter, chunk);
                                    dbg = Debug.State.ScrubbingBackward;
                                }
                                //else: Ignore, we aren't playing single event backward
                            }
                            m_LastEvent = kErrorIndex; //TODOPAUL: Think twice, could it be an issue ?
                        }

                        var eventList = GetEventsIndex(chunk, m_LastPlayableTime, playableTime, m_LastEvent);
                        foreach (var itEvent in eventList)
                            ProcessEvent(itEvent, chunk);
                    }
                }
                m_LastPlayableTime = playableTime;
                PushDebugState(dbg, deltaTime);
            }

            void ProcessEvent(int eventIndex, Chunk currentChunk)
            {
                if (eventIndex == kErrorIndex)
                    return;

                m_LastEvent = eventIndex;
                var currentEvent = currentChunk.events[eventIndex];

                //TODOPAUL, these update are only debug code
                if (currentEvent.clipType == Event.ClipType.Enter)
                {
                    if (m_ClipState[currentEvent.clipIndex])
                        throw new InvalidOperationException(); //TODOPAUL remove exception here

                    m_ClipState[currentEvent.clipIndex] = true;
                }
                else if (currentEvent.clipType == Event.ClipType.Exit)
                {
                    if (!m_ClipState[currentEvent.clipIndex])
                        throw new InvalidOperationException(); //TODOPAUL remove exception here

                    m_ClipState[currentEvent.clipIndex] = false;
                }

                m_Target.SendEvent(currentEvent.nameId, currentEvent.attribute);
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

            static VFXEventAttribute ComputeAttribute(VisualEffect vfx, EventAttributes attributes)
            {
                if (attributes.content == null || attributes.content.Length == 0)
                    return null;

                var vfxAttribute = vfx.CreateVFXEventAttribute();
                bool anyRelevantAttribute = false;
                foreach (var attribute in attributes.content)
                {
                    if (attribute != null)
                    {
                        anyRelevantAttribute = anyRelevantAttribute || attribute.ApplyToVFX(vfxAttribute);
                    }
                }

                if (!anyRelevantAttribute)
                    return null;

                return vfxAttribute;
            }

            static IEnumerable<Event> ComputeRuntimeEvent(VisualEffectControlPlayableBehaviour behavior, VisualEffect vfx)
            {
                var events = VFXTimeSpaceHelper.GetEventNormalizedSpace(VisualEffectPlayableSerializedEvent.TimeSpace.Absolute, behavior);
                foreach (var itEvent in events)
                {
                    //Apply clamping on the fly
                    var absoluteTime = itEvent.time;
                    if (absoluteTime > behavior.clipEnd)
                        absoluteTime = behavior.clipEnd;
                    if (absoluteTime < behavior.clipStart)
                        absoluteTime = behavior.clipStart;

                    yield return new Event()
                    {
                        attribute = ComputeAttribute(vfx, itEvent.eventAttributes),
                        nameId = itEvent.name,
                        time = absoluteTime,
                        clipIndex = -1,
                        clipType = Event.ClipType.None
                    };
                }
            }

            class VisualEffectControlPlayableBehaviourComparer : IComparer<VisualEffectControlPlayableBehaviour>
            {
                public int Compare(VisualEffectControlPlayableBehaviour x, VisualEffectControlPlayableBehaviour y)
                {
                    return x.clipStart.CompareTo(y.clipStart);
                }
            }

            public void RestoreVFXState()
            {
                //Target could have been destroyed
                if (m_Target == null)
                    return;

                m_Target.pause = false;
                m_Target.startSeed = m_BackupStartSeed;
                m_Target.resetSeedOnPlay = m_BackupReseedOnPlay;
            }

            public void Init(Playable playable, VisualEffect vfx)
            {
                m_Target = vfx;
                m_BackupStartSeed = m_Target.startSeed;
                m_BackupReseedOnPlay = m_Target.resetSeedOnPlay;

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
                        || inputBehavior.clipStart > chunks.Peek().end
                        || inputBehavior.scrubbing != chunks.Peek().scrubbing
                        || inputBehavior.startSeed != chunks.Peek().startSeed
                        || inputBehavior.prewarmStepCount != 0u)
                    {
                        chunks.Push(new Chunk()
                        {
                            begin = inputBehavior.clipStart,
                            events = new Event[0],
                            clips = new Clip[0],
                            scrubbing = inputBehavior.scrubbing,
                            startSeed = inputBehavior.startSeed,

                            prewarmCount = inputBehavior.prewarmStepCount,
                            prewarmDeltaTime = inputBehavior.prewarmDeltaTime,
                            prewarmEvent = inputBehavior.prewarmEvent != null ? inputBehavior.prewarmEvent : 0,
                            prewarmOffset = (double)inputBehavior.prewarmStepCount * inputBehavior.prewarmDeltaTime
                        });
                    }

                    var currentChunk = chunks.Peek();
                    currentChunk.end = inputBehavior.clipEnd;
                    var currentsEvents = ComputeRuntimeEvent(inputBehavior, vfx);

                    if (!currentChunk.scrubbing)
                    {
                        //TODOPAUL: Not optimal due O² search (+ lot of garbage)
                        var eventsWithIndex = currentsEvents.Select((e, i) =>
                        {
                            return new
                            {
                                evt = e,
                                index = i
                            };
                        }).OrderBy(o => o.evt.time)
                        .ToList();

                        var newClips = new Clip[inputBehavior.clipEventsCount];
                        var newEvents = new List<Event>();
                        foreach (var itEvent in eventsWithIndex)
                        {
                            var newEvent = itEvent.evt;
                            if (itEvent.index < inputBehavior.clipEventsCount * 2)
                            {
                                var actualSortedClipIndex = currentChunk.events.Length + eventsWithIndex.FindIndex(o => o.index == itEvent.index);
                                var localClipIndex = itEvent.index / 2;
                                newEvent.clipIndex = localClipIndex + currentChunk.clips.Length;
                                if (itEvent.index % 2 == 0)
                                {
                                    newEvent.clipType = Event.ClipType.Enter;
                                    newClips[localClipIndex].enter = actualSortedClipIndex;
                                }
                                else
                                {
                                    newEvent.clipType = Event.ClipType.Exit;
                                    newClips[localClipIndex].exit = actualSortedClipIndex;
                                }
                                newEvents.Add(newEvent);
                            }
                            else //Not a clip event
                            {
                                newEvents.Add(newEvent);
                            }
                        }
                        currentChunk.clips = currentChunk.clips.Concat(newClips).ToArray();
                        currentChunk.events = currentChunk.events.Concat(newEvents).ToArray();
                    }
                    else
                    {
                        //No need to compute clip information
                        currentsEvents = currentsEvents.OrderBy(o => o.time);
                        currentChunk.events = currentChunk.events.Concat(currentsEvents).ToArray();
                    }


                    chunks.Pop();
                    chunks.Push(currentChunk);
                }
                m_Chunks = chunks.Reverse().ToArray();
            }

            public void Release()
            {
                RestoreVFXState();
            }
        }

        ScrubbingCacheHelper m_ScrubbingCacheHelper;
        VisualEffect m_Target;

        public Queue<ScrubbingCacheHelper.Debug> GetDebugInfo()
        {
            return m_ScrubbingCacheHelper?.m_DebugFrame;
        }

        public override void PrepareFrame(Playable playable, FrameData data)
        {
            if (m_Target == null)
                return;

            if (m_ScrubbingCacheHelper == null)
            {
                m_ScrubbingCacheHelper = new ScrubbingCacheHelper();
                m_ScrubbingCacheHelper.Init(playable, m_Target);
            }

            var globalTime = playable.GetTime();
            var deltaTime = data.deltaTime;
            m_ScrubbingCacheHelper.Update(globalTime, deltaTime);
        }

        public override void ProcessFrame(Playable playable, FrameData data, object playerData)
        {
            var vfx = playerData as VisualEffect;
            if (m_Target == vfx)
                return;

            if (vfx != null)
            {
                if (vfx.visualEffectAsset == null)
                    vfx = null;

                if (!vfx.isActiveAndEnabled)
                    vfx = null;
            }

            m_Target = vfx;
            if (m_Target)
                m_Target.pause = true;

            InvalidateScrubbingHelper();
        }

        public override void OnBehaviourPause(Playable playable, FrameData data)
        {
            base.OnBehaviourPause(playable, data);
            PrepareFrame(playable, data);
        }

        void InvalidateScrubbingHelper()
        {
            if (m_ScrubbingCacheHelper != null)
            {
                m_ScrubbingCacheHelper.Release();
                m_ScrubbingCacheHelper = null;
            }
        }

        public override void OnPlayableCreate(Playable playable)
        {
            InvalidateScrubbingHelper();
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            InvalidateScrubbingHelper();
            m_Target = null;
        }
    }
}
#endif
