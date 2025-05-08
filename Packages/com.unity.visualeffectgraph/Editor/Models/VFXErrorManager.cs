using System;
using System.Collections.Generic;
using Unity.Profiling;

namespace UnityEditor.VFX
{
    enum VFXErrorType
    {
        Warning,
        PerfWarning,
        Error
    }

    enum VFXErrorOrigin
    {
        Compilation,
        Invalidate
    }

    class VFXErrorManager
    {
        public IVFXErrorReporter errorReporter { get; private set; }

        public IVFXErrorReporter compileReporter { get; private set; }

        public void RefreshInvalidateReport(VFXModel model)
        {
            if (errorReporter == null)
            {
                errorReporter = new VFXErrorReporter(VFXErrorOrigin.Invalidate);
            }

            errorReporter.InvalidateModelErrors(model);
        }

        public void RefreshCompilationReport()
        {
            if (compileReporter == null)
            {
                compileReporter = new VFXErrorReporter(VFXErrorOrigin.Compilation);
            }

            compileReporter.Clear();
        }

        public void GenerateErrors()
        {
            errorReporter?.GenerateErrors();
            compileReporter?.GenerateErrors();
        }
    }

    interface IVFXErrorReporter
    {
        VFXErrorOrigin origin { get; }
        IEnumerable<VFXModel> dirtyModels { get; }
        void Clear();
        void ClearDirtyModels();
        IEnumerable<ReportError> GetDirtyModelErrors(VFXModel model);
        void RegisterError(string error, VFXErrorType type, string description, VFXModel model);
        void InvalidateModelErrors(VFXModel model);
        void GenerateErrors();
    }

    internal class ReportError
    {
        public VFXModel model { get; }
        public VFXErrorType type { get; }
        public string error { get; }
        public string description { get; }

        public ReportError(VFXModel model, VFXErrorType type, string error, string description)
        {
            this.model = model;
            this.type = type;
            this.error = error;
            this.description = description;
        }
    }

    class VFXErrorReporter : IVFXErrorReporter
    {
        private readonly ProfilerMarker generateErrorsMarker = new("VFXErrorReporter.GenerateErrors");
        private readonly ProfilerMarker invalidateErrorsMarker = new("VFXErrorReporter.InvalidateModelErrors");
        private readonly Dictionary<VFXModel, List<ReportError>> m_Errors = new();
        private HashSet<VFXModel> m_ScheduledModels = new();
        private HashSet<VFXModel> m_DirtyModels = new();

        private bool m_IsGeneratingErrors;

        public VFXErrorReporter(VFXErrorOrigin origin)
        {
            this.origin = origin;
        }

        public VFXErrorOrigin origin { get; }
        public IEnumerable<VFXModel> dirtyModels => m_DirtyModels;
        public void ClearDirtyModels() => m_DirtyModels.Clear();

        public void Clear()
        {
            // When clearing errors, we mark the models as dirty so that badges can be removed in the view update
            foreach (var error in m_Errors)
            {
                m_DirtyModels.Add(error.Key);
            }
            m_Errors.Clear();
        }

        public IEnumerable<ReportError> GetDirtyModelErrors(VFXModel model)
        {
            if (m_Errors.TryGetValue(model, out var errors))
            {
                return errors.AsReadOnly();
            }

            return Array.Empty<ReportError>();
        }

        public void RegisterError(string error, VFXErrorType type, string description, VFXModel model)
        {
            if (!model.IsErrorIgnored(error))
            {
                var reportError = new ReportError(model, type, error, description);
                if (m_Errors.TryGetValue(model, out var errors))
                {
                    errors.Add(reportError);
                }
                else
                {
                    m_Errors[model] = new List<ReportError> { reportError };
                }
            }
        }

        public void InvalidateModelErrors(VFXModel model)
        {
            if (m_IsGeneratingErrors)
                return;
            using var marker = invalidateErrorsMarker.Auto();
            m_ScheduledModels.Add(model);
            m_Errors.Remove(model);
        }

        public void GenerateErrors()
        {
            if (m_ScheduledModels.Count > 0)
            {
                using var marker = generateErrorsMarker.Auto();
                try
                {
                    m_IsGeneratingErrors = true;
                    foreach (var model in m_ScheduledModels)
                    {
                        model.GenerateErrors(this);
                    }

                }
                finally
                {
                    // swap dirty and scheduled models
                    var tmp = m_DirtyModels;
                    m_DirtyModels = m_ScheduledModels;
                    m_ScheduledModels = tmp;
                    m_ScheduledModels.Clear();
                    m_IsGeneratingErrors = false;
                }
            }
        }
    }
}
