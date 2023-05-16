#if VFX_HAS_TIMELINE
using UnityEngine.Playables;
using System.Linq;
using System.Collections.Generic;
using System;

namespace UnityEngine.VFX
{
#if UNITY_EDITOR
    class VisualEffectControlErrorHelper
    {
        public struct MaxScrubbingWarning
        {
            public float requestedTime;
            public float fixedTimeStep;
            public VisualEffectControlTrackController controller;
        }

        private void UpdateConflictingControlTrack()
        {
            //Detect potential issue with multiple track controlling the same vfx with some scrubbing
            m_ConflictingControlTrack.Clear();
            foreach (var group in m_RegisteredControlTrack.GroupBy(o => o.GetTarget()))
            {
                if (group.Count() > 1 && group.Any(o => o.GetScrubbing()))
                {
                    m_ConflictingControlTrack.Add(group.ToArray());
                }
            }
        }

        public void RegisterControlTrack(VisualEffectControlTrackController controller)
        {
            m_RegisteredControlTrack.Add(controller);
            UpdateConflictingControlTrack();
        }

        public void UnregisterControlTrack(VisualEffectControlTrackController controller)
        {
            UnregisterScrubbingWarning(controller);
            m_RegisteredControlTrack.RemoveAll(o => o == controller);
            UpdateConflictingControlTrack();
        }

        public void RegisterScrubbingWarning(VisualEffectControlTrackController controller, float requestedTime, float fixedTimeStep)
        {
            UnregisterScrubbingWarning(controller);
            m_ScrubbingWarnings.Add(new MaxScrubbingWarning()
            {
                controller = controller,
                requestedTime = requestedTime,
                fixedTimeStep = fixedTimeStep
            });
        }

        public void UnregisterScrubbingWarning(VisualEffectControlTrackController controller)
        {
            m_ScrubbingWarnings.RemoveAll(o => o.controller == controller);
        }

        public IEnumerable<VisualEffectControlTrackController[]> GetConflictingControlTrack()
        {
            return m_ConflictingControlTrack;
        }

        public IEnumerable<MaxScrubbingWarning> GetMaxScrubbingWarnings()
        {
            return m_ScrubbingWarnings;
        }

        public bool AnyError()
        {
            return m_ScrubbingWarnings.Any() || m_ConflictingControlTrack.Any();
        }

        private static VisualEffectControlErrorHelper m_Instance = new VisualEffectControlErrorHelper();
        public static VisualEffectControlErrorHelper instance => m_Instance;
        List<VisualEffectControlTrackController> m_RegisteredControlTrack = new List<VisualEffectControlTrackController>();
        List<VisualEffectControlTrackController[]> m_ConflictingControlTrack = new List<VisualEffectControlTrackController[]>();
        List<MaxScrubbingWarning> m_ScrubbingWarnings = new List<MaxScrubbingWarning>();
    }
#endif

    class VisualEffectControlTrackController
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
        List<int> m_EventListIndexCache = new ();

#if UNITY_EDITOR
        bool[] m_ClipState;
        bool m_HasScrubbingWarnings;
        bool m_Scrubbing;
        PlayableDirector m_Director;
        VisualEffectControlTrack m_Track;

        public bool GetScrubbing()
        {
            return m_Scrubbing;
        }

        public VisualEffect GetTarget()
        {
            return m_Target;
        }

        public VisualEffectControlTrack GetTrack()
        {
            return m_Track;
        }

        public PlayableDirector GetDirector()
        {
            return m_Director;
        }
#endif
        VisualEffect m_Target;
        bool m_BackupReseedOnPlay;
        uint m_BackupStartSeed;
        Chunk[] m_Chunks;

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
                //Using infinity as virtual limit to force include events where time is exactly 0 or duration.
                ProcessNoScrubbingEvents(previousChunk, m_LastPlayableTime, leavingGoingBeforeClip ? double.NegativeInfinity : double.PositiveInfinity);
            }

            RestoreVFXState(previousChunk.scrubbing, previousChunk.reinitEnter);

#if UNITY_EDITOR
            m_ClipState = null;
