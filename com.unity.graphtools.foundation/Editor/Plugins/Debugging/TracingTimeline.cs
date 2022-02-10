using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Plugins.Debugging
{
    class TracingTimeline
    {
        const int k_FrameRectangleHeight = 8;
        const int k_MinTimeVisibleOnTheRight = 30;

        readonly GraphView m_GraphView;
        TracingControlStateComponent m_TracingControlState;
        TimeArea m_TimeArea;
        AnimEditorOverlay m_Overlay;
        TimelineState m_State;

        public TracingTimeline(GraphView graphView, TracingControlStateComponent tracingControlState)
        {
            m_GraphView = graphView;
            m_TracingControlState = tracingControlState;
            m_TimeArea = new TimeArea();
            m_Overlay = new AnimEditorOverlay() { PlayHeadColor = new Color(0.2117647F, 0.6039216F, 0.8F) };
            m_State = new TimelineState(m_TimeArea);
            m_Overlay.state = m_State;

            var debugger = ((Stencil)m_GraphView?.GraphViewState.GraphModel?.Stencil)?.Debugger;
            IGraphTrace trace = debugger?.GetGraphTrace(m_GraphView?.GraphViewState.GraphModel,
                tracingControlState.CurrentTracingTarget);
            if (trace?.AllFrames != null && trace.AllFrames.Count > 0)
            {
                int firstFrame = trace.AllFrames[0].Frame;
                int lastFrame = trace.AllFrames[trace.AllFrames.Count - 1].Frame;
                m_TimeArea.SetShownRange(
                    firstFrame - k_MinTimeVisibleOnTheRight,
                    lastFrame + k_MinTimeVisibleOnTheRight);
            }
        }

        public void OnGUI(Rect timeRect)
        {
            // sync timeline and tracing toolbar state both ways
            m_State.CurrentTime = TimelineState.FrameToTime(m_TracingControlState.CurrentTracingFrame);

            m_Overlay.HandleEvents();
            int timeChangedByTimeline = TimelineState.TimeToFrame(m_State.CurrentTime);

            using (var updater = m_TracingControlState.UpdateScope)
            {
                // force graph update
                if (timeChangedByTimeline != m_TracingControlState.CurrentTracingFrame)
                    updater.CurrentTracingStep = -1;
                updater.CurrentTracingFrame = timeChangedByTimeline;
            }

            GUI.BeginGroup(timeRect);

            var debugger = ((Stencil)m_GraphView?.GraphViewState.GraphModel?.Stencil)?.Debugger;
            IGraphTrace trace = debugger?.GetGraphTrace(m_GraphView?.GraphViewState.GraphModel, m_TracingControlState.CurrentTracingTarget);
            if (trace?.AllFrames != null && trace.AllFrames.Count > 0)
            {
                float frameDeltaToPixel = m_TimeArea.FrameDeltaToPixel();
                int firstFrame = trace.AllFrames[0].Frame;
                int lastFrame = trace.AllFrames[trace.AllFrames.Count - 1].Frame;
                float start = m_TimeArea.FrameToPixel(firstFrame);
                float width = frameDeltaToPixel * Mathf.Max(lastFrame - firstFrame, 1);

                // draw active range
                EditorGUI.DrawRect(new Rect(start,
                    timeRect.yMax - k_FrameRectangleHeight, width, k_FrameRectangleHeight), k_FrameHasDataColor);

                // draw per-node active ranges
                var framesPerNode = IndexFramesPerNode(ref s_FramesPerNodeCache, (Stencil)m_GraphView?.GraphViewState.GraphModel?.Stencil, trace, firstFrame, lastFrame, m_TracingControlState.CurrentTracingTarget, out bool invalidated);

                // while recording in unpaused playmode, adjust the timeline to show all data
                // same if the cached data changed (eg. Load a trace dump)
                if (EditorApplication.isPlaying && !EditorApplication.isPaused || invalidated)
                {
                    m_TimeArea.SetShownRange(
                        firstFrame - k_MinTimeVisibleOnTheRight,
                        lastFrame + k_MinTimeVisibleOnTheRight);
                }

                if (framesPerNode != null)
                {
                    INodeModel nodeModelSelected = m_GraphView.GetSelection()
                        .OfType<INodeModel>()
                        .FirstOrDefault();

                    if (nodeModelSelected != null && framesPerNode.TryGetValue(nodeModelSelected.Guid, out List<(int InclusiveFirstFrame, int ExclusiveLastFrame)> frames))
                    {
                        foreach (var frameInterval in frames)
                        {
                            float xStart = m_TimeArea.FrameToPixel(frameInterval.InclusiveFirstFrame);
                            float xEnd = m_TimeArea.FrameToPixel(frameInterval.ExclusiveLastFrame) - Mathf.Min(1, frameDeltaToPixel * 0.1f);
                            Rect rect = new Rect(
                                xStart,
                                timeRect.yMin,
                                xEnd - xStart,
                                timeRect.yMax);
                            EditorGUI.DrawRect(rect, k_FrameHasNodeColor);
                        }
                    }
                }
            }
            GUI.EndGroup();


            // time scales
            GUILayout.BeginArea(timeRect);
            m_TimeArea.Draw(timeRect);
            GUILayout.EndArea();

            // playing head
            m_Overlay.OnGUI(timeRect, timeRect);
        }

        internal struct FramesPerNodeCache
        {
            public Dictionary<SerializableGUID, List<(int, int)>> NodeToFrames;
            public int FirstFrame;
            public int LastFrame;
            public Stencil GraphModel;
            public int EntityId;
        }

        static FramesPerNodeCache s_FramesPerNodeCache;
        static ProfilerMarker s_IndexFramesPerNodeMarker = new ProfilerMarker("IndexFramesPerNode");
        static readonly Color32 k_FrameHasDataColor = new Color32(38, 80, 154, 200);
        static readonly Color32 k_FrameHasNodeColor = new Color32(255, 255, 255, 62);

        static readonly Dictionary<SerializableGUID, SortedSet<int>> s_FramesPerNodeRaw = new Dictionary<SerializableGUID, SortedSet<int>>();

        /// <summary>
        /// Transforms a graph trace (frame to active nodes mapping) to a "node to active frames" mapping, where the active frames are non overlapping intervals of the form (inclusive first frame, exclusive last frame)
        /// </summary>
        /// <param name="cachedIndex">The previous computation.</param>
        /// <param name="stencil"></param>
        /// <param name="trace"></param>
        /// <param name="firstFrame">Inclusive first frame to compute the mapping for.</param>
        /// <param name="lastFrame">Inclusive last frame to compute the mapping for.</param>
        /// <param name="entityId"></param>
        /// <param name="invalidated"></param>
        /// <returns></returns>
        internal static Dictionary<SerializableGUID, List<(int StartFrame, int EndFrame)>> IndexFramesPerNode(ref FramesPerNodeCache cachedIndex, Stencil stencil, IGraphTrace trace, int firstFrame, int lastFrame, int entityId, out bool invalidated)
        {
            invalidated = false;
            bool cachedSameEntityGraph = cachedIndex.GraphModel != null && cachedIndex.GraphModel == stencil &&
                cachedIndex.EntityId == entityId &&
                cachedIndex.NodeToFrames != null;

            //simple case: we already computed everything
            if (cachedSameEntityGraph &&
                cachedIndex.FirstFrame == firstFrame &&
                cachedIndex.LastFrame == lastFrame)
                return cachedIndex.NodeToFrames;

            invalidated = true;
            s_IndexFramesPerNodeMarker.Begin();

            s_FramesPerNodeRaw.Clear();

            Dictionary<SerializableGUID, List<(int InclusiveFirstFrame, int ExclusiveLastFrame)>> framesPerNode;
            if (cachedIndex.NodeToFrames == null)
            {
                cachedIndex.NodeToFrames = framesPerNode = new Dictionary<SerializableGUID, List<(int, int)>>();
                cachedIndex.FirstFrame = firstFrame;
            }
            else
            {
                framesPerNode = cachedIndex.NodeToFrames;

                Assert.IsTrue(firstFrame >= cachedIndex.FirstFrame, "Cannot go backward in the past");

                // remove too old intervals
                foreach (var keyValuePair in framesPerNode)
                {
                    var intervals = keyValuePair.Value;
                    for (int i = 0; i < intervals.Count; i++)
                    {
                        // only remove non first-frame-overlapping intervals, keep overlapping ones (eg. if first frame is 12
                        // and the interval is [10, 14[, keep it. if the interval is [10,11[, remove it)
                        if (intervals[i].ExclusiveLastFrame <= firstFrame)
                        {
                            intervals.RemoveAt(i);
                            i--;
                        }
                    }
                }

                cachedIndex.FirstFrame = firstFrame;
                // only compute new frames
                firstFrame = cachedIndex.LastFrame + 1;
            }

            // index individual frames where a node is active
            for (var index = 0; index < trace.AllFrames.Count; index++)
            {
                IFrameData frame = trace.AllFrames[index];
                if (frame.Frame < firstFrame) // skip
                    continue;
                if (frame.Frame > lastFrame) // abort
                    break;
                using (var tracingStepEnumerator = frame.GetDebuggingSteps(stencil).GetEnumerator())
                {
                    while (tracingStepEnumerator.MoveNext())
                    {
                        var node = tracingStepEnumerator.Current.NodeModel;
                        if (node == null)
                            continue;
                        if (!s_FramesPerNodeRaw.TryGetValue(node.Guid, out var frames))
                            s_FramesPerNodeRaw.Add(node.Guid, frames = new SortedSet<int>());
                        frames.Add(frame.Frame);
                    }
                }
            }

            // compute frame intervals where a node is active from the raw individual frames
            foreach (var pair in s_FramesPerNodeRaw)
            {
                if (pair.Value.Count == 0)
                    continue;

                if (!framesPerNode.TryGetValue(pair.Key, out var intervals))
                    framesPerNode.Add(pair.Key, intervals = new List<(int, int)>());

                using (var nodeFramesEnumerator = pair.Value.GetEnumerator())
                {
                    nodeFramesEnumerator.MoveNext();
                    int firstNodeFrame = nodeFramesEnumerator.Current;
                    // first frame is 3, initial interval is (3,4)
                    (int start, int end)curInterval = (firstNodeFrame, firstNodeFrame + 1);
                    // maybe the first computed frame needs to extend the last cached interval
                    bool patchLastInterval = intervals.Count > 0 &&
                        intervals[intervals.Count - 1].ExclusiveLastFrame == firstNodeFrame;
                    while (nodeFramesEnumerator.MoveNext())
                    {
                        var i = nodeFramesEnumerator.Current;
                        // interval was (3,5),  node is not active during frame 5, cur frame is 6
                        if (i != curInterval.end)
                        {
                            AddInterval(ref patchLastInterval, intervals, curInterval);

                            curInterval = (i, i + 1);
                        }
                        else // current frame is 4, extend cur interval to (3,5)
                            curInterval.end = i + 1;
                    }

                    AddInterval(ref patchLastInterval, intervals, curInterval);
                }
            }

            s_IndexFramesPerNodeMarker.End();

            cachedIndex.GraphModel = stencil;
            cachedIndex.LastFrame = lastFrame;
            cachedIndex.EntityId = entityId;

            return framesPerNode;
        }

        static void AddInterval(ref bool patchLastInterval,
            List<(int InclusiveFirstFrame, int ExclusiveLastFrame)> intervals, (int start, int end) curInterval)
        {
            if (patchLastInterval)
            {
                patchLastInterval = false;
                var lastInterval = intervals[intervals.Count - 1];
                lastInterval.ExclusiveLastFrame = curInterval.end;
                intervals[intervals.Count - 1] = lastInterval;
            }
            else
                intervals.Add(curInterval);
        }
    }
}
