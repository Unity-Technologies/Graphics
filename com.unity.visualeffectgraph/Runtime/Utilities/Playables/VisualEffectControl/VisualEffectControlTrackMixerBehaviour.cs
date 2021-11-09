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
#if UNITY_EDITOR
        public struct MaxScrubbingWarning
        {
            public float requestedTime;
            public float fixedTimeStep;
            public VisualEffect target;
        }

        static uint s_WarningId = 0u;
        static List<(uint, MaxScrubbingWarning)> s_RegisteredWarnings = new List<(uint, MaxScrubbingWarning)>();
        static public uint RegisterScrubbingWarning(MaxScrubbingWarning warning)
        {
            var currentID = s_WarningId++;
            if (currentID == uint.MaxValue)
                currentID = 0u;
            s_RegisteredWarnings.Add((currentID, warning));
            return currentID;
        }

        static public IEnumerable<MaxScrubbingWarning> GetScrubbingWarnings()
        {
            foreach (var registeredWarning in s_RegisteredWarnings)
                yield return registeredWarning.Item2;
        }

        static public void UnregisterScrubbingWarning(uint id)
        {
            s_RegisteredWarnings.RemoveAll(o => o.Item1 == id);
        }
#endif
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
                public bool reinitEnter;
                public bool reinitExit;
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
            double m_LastPlayableTime = double.MinValue;

#if UNITY_EDITOR
            uint m_LastErrorID = uint.MaxValue;
            bool[] m_ClipState;
            Queue<Debug> m_DebugFrame = new Queue<Debug>(); //TODOPAUL: can be cleaned after QA verification
            public IEnumerable<Debug> GetDebugFrames()
            {
                return m_DebugFrame;
            }
#endif
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

            //TODOPAUL if right thing todo => store it in VFXManager & use reflection in VFXSettings UX
            public static float s_MaximumScrubbingTime = 30.0f;

            private void OnEnterChunk(int currentChunk)
            {
                var chunk = m_Chunks[currentChunk];
#if UNITY_EDITOR
                if (!chunk.scrubbing)
                    m_ClipState = new bool[chunk.clips.Length];
#endif
                if (chunk.reinitEnter)
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

            private void OnLeaveChunk(int previousChunkIndex, bool leavingGoingBeforeClip)
            {
                var previousChunk = m_Chunks[previousChunkIndex];

                if (previousChunk.reinitExit)
                {
                    m_Target.Reinit(false);
                }
                else
                {
#if UNITY_EDITOR
                    if (previousChunk.scrubbing)
                        throw new InvalidOperationException();
#endif
                    ProcessNoScrubbingEvents(previousChunk, m_LastPlayableTime, leavingGoingBeforeClip ? previousChunk.begin : previousChunk.end);
                }

                RestoreVFXState(previousChunk.scrubbing, previousChunk.reinitEnter);

#if UNITY_EDITOR
                m_ClipState = null;
#endif
            }

#if UNITY_EDITOR
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
#endif
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
#if UNITY_EDITOR
                if (m_LastErrorID != uint.MaxValue)
                {
                    UnregisterScrubbingWarning(m_LastErrorID);
                    m_LastErrorID = uint.MaxValue;
                }
#endif

                var paused = deltaTime == 0.0;
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
                        var before = playableTime < m_Chunks[m_LastChunk].begin;
                        OnLeaveChunk(m_LastChunk, before);
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

                        var playingBackward = playableTime < m_LastPlayableTime;
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
#if UNITY_EDITOR
                                m_LastErrorID = RegisterScrubbingWarning(new MaxScrubbingWarning()
                                {
                                    fixedTimeStep = fixedStep,
                                    requestedTime = (float)(expectedCurrentTime - actualCurrentTime),
                                    target = m_Target
                                });
#endif
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
                        ProcessNoScrubbingEvents(chunk, m_LastPlayableTime, playableTime);
                    }
                }
                m_LastPlayableTime = playableTime;
#if UNITY_EDITOR
                PushDebugState(dbg, deltaTime);
