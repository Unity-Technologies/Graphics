//#define USE_SHADER_AS_SUBASSET
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.VFX;
using UnityEngine;
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

    class VFXInvalidateErrorReporter : IDisposable
    {
        readonly VFXModel m_Model;
        readonly VFXErrorManager m_Manager;

        public VFXInvalidateErrorReporter(VFXErrorManager manager, VFXModel model)
        {
            m_Model = model;
            m_Manager = manager;
        }

        public void Dispose()
        {
        }

        public void RegisterError(string error, VFXErrorType type, string description)
        {
            if (!m_Model.IsErrorIgnored(error))
                m_Manager.RegisterError(m_Model, VFXErrorOrigin.Invalidate, error, type, description);
        }
    }

    class VFXCompileErrorReporter : IDisposable
    {
        readonly VFXErrorManager m_Manager;

        public VFXCompileErrorReporter(VFXErrorManager manager)
        {
            m_Manager = manager;
        }

        public void Dispose()
        {
        }

        public void RegisterError(VFXModel model, string error, VFXErrorType type, string description)
        {
            if (model != null && !model.IsErrorIgnored(error))
                m_Manager.RegisterError(model, VFXErrorOrigin.Compilation, error, type, description);
        }
    }
}
