using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.UI
{
    class VFXBoundsRecorder
    {
        readonly VisualEffect m_Effect;
        readonly Dictionary<string, Bounds> m_Bounds;
        readonly VFXView m_View;
        readonly VFXGraph m_Graph;

        bool m_IsRecording = false;

        public bool isRecording
        {
            get => m_IsRecording;
            set => m_IsRecording = value;
        }

        public IEnumerable<string> systemNames
        {
            get
            {
                foreach (var system in systems)
                {
                    var systemName = "";
                    try
                    {
                        systemName = m_Graph.systemNames.GetUniqueSystemName(system);
                    }
                    catch
                    {
                        break;
                    }
                    yield return systemName;
                }
            }
        }

        public enum ExclusionCause
        {
            kNone,
            kAutomatic,
            kManual,
            kGraphComputed,
            kError,
        }

        public static Dictionary<ExclusionCause, string> exclusionCauseString = new Dictionary<ExclusionCause, string>()
        {
            {ExclusionCause.kNone, ""},
            {ExclusionCause.kAutomatic, "(Automatic)"},
            {ExclusionCause.kManual, "(Manual)"},
            {ExclusionCause.kGraphComputed, "(Graph-Computed)"},
            {ExclusionCause.kError, "(Error)"},
        };

        public static Dictionary<ExclusionCause, string> exclusionCauseTooltip = new Dictionary<ExclusionCause, string>()
        {
            {ExclusionCause.kNone, ""},
            {ExclusionCause.kAutomatic, "its Bounds mode is not set to Recorded."},
            {ExclusionCause.kManual, "its Bounds mode is not set to Recorded."},
            {ExclusionCause.kGraphComputed, "its Bounds are set from operators."},
            {ExclusionCause.kError, "an error occured."},
        };

        IEnumerable<VFXDataParticle> systems
        {
            get
            {
                return m_View.GetAllContexts()
                    .Select(c => c.controller.model.GetData())
                    .OfType<VFXDataParticle>()
                    .Distinct()
                    .Where(x => x != null && x.CanBeCompiled());
            }
        }

        VFXDataParticle GetSystem(string systemName)
        {
            return systems.First(x => m_Graph.systemNames.GetUniqueSystemName(x) == systemName);
        }

        public BoundsSettingMode GetSystemBoundsSettingMode(string systemName)
        {
            return GetSystem(systemName).boundsMode;
        }

        public VFXBoundsRecorder(VisualEffect effect, VFXView view)
        {
            m_View = view;
            m_Graph = m_View.controller.graph;
            m_Effect = effect;
            EditorApplication.update += UpdateBounds;
            SceneView.duringSceneGui += RenderBounds;
            m_Bounds = new Dictionary<string, Bounds>();

            m_Graph.onInvalidateDelegate += OnParamSystemModified;
        }

        public void CleanUp()
        {
            EditorApplication.update -= UpdateBounds;
            SceneView.duringSceneGui -= RenderBounds;
            isRecording = false;
        }

        bool NeedsToBeRecorded(string systemName)
        {
            try
            {
                return NeedsToBeRecorded(GetSystem(systemName));
            }
            catch
            {
                return false;
            }
        }

        public bool NeedsToBeRecorded(string systemName, out ExclusionCause cause)
        {
            try
            {
                return NeedsToBeRecorded(GetSystem(systemName), out cause);
            }
            catch
            {
                cause = ExclusionCause.kError;
                return false;
            }
        }

        public bool NeedsAnyToBeRecorded() => systems.Any(NeedsToBeRecorded);

        bool NeedsToBeRecorded(VFXDataParticle system)
        {
            var initializeContext = system.owners.OfType<VFXBasicInitialize>().Single();
            var boundsSlot = initializeContext.inputSlots.FirstOrDefault(s => s.name == "bounds");
            return system.boundsMode == BoundsSettingMode.Recorded && !boundsSlot.AllChildrenWithLink().Any();
        }

        bool NeedsToBeRecorded(VFXDataParticle system, out ExclusionCause cause)
        {
            try
            {
                var initializeContext = system.owners.OfType<VFXBasicInitialize>().Single();
                var boundsSlot = initializeContext.inputSlots.First(s => s.name == "bounds");
                if (boundsSlot.HasLink(true))
                {
                    cause = ExclusionCause.kGraphComputed;
                    return false;
                }
            }
            catch
            {
                if (system.boundsMode == BoundsSettingMode.Automatic)
                {
                    cause = ExclusionCause.kAutomatic;
                    return false;
                }
                cause = ExclusionCause.kError;
                return false;
            }

            if (system.boundsMode == BoundsSettingMode.Manual)
            {
                cause = ExclusionCause.kManual;
                return false;
            }

            cause = ExclusionCause.kNone;
            return true;
        }

        //If a slot is modified, find what system/particleData it affects, and reset the bounds at next frame
        void OnParamSystemModified(VFXModel model, VFXModel.InvalidationCause cause)
        {
            if (isRecording)
            {
                if (cause == VFXModel.InvalidationCause.kParamChanged || cause == VFXModel.InvalidationCause.kExpressionValueInvalidated)
                {
                    if (model is VFXSlot slot && slot.name != "bounds" && slot.name != "boundsPadding")
                    {
                        if (slot.owner is IVFXDataGetter dataGetter && dataGetter.GetData() is VFXDataParticle system)
                        {
                            var systemName = m_Graph.systemNames.GetUniqueSystemName(system);
                            m_Bounds.Remove(systemName);
                        }
                    }
                }
            }
        }

        internal void UpdateBounds()
        {
            if (m_IsRecording && m_Effect)
            {
                foreach (var system in systems)
                {
                    var systemName = m_Graph.systemNames.GetUniqueSystemName(system);
                    if (NeedsToBeRecorded(system))
                    {
                        var currentBounds = m_Effect.GetComputedBounds(systemName);
                        if (currentBounds.size == Vector3.zero)
                            continue;
                        var padding = m_Effect.GetCurrentBoundsPadding(systemName);
                        currentBounds.extents -= padding;
                        if (m_Bounds.TryGetValue(systemName, out var previousBounds))
                        {
                            previousBounds.Encapsulate(currentBounds);
                            m_Bounds[systemName] = previousBounds;
                        }
                        else
                        {
                            m_Bounds[systemName] = currentBounds;
                        }
                    }
                }
            }
        }

        void RenderBounds(SceneView sv)
        {
            if (m_IsRecording && m_Effect.gameObject.activeSelf)
            {
                bool renderAllRecordedBounds = false;
                HashSet<string> selectedSystems = new HashSet<string>();
                foreach (var system in systems)
                {
                    var allSystemContexts = system.owners.ToList();

                    var selectedSystemContexts = m_View.GetAllContexts()
                        .Where(c => c.selected && c.controller.model is VFXBasicInitialize)
                        .Select(c => c.controller.model)
                        .Where(m => allSystemContexts.Contains(m));

                    if (selectedSystemContexts.Any())
                    {
                        string systemName = "";
                        try  //RenderBounds() is not executed in the same thread, so it can be executed before viewableSystems is up-to-date when a system is deleted
                        {
                            systemName = m_Graph.systemNames.GetUniqueSystemName(system);
                        }
                        catch
                        {
                            continue;
                        }

                        selectedSystems.Add(systemName);
                    }
                }

                if (!selectedSystems.Where(NeedsToBeRecorded).Any())
                    renderAllRecordedBounds = true;
                foreach (var system in systems)
                {
                    string systemName = "";
                    try  //RenderBounds() is not executed in the same thread, so it can be executed before viewableSystems is up-to-date when a system is deleted
                    {
                        systemName = m_Graph.systemNames.GetUniqueSystemName(system);
                    }
                    catch
                    {
                        continue;
                    }

                    if ((renderAllRecordedBounds || selectedSystems.Contains(systemName)) &&
                        m_Bounds.TryGetValue(systemName, out var currentBounds) && NeedsToBeRecorded(system))
                    {
                        var padding = m_Effect.GetCurrentBoundsPadding(systemName);
                        var paddedBounds = new Bounds(currentBounds.center, 2 * (currentBounds.extents + padding));
                        RenderBoundsSystem(paddedBounds);
                    }
                }
            }
        }

        private void RenderBoundsSystem(Bounds bounds)
        {
            Matrix4x4 oldMatrix = Handles.matrix;
            Handles.matrix = m_Effect.transform.localToWorldMatrix;

            var points = ExtractVerticesFromBounds(bounds);

            Color prevColor = Handles.color;
            Handles.color = Color.red;
            Handles.DrawLine(points[0], points[1]);
            Handles.DrawLine(points[2], points[3]);
            Handles.DrawLine(points[4], points[5]);
            Handles.DrawLine(points[6], points[7]);

            Handles.DrawLine(points[0], points[2]);
            Handles.DrawLine(points[0], points[4]);
            Handles.DrawLine(points[1], points[3]);
            Handles.DrawLine(points[1], points[5]);

            Handles.DrawLine(points[2], points[6]);
            Handles.DrawLine(points[3], points[7]);
            Handles.DrawLine(points[4], points[6]);
            Handles.DrawLine(points[5], points[7]);
            Handles.matrix = oldMatrix;

            Handles.color = prevColor;
        }

        private Vector3[] ExtractVerticesFromBounds(Bounds bounds)
        {
            Vector3[] points = new Vector3[8];

            Vector3 center = bounds.center;
            Vector3 size = bounds.size;

            points[0] = center + new Vector3(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);
            points[1] = center + new Vector3(size.x * 0.5f, -size.y * 0.5f, size.z * 0.5f);

            points[2] = center + new Vector3(-size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);
            points[3] = center + new Vector3(-size.x * 0.5f, -size.y * 0.5f, size.z * 0.5f);

            points[4] = center + new Vector3(size.x * 0.5f, size.y * 0.5f, -size.z * 0.5f);
            points[5] = center + new Vector3(size.x * 0.5f, -size.y * 0.5f, -size.z * 0.5f);

            points[6] = center + new Vector3(-size.x * 0.5f, size.y * 0.5f, -size.z * 0.5f);
            points[7] = center + new Vector3(-size.x * 0.5f, -size.y * 0.5f, -size.z * 0.5f);
            return points;
        }

        public void ModifyMode(string systemName, BoundsSettingMode mode)
        {
            var system = GetSystem(systemName);
            system.SetSettingValue("boundsMode", mode);
        }

        public void ToggleRecording()
        {
            m_IsRecording = !m_IsRecording;
            foreach (var system in systems.Where(NeedsToBeRecorded))
            {
                system.SetSettingValue("needsComputeBounds", m_IsRecording);
            }

            if (m_IsRecording)
            {
                m_Bounds.Clear();
            }
        }

        public void ApplyCurrentBounds()
        {
            foreach (var system in systems)
            {
                string systemName = m_Graph.systemNames.GetUniqueSystemName(system);

                if (m_Bounds.TryGetValue(systemName, out var currentBounds) && NeedsToBeRecorded(system))
                {
                    var initializeContext = system.owners.OfType<VFXBasicInitialize>().Single();
                    var boundsSlot = initializeContext.inputSlots.FirstOrDefault(s => s.name == "bounds");
                    var bounds = new Bounds(currentBounds.center, 2 * currentBounds.extents);
                    if (boundsSlot.space == VFXCoordinateSpace.Local)
                        boundsSlot.value = new AABox { center = bounds.center, size = bounds.size };
                    else
                    {
                        //Subject to change depending on the future behavior of AABox w.r.t. to Spaceable
                        var positionWorld = m_Effect.transform.TransformPoint(bounds.center);
                        boundsSlot.value = new AABox { center = positionWorld, size = bounds.size };
                    }
                }
            }
        }

        public VFXContextUI GetInitializeContextUI(string systemName)
        {
            VFXContextUI initializeContextUI;
            try
            {
                initializeContextUI = m_View.GetAllContexts().First(c =>
                    c.controller.model is VFXBasicInitialize &&
                    m_Graph.systemNames.GetUniqueSystemName(c.controller.model.GetData()) == systemName);
            }
            catch
            {
                throw new InvalidOperationException("The system does not have an Init context.");
            }

            return initializeContextUI;
        }
    }
}