#endif
            }

            void ProcessNoScrubbingEvents(Chunk chunk, double oldTime, double newTime)
            {
#if UNITY_EDITOR
                if (chunk.scrubbing)
                    throw new InvalidOperationException();
#endif
                if (newTime < oldTime) // == playingBackward
                {
                    var eventBehind = GetEventsIndex(chunk, newTime, oldTime, kErrorIndex);
                    if (eventBehind.Any())
                    {
                        foreach (var itEvent in eventBehind.Reverse())
                        {
                            var currentEvent = chunk.events[itEvent];
                            if (currentEvent.clipType == Event.ClipType.Enter)
                            {
                                ProcessEvent(chunk.clips[currentEvent.clipIndex].exit, chunk);
                            }
                            else if (currentEvent.clipType == Event.ClipType.Exit)
                            {
                                ProcessEvent(chunk.clips[currentEvent.clipIndex].enter, chunk);
                            }
                            //else: Ignore, we aren't playing single event backward
                        }

                        //The last event will be always invalid in case of scrubbing backward
                        m_LastEvent = kErrorIndex;
                    }
                }
                else
                {
                    var eventAhead = GetEventsIndex(chunk, oldTime, newTime, m_LastEvent);
                    foreach (var itEvent in eventAhead)
                        ProcessEvent(itEvent, chunk);
                }
            }

            void ProcessEvent(int eventIndex, Chunk currentChunk)
            {
                if (eventIndex == kErrorIndex)
                    return;

                m_LastEvent = eventIndex;
                var currentEvent = currentChunk.events[eventIndex];

#if UNITY_EDITOR
                if (currentEvent.clipType == Event.ClipType.Enter)
                {
                    if (m_ClipState[currentEvent.clipIndex])
                        throw new InvalidOperationException();

                    m_ClipState[currentEvent.clipIndex] = true;
                }
                else if (currentEvent.clipType == Event.ClipType.Exit)
                {
                    if (!m_ClipState[currentEvent.clipIndex])
                        throw new InvalidOperationException();

                    m_ClipState[currentEvent.clipIndex] = false;
                }
#endif

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

            public void RestoreVFXState(bool restorePause = true, bool restoreSeedState = true)
            {
                //Target could have been destroyed
                if (m_Target == null)
                    return;

                if (restorePause)
                    m_Target.pause = false;

                if (restoreSeedState)
                {
                    m_Target.startSeed = m_BackupStartSeed;
                    m_Target.resetSeedOnPlay = m_BackupReseedOnPlay;
                }
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
                    var inputPlayable = playable.GetInput(i);
                    if (inputPlayable.GetPlayableType() != typeof(VisualEffectControlPlayableBehaviour))
                        continue;

                    var inputVFXPlayable = (ScriptPlayable<VisualEffectControlPlayableBehaviour>)inputPlayable;
                    var inputBehavior = inputVFXPlayable.GetBehaviour();
                    if (inputBehavior != null)
                        playableBehaviors.Add(inputBehavior);
                }

                playableBehaviors.Sort(new VisualEffectControlPlayableBehaviourComparer());
                foreach (var inputBehavior in playableBehaviors)
                {
                    if (   !chunks.Any()
                        || inputBehavior.clipStart > chunks.Peek().end
                        || inputBehavior.scrubbing != chunks.Peek().scrubbing
                        || (!inputBehavior.scrubbing && (inputBehavior.reinitEnter || chunks.Peek().reinitExit))
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
                            reinitEnter = inputBehavior.reinitEnter,
                            reinitExit = inputBehavior.reinitExit,

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
                        var sortedEventWithSourceIndex = currentsEvents.Select((e, i) =>
                        {
                            return new
                            {
                                evt = e,
                                sourceIndex = i
                            };
                        }).OrderBy(o => o.evt.time)
                        .ToList();

                        var newClips = new Clip[inputBehavior.clipEventsCount];
                        var newEvents = new List<Event>();
                        for (int actualIndex = 0; actualIndex < sortedEventWithSourceIndex.Count; actualIndex++)
                        {
                            var newEvent = sortedEventWithSourceIndex[actualIndex].evt;
                            var sourceIndex = sortedEventWithSourceIndex[actualIndex].sourceIndex;
                            if (sourceIndex < inputBehavior.clipEventsCount * 2)
                            {
                                var actualSortedClipIndex = currentChunk.events.Length + actualIndex;
                                var localClipIndex = sourceIndex / 2;
                                newEvent.clipIndex = localClipIndex + currentChunk.clips.Length;
                                if (sourceIndex % 2 == 0)
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
#if UNITY_EDITOR
                if (m_LastErrorID != uint.MaxValue)
                    UnregisterScrubbingWarning(m_LastErrorID);
#endif
                RestoreVFXState();
            }
        }

        ScrubbingCacheHelper m_ScrubbingCacheHelper;
        VisualEffect m_Target;

#if UNITY_EDITOR
        public IEnumerable<ScrubbingCacheHelper.Debug> GetDebugFrames()
        {
            return m_ScrubbingCacheHelper?.GetDebugFrames();
        }
#endif

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
                else if (!vfx.isActiveAndEnabled)
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
