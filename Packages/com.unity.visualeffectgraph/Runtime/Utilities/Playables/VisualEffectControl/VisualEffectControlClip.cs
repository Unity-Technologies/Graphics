#if VFX_HAS_TIMELINE
using System;
using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Runtime.CompilerServices;
using UnityEngine.VFX.Utility;

[assembly: InternalsVisibleTo("VisualEffect.Playable.Editor")]
namespace UnityEngine.VFX
{
    [Serializable]
    class VisualEffectControlClip : PlayableAsset, ITimelineClipAsset
    {
        public ClipCaps clipCaps
        {
            get { return ClipCaps.None; }
        }

        public double clipStart { get; set; }
        public double clipEnd { get; set; }

        [NotKeyable]
        public bool scrubbing = true;
        [NotKeyable]
        public uint startSeed;

        public enum ReinitMode
        {
            None,
            OnExitClip,
            OnEnterClip,
            OnEnterOrExitClip
        }
        [NotKeyable]
        public ReinitMode reinit = ReinitMode.OnEnterOrExitClip;

        [Serializable]
        public struct PrewarmClipSettings
        {
            public bool enable;
            public uint stepCount;
            public float deltaTime;
            public ExposedProperty eventName;
        }

        [NotKeyable]
        public PrewarmClipSettings prewarm = new PrewarmClipSettings()
        {
            enable = false,
            stepCount = 20u,
            deltaTime = 0.05f,
            eventName = VisualEffectAsset.PlayEventName
        };

        [Serializable]
        public struct ClipEvent
        {
            public static Color defaultEditorColor = new Color32(123, 158, 5, 255);
            public Color editorColor;
            public VisualEffectPlayableSerializedEventNoColor enter;
            public VisualEffectPlayableSerializedEventNoColor exit;
        }

        [NotKeyable]
        public List<ClipEvent> clipEvents = new List<ClipEvent>()
        {
            new ClipEvent()
            {
                editorColor = ClipEvent.defaultEditorColor,
                enter = new VisualEffectPlayableSerializedEventNoColor()
                {
                    name = VisualEffectAsset.PlayEventName,
                    time = 0.0,
                    timeSpace = PlayableTimeSpace.AfterClipStart,
                    eventAttributes = new EventAttributes() { content = Array.Empty<EventAttribute>() }
                },

                exit = new VisualEffectPlayableSerializedEventNoColor()
                {
                    name = VisualEffectAsset.StopEventName,
                    time = 0.0,
                    timeSpace = PlayableTimeSpace.BeforeClipEnd,
                    eventAttributes = new EventAttributes() { content = Array.Empty<EventAttribute>() }
                }
            }
        };

        [NotKeyable]
        public List<VisualEffectPlayableSerializedEvent> singleEvents = new List<VisualEffectPlayableSerializedEvent>();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<VisualEffectControlPlayableBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.clipStart = clipStart;
            behaviour.clipEnd = clipEnd;
            behaviour.scrubbing = scrubbing;
            behaviour.startSeed = startSeed;

            if (scrubbing)
            {
                behaviour.reinitEnter = true;
                behaviour.reinitExit = true;
            }
            else
            {
                switch (reinit)
                {
                    case ReinitMode.None:
                        behaviour.reinitEnter = false;
                        behaviour.reinitExit = false;
                        break;
                    case ReinitMode.OnExitClip:
                        behaviour.reinitEnter = false;
                        behaviour.reinitExit = true;
                        break;
                    case ReinitMode.OnEnterClip:
                        behaviour.reinitEnter = true;
                        behaviour.reinitExit = false;
                        break;
                    case ReinitMode.OnEnterOrExitClip:
                        behaviour.reinitEnter = true;
                        behaviour.reinitExit = true;
                        break;
                }
            }

            if (clipEvents == null)
                clipEvents = new List<ClipEvent>();
            if (singleEvents == null)
                singleEvents = new List<VisualEffectPlayableSerializedEvent>();

            behaviour.clipEventsCount = (uint)clipEvents.Count;

            var tempCollectedEvent = new List<VisualEffectPlayableSerializedEvent>();
            foreach (var clipEvent in clipEvents)
            {
                tempCollectedEvent.Add(clipEvent.enter);
                tempCollectedEvent.Add(clipEvent.exit);
            }
            foreach (var singleEvent in singleEvents)
            {
                tempCollectedEvent.Add(singleEvent);
            }
            behaviour.events = tempCollectedEvent.ToArray();

            if (!prewarm.enable || !behaviour.reinitEnter || prewarm.eventName == null || string.IsNullOrEmpty((string)prewarm.eventName))
            {
                behaviour.prewarmStepCount = 0u;
                behaviour.prewarmDeltaTime = 0.0f;
                behaviour.prewarmEvent = null;
            }
            else
            {
                behaviour.prewarmStepCount = prewarm.stepCount;
                behaviour.prewarmDeltaTime = prewarm.deltaTime;
                behaviour.prewarmEvent = prewarm.eventName;
            }

            return playable;
        }
    }
}
#endif
