using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using System;


namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    abstract public class SkyRenderer
    {
        abstract public void Build();
        abstract public void Cleanup();
        abstract public void RenderSky(BuiltinSkyParameters builtinParams, SkyParameters skyParameters);
        abstract public bool IsSkyValid(SkyParameters skyParameters);

        virtual public bool IsParameterValid(SkyParameters skyParameters) { return false; }
        virtual public Type GetSkyParameterType() { return typeof(SkyParameters); }
    }

    abstract public class SkyRenderer<ParameterType> : SkyRenderer
        where ParameterType : SkyParameters
    {
        override public bool IsParameterValid(SkyParameters skyParameters)
        {
            return GetParameters(skyParameters) != null;
        }

        override public Type GetSkyParameterType()
        {
            return typeof(ParameterType);
        }

        protected ParameterType GetParameters(SkyParameters parameters)
        {
            return parameters as ParameterType;
        }
    }
}
