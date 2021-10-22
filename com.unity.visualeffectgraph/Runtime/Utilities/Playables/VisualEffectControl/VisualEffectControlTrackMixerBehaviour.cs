#if VFX_HAS_TIMELINE
using UnityEngine;
using UnityEngine.Playables;
using System.Linq;
using System.Collections.Generic;
using System;

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

                public Event[] events;
                public Clip[] clips;
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
            private bool[] m_ClipState; //TODOPAUL: Actually, it's only useful for debug
            private double m_LastPlayableTime = double.MinValue;

            private void OnEnterChunk(VisualEffect vfx, int currentChunk)
            {
                vfx.Reinit(false);
                if (!m_Chunks[currentChunk].scrubbing)
                {
                    m_ClipState = new bool[m_Chunks[currentChunk].clips.Length];
                }
            }

            private void OnLeaveChunk(VisualEffect vfx, int previousChunk)
            {
                m_ClipState = null;
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
                    if (m_LastChunk != kErrorIndex)
                        OnLeaveChunk(vfx, m_LastChunk);
                    if (currentChunkIndex != kErrorIndex)
                        OnEnterChunk(vfx, currentChunkIndex);

                    m_LastChunk = currentChunkIndex;
                    m_LastEvent = kErrorIndex;
                }

                if (currentChunkIndex != kErrorIndex)
                {
                    dbg = dbg_state.Playing;

                    var chunk = m_Chunks[currentChunkIndex];
                    if (chunk.scrubbing)
                    {
                        vfx.pause = paused;
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
                            m_LastEvent = kErrorIndex;
                            OnEnterChunk(vfx, m_LastChunk);
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
                    else //No scrubbing
                    {
                        vfx.pause = false;
                        if (playingBackward)
                        {
                            var eventBehind = GetEventsIndex(chunk, playableTime, m_LastPlayableTime, kErrorIndex);
                            foreach (var itEvent in eventBehind)
                            {
                                var currentEvent = chunk.events[itEvent];
                                if (currentEvent.clipType == Event.ClipType.Enter)
                                {
                                    ProcessEvent(chunk.clips[currentEvent.clipIndex].exit, chunk, vfx);
                                    dbg = dbg_state.ScrubbingBackward;
                                }
                                else if (currentEvent.clipType == Event.ClipType.Exit)
                                {
                                    ProcessEvent(chunk.clips[currentEvent.clipIndex].enter, chunk, vfx);
                                    dbg = dbg_state.ScrubbingBackward;
                                }
                                //Else: Ignore, we aren't playing single event backward
                            }
                            m_LastEvent = kErrorIndex; //TODOPAUL: Think twice, could it be an issue ?
                        }

                        var eventList = GetEventsIndex(chunk, m_LastPlayableTime, playableTime, m_LastEvent);
                        foreach (var itEvent in eventList)
                            ProcessEvent(itEvent, chunk, vfx);

                        UpdateScrubbingState(vfx, dbg);
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

                vfx.SendEvent(currentEvent.nameId, currentEvent.attribute);
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
                foreach (var attr in attributes.content)
                {
                    if (attr is EventAttributeValue<Vector3>)
                    {
                        var test = attr as EventAttributeValue<Vector3>;
                        vfxAttribute.SetVector3((int)attr.id, test.value);
                    }
                    //TODOPAUL: finish it here or abstract it
                }
                return vfxAttribute;
            }

            static IEnumerable<Event> ComputeRuntimeEvent(VisualEffectControlPlayableBehaviour behavior, VisualEffect vfx)
            {
                var events = VisualEffectPlayableSerializedEvent.GetEventNormalizedSpace(VisualEffectPlayableSerializedEvent.TimeSpace.Absolute, behavior);
                foreach (var itEvent in events)
                {
                    double absoluteTime = itEvent.time;

                    //TODOPAUL: Should not be there but in UX
                    if (absoluteTime > behavior.clipEnd)
                        absoluteTime = behavior.clipEnd;
                    if (absoluteTime < behavior.clipStart)
                        absoluteTime = behavior.clipStart;

                    yield return new Event()
                    {
                        attribute = ComputeAttribute(vfx, itEvent.eventAttributes),
                        nameId = Shader.PropertyToID(itEvent.name),
                        time = absoluteTime,
                        clipIndex = -1,
                        clipType = Event.ClipType.None
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
                        || inputBehavior.clipStart > chunks.Peek().end
                        || inputBehavior.scrubbing != chunks.Peek().scrubbing
                        || inputBehavior.startSeed != chunks.Peek().startSeed)
                    {
                        chunks.Push(new Chunk()
                        {
                            begin = inputBehavior.clipStart,
                            events = new Event[0],
                            clips = new Clip[0],
                            scrubbing = inputBehavior.scrubbing,
                            startSeed = inputBehavior.startSeed
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
        }

        ScrubbingCacheHelper m_ScrubbingCacheHelper;
        VisualEffect m_Target;

        public override void PrepareFrame(Playable playable, FrameData data)
        {
            //TODOPAUL we aren't detecting correctly the change of visualEffectAsset
            if (m_Target == null || m_Target.visualEffectAsset == null)
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

        public override void ProcessFrame(Playable playable, FrameData data, object playerData)
        {
            SetDefaults(playerData as VisualEffect);
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
            if (m_Target)
                m_Target.pause = true; //Awaiting for the first clip to call Reinit
            m_ScrubbingCacheHelper = null;
        }
    }
}
#endif
