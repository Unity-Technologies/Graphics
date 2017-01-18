using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System.Collections.Generic;
using System;


namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    abstract public class SkyRenderer
    {
        abstract public void Build();
        abstract public void Cleanup();
        abstract public void SetRenderTargets(BuiltinSkyParameters builtinParams);
        // renderForCubemap: When rendering into a cube map, no depth buffer is available so user has to make sure not to use depth testing or the depth texture.
        abstract public void RenderSky(BuiltinSkyParameters builtinParams, SkyParameters skyParameters, bool renderForCubemap);
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
