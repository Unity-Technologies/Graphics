//#define USE_SHADER_AS_SUBASSET
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.VFX;
using UnityEngine.Profiling;


using UnityObject = UnityEngine.Object;

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
        public Action<VFXModel, VFXErrorOrigin> onClearAllErrors;
        public Action<VFXModel, VFXErrorOrigin, string, VFXErrorType, string> onRegisterError;

        public void ClearAllErrors(VFXModel model, VFXErrorOrigin errorOrigin)
        {
            if (onClearAllErrors != null)
                onClearAllErrors(model, errorOrigin);
        }

        public void RegisterError(VFXModel model, VFXErrorOrigin errorOrigin, string error, VFXErrorType type, string description)
        {
            if (onRegisterError != null)
                onRegisterError(model, errorOrigin, error, type, description);
        }
    }

    interface IVFXErrorReporter : IDisposable
    {
        void RegisterError(string error, VFXErrorType type, string description, VFXModel model);
    }

    class VFXInvalidateErrorReporter : IVFXErrorReporter
    {
        readonly VFXModel m_Model;
        readonly VFXErrorManager m_Manager;

        public VFXInvalidateErrorReporter(VFXErrorManager manager, VFXModel model)
        {
            m_Model = model;
            m_Manager = manager;
        }

        public void RegisterError(string error, VFXErrorType type, string description, VFXModel model = null)
        {
            model ??= m_Model;
            if (!m_Model.IsErrorIgnored(error))
                m_Manager.RegisterError(model, VFXErrorOrigin.Invalidate, error, type, description);
        }

        public void Dispose() { }
    }

    class VFXCompileErrorReporter : IVFXErrorReporter
    {
        private readonly VFXGraph m_Graph;
        readonly VFXErrorManager m_Manager;

        public VFXCompileErrorReporter(VFXGraph graph, VFXErrorManager manager)
        {
            m_Graph = graph;
            m_Manager = manager;
            Assert.IsNull(m_Graph.compileReporter);
            m_Graph.compileReporter = this;
        }

        public void Dispose()
        {
            Assert.IsNotNull(m_Graph.compileReporter);
            m_Graph.compileReporter = null;
        }

        public void RegisterError(string error, VFXErrorType type, string description, VFXModel model)
        {
            if (model != null && !model.IsErrorIgnored(error))
                m_Manager.RegisterError(model, VFXErrorOrigin.Compilation, error, type, description);
        }
    }
}