#endif
        }

        bool IsTimeInChunk(double time, int index)
        {
            var chunk = m_Chunks[index];
            return chunk.begin <= time && time < chunk.end;
        }

        public void Update(double playableTime, float deltaTime)
        {
#if UNITY_EDITOR
            if (m_HasScrubbingWarnings)
            {
                VisualEffectControlErrorHelper.instance.UnregisterScrubbingWarning(this);
                m_HasScrubbingWarnings = false;
            }
#endif
            var paused = deltaTime == 0.0;
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
                        if (Math.Abs(m_LastPlayableTime - actualCurrentTime) < VFXManager.maxDeltaTime)
                        {
                            //Remove the float part from VFX and only keep double precision
                            actualCurrentTime = m_LastPlayableTime;
                        }
                        else
                        {
                            //VFX is too late on timeline (or a bit ahead), we will have to launch simulate
                            //Warning, in that case, event could have been already sent
                            //m_LastEvent status prevents sending twice the same event
                        }
                    }
                    else
                    {
                        actualCurrentTime = chunk.begin;
                        m_LastEvent = kErrorIndex;
                        OnEnterChunk(m_LastChunk);
                    }

                    double expectedCurrentTime;
                    if (paused)
                        expectedCurrentTime = playableTime;
                    else
                        expectedCurrentTime = playableTime - VFXManager.fixedTimeStep;

                    //Sending missed event (in case of VFX ahead)
                    if (m_LastPlayableTime < actualCurrentTime)
                    {
                        var eventList = m_EventListIndexCache;
                        GetEventsIndex(chunk, m_LastPlayableTime, actualCurrentTime, m_LastEvent, eventList);
                        foreach (var itEvent in eventList)
                            ProcessEvent(itEvent, chunk);
                    }

                    if (actualCurrentTime < expectedCurrentTime)
                    {
                        //Process adjustment if actualCurrentTime < expectedCurrentTime
                        var eventList = m_EventListIndexCache;
                        GetEventsIndex(chunk, actualCurrentTime, expectedCurrentTime, m_LastEvent, eventList);
                        var eventCount = eventList.Count;
                        var nextEvent = 0;

                        var maxScrubTime = VFXManager.maxScrubTime;
                        var fixedStep = VFXManager.maxDeltaTime;
                        if (expectedCurrentTime - actualCurrentTime > maxScrubTime)
                        {
                            //Choose a bigger time step to reach the actual expected time
                            fixedStep = (float)((expectedCurrentTime - actualCurrentTime) * (double)VFXManager.maxDeltaTime / (double)maxScrubTime);
#if UNITY_EDITOR
                            VisualEffectControlErrorHelper.instance.RegisterScrubbingWarning(this, (float)(expectedCurrentTime - actualCurrentTime), fixedStep);
                            m_HasScrubbingWarnings = true;
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
                                    //We reached the maximum precision according to the current fixedStep & no more event
                                    break;
                                }
                            }

                            if (currentStepCount != 0)
                            {
                                m_Target.Simulate((float)fixedStep, currentStepCount);
                                actualCurrentTime += fixedStep * currentStepCount;
                            }
                            ProcessEvent(currentEventIndex, chunk);
                        }
                    }

                    //Sending incoming event
                    if (actualCurrentTime < playableTime)
                    {
                        var eventList = m_EventListIndexCache;
                        GetEventsIndex(chunk, actualCurrentTime, playableTime, m_LastEvent, eventList);
                        foreach (var itEvent in eventList)
                            ProcessEvent(itEvent, chunk);
                    }
                }
                else //No scrubbing, only update events
                {
                    m_Target.pause = false;
                    ProcessNoScrubbingEvents(chunk, m_LastPlayableTime, playableTime);
                }
            }
            m_LastPlayableTime = playableTime;
        }

        void ProcessNoScrubbingEvents(Chunk chunk, double oldTime, double newTime)
        {
#if UNITY_EDITOR
            if (chunk.scrubbing)
                throw new InvalidOperationException();
#endif
            if (newTime < oldTime) // == playingBackward
            {
                var eventBehind = m_EventListIndexCache;
                GetEventsIndex(chunk, newTime, oldTime, kErrorIndex, eventBehind);
                if (eventBehind.Count > 0)
                {
                    for (int index = eventBehind.Count - 1; index >= 0; index--)
                    {
                        var itEvent = eventBehind[index];
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
                var eventAhead = m_EventListIndexCache;
                GetEventsIndex(chunk, oldTime, newTime, m_LastEvent, eventAhead);
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

        static void GetEventsIndex(Chunk chunk, double minTime, double maxTime, int lastIndex, List<int> eventListIndex)
        {
            eventListIndex.Clear();

            var startIndex = lastIndex == kErrorIndex ? 0 : lastIndex + 1;
            for (int i = startIndex; i < chunk.events.Length; ++i)
            {
                var currentEvent = chunk.events[i];
                //We are retrieving events between [minTime, maxTime[
                //If currentEvent.time == maxTime, skip, it prevents the multiple sending of the same event.
                if (currentEvent.time >= maxTime)
                    break;

                if (minTime <= currentEvent.time)
                    eventListIndex.Add(i);
            }
        }

        static VFXEventAttribute ComputeAttribute(VisualEffect vfx, EventAttributes attributes)
        {
            if (attributes.content == null || attributes.content.Length == 0)
                return null;

            var vfxAttribute = vfx.CreateVFXEventAttribute();
            if (attributes.content.Count(x => x?.ApplyToVFX(vfxAttribute) == true) == 0)
            {
                //We didn't setup any vfxEventAttribute, ignoring the event payload
                return null;
            }

            return vfxAttribute;
        }

        static IEnumerable<Event> ComputeRuntimeEvent(VisualEffectControlPlayableBehaviour behavior, VisualEffect vfx)
        {
            var events = VFXTimeSpaceHelper.GetEventNormalizedSpace(PlayableTimeSpace.Absolute, behavior);
            foreach (var itEvent in events)
            {
                //Apply clamping on the fly
                var absoluteTime = Math.Max(behavior.clipStart, Math.Min(behavior.clipEnd, itEvent.time));

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

        public void Init(Playable playable, VisualEffect vfx, VisualEffectControlTrack parentTrack)
        {
            m_Target = vfx;
#if UNITY_EDITOR
            m_Director = playable.GetGraph().GetResolver() as PlayableDirector;
            m_Track = parentTrack;
#endif
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
                if (!chunks.Any()
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
#if UNITY_EDITOR
                    m_Scrubbing = true;
#endif

                    //No need to compute clip information
                    currentsEvents = currentsEvents.OrderBy(o => o.time);
                    currentChunk.events = currentChunk.events.Concat(currentsEvents).ToArray();
                }


                chunks.Pop();
                chunks.Push(currentChunk);
            }
            m_Chunks = chunks.Reverse().ToArray();
#if UNITY_EDITOR
            VisualEffectControlErrorHelper.instance.RegisterControlTrack(this);
#endif
        }

        public void Release()
        {
#if UNITY_EDITOR
            VisualEffectControlErrorHelper.instance.UnregisterControlTrack(this);
#endif
            RestoreVFXState();
        }
    }

    class VisualEffectControlTrackMixerBehaviour : PlayableBehaviour
    {
        VisualEffectControlTrackController m_ScrubbingCacheHelper;
#if UNITY_EDITOR
        VisualEffectControlTrack m_ParentTrack;
#endif
        VisualEffect m_Target;
        bool m_ReinitWithBinding;
        bool m_ReinitWithUnbinding;

        public void Init(VisualEffectControlTrack parentTrack, bool reinitWithBinding, bool reinitWithUnbinding)
        {
#if UNITY_EDITOR
            m_ParentTrack = parentTrack;
#endif
            m_ReinitWithBinding = reinitWithBinding;
            m_ReinitWithUnbinding = reinitWithUnbinding;
        }

        public override void PrepareFrame(Playable playable, FrameData data)
        {
            if (m_Target == null)
                return;

            if (m_ScrubbingCacheHelper == null)
            {
                m_ScrubbingCacheHelper = new VisualEffectControlTrackController();

                VisualEffectControlTrack parentTrack = null;
#if UNITY_EDITOR
                parentTrack = m_ParentTrack;
#endif
                m_ScrubbingCacheHelper.Init(playable, m_Target, parentTrack);
            }

            var duration = playable.GetOutput(0).GetDuration();
            var globalTime = playable.GetTime();
            var numberOfFullLoops = (int)(globalTime / duration);
            globalTime -= numberOfFullLoops * duration;

            var deltaTime = data.deltaTime;
            m_ScrubbingCacheHelper.Update(globalTime, deltaTime);
        }

        void BindVFX(VisualEffect vfx)
        {
            m_Target = vfx;
            if (m_Target != null && m_ReinitWithBinding)
            {
                m_Target.Reinit(false);
            }
        }

        void UnbindVFX()
        {
            if (m_Target != null && m_ReinitWithUnbinding)
            {
                m_Target.Reinit(true);
            }
            m_Target = null;
        }

        public override void ProcessFrame(Playable playable, FrameData data, object playerData)
        {
            var vfx = playerData as VisualEffect;
            if (m_Target == vfx)
                return;

            UnbindVFX();

            if (vfx != null)
            {
                if (vfx.visualEffectAsset == null)
                    vfx = null;
                else if (!vfx.isActiveAndEnabled)
                    vfx = null;
            }

            BindVFX(vfx);
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
            UnbindVFX();
        }
    }
}
#endif
