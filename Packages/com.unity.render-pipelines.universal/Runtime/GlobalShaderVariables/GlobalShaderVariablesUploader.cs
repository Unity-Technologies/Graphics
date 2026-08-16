namespace UnityEngine.Rendering.Universal
{
    internal sealed class GlobalShaderVariablesUploader : IUniversalGlobalShaderVariablesUploader, IUniversal2DGlobalShaderVariablesUploader
    {
        // None of these vars depend on the target UV origin (y-flip), so a single default covers both backbuffer and offscreen rendering.
        internal GlobalShaderVariables m_DefaultVars;

        private readonly bool m_is3D;

        private GlobalShaderVariables m_StagingVars;

        // Which staging fields changed since the last push. Setters mark unconditionally rather than comparing values.
        private GlobalShaderVariablesDirty m_Dirty;

        internal GlobalShaderVariablesUploader(bool is3D)
        {
            m_is3D = is3D;
            m_Dirty.Clear();
        }

        internal void Reset()
        {
            m_Dirty.Clear();
        }

        // Only pushes what changed since the last push, staging is assumed to still match engine state for the rest.
        public void PushGlobal(IBaseCommandBuffer cmd)
        {
            if (!m_Dirty.isDirty)
                return;

            m_StagingVars.SetGlobals(cmd, m_Dirty, m_is3D);
            m_Dirty.Clear();
        }

        public void ResetToDefault()
        {
            m_StagingVars = m_DefaultVars;

            // Everything is marked dirty on purpose: we are not the only writer of these globals (cmd.SetupCameraProperties,
            // other cameras and renderers sharing the frame), so staging cannot be assumed to match engine state here.
            // The next push re-establishes all of them.
            m_Dirty = GlobalShaderVariablesDirty.all;
        }

        public void PushDefaultToGlobal(IBaseCommandBuffer cmd)
        {
            // We need to reset staging so it stays up to date with GPU data
            ResetToDefault();
            PushGlobal(cmd);
        }

        /// <summary>
        /// [t/20, t, t*2, t*3]
        /// </summary>
        public Vector4 _Time
        {
            get => m_StagingVars.varsBase._Time;
            set
            {
                m_StagingVars.varsBase._Time = value;
                m_Dirty.varsBase |= GlobalShaderVariablesBaseDirty._Time;
            }
        }

        /// <summary>
        /// [sin(t/8), sin(t/4), sin(t/2), sin(t)]
        /// </summary>
        public Vector4 _SinTime
        {
            get => m_StagingVars.varsBase._SinTime;
            set
            {
                m_StagingVars.varsBase._SinTime = value;
                m_Dirty.varsBase |= GlobalShaderVariablesBaseDirty._SinTime;
            }
        }

        /// <summary>
        /// [cos(t/8), cos(t/4), cos(t/2), cos(t)]
        /// </summary>
        public Vector4 _CosTime
        {
            get => m_StagingVars.varsBase._CosTime;
            set
            {
                m_StagingVars.varsBase._CosTime = value;
                m_Dirty.varsBase |= GlobalShaderVariablesBaseDirty._CosTime;
            }
        }

        /// <summary>
        /// [dt, 1/dt, smoothdt, 1/smoothdt]
        /// </summary>
        public Vector4 unity_DeltaTime
        {
            get => m_StagingVars.varsBase.unity_DeltaTime;
            set
            {
                m_StagingVars.varsBase.unity_DeltaTime = value;
                m_Dirty.varsBase |= GlobalShaderVariablesBaseDirty.unity_DeltaTime;
            }
        }

        /// <summary>
        /// [t, sin(t), cos(t)]
        /// </summary>
        public Vector4 _TimeParameters
        {
            get => m_StagingVars.varsBase._TimeParameters;
            set
            {
                m_StagingVars.varsBase._TimeParameters = value;
                m_Dirty.varsBase |= GlobalShaderVariablesBaseDirty._TimeParameters;
            }
        }

        /// <summary>
        /// [t, sin(t), cos(t)]
        /// </summary> 
        public Vector4 _LastTimeParameters
        {
            get => m_StagingVars.varsBase._LastTimeParameters;
            set
            {
                m_StagingVars.varsBase._LastTimeParameters = value;
                m_Dirty.varsBase |= GlobalShaderVariablesBaseDirty._LastTimeParameters;
            }
        }

        /// <summary>
        /// x = width
        /// y = height
        /// z = 1 + 1.0/width
        /// w = 1 + 1.0/height
        /// </summary>
        public Vector4 _ScreenParams
        {
            get => m_StagingVars.varsBase._ScreenParams;
            set
            {
                m_StagingVars.varsBase._ScreenParams = value;
                m_Dirty.varsBase |= GlobalShaderVariablesBaseDirty._ScreenParams;
            }
        }

        /// <summary>
        /// Values used to linearize the Z buffer (http://www.humus.name/temp/Linearize%20depth.txt)
        /// x = 1-far/near
        /// y = far/near
        /// z = x/far
        /// w = y/far
        /// or in case of a reversed depth buffer (UNITY_REVERSED_Z is 1)
        /// x = -1+far/near
        /// y = 1 
        /// </summary>
        public Vector4 _ZBufferParams
        {
            get => m_StagingVars.varsBase._ZBufferParams;
            set
            {
                m_StagingVars.varsBase._ZBufferParams = value;
                m_Dirty.varsBase |= GlobalShaderVariablesBaseDirty._ZBufferParams;
            }
        }

        /// <summary>
        /// x = orthographic camera's width
        /// y = orthographic camera's height
        /// z = unused
        /// w = 1.0 if camera is ortho, 0.0 if perspective
        /// </summary>
        public Vector4 unity_OrthoParams
        {
            get => m_StagingVars.varsBase.unity_OrthoParams;
            set
            {
                m_StagingVars.varsBase.unity_OrthoParams = value;
                m_Dirty.varsBase |= GlobalShaderVariablesBaseDirty.unity_OrthoParams;
            }
        }

        /// <summary>
        // [w / RTHandle.maxWidth, h / RTHandle.maxHeight] : xy = currFrame, zw = prevFrame
        /// </summary>
        public Vector4 _RTHandleScale
        {
            get => m_StagingVars.varsBase._RTHandleScale;
            set
            {
                m_StagingVars.varsBase._RTHandleScale = value;
                m_Dirty.varsBase |= GlobalShaderVariablesBaseDirty._RTHandleScale;
            }
        }

        /// <summary>
        /// Like _ScreenParams but scaled by the current render scale / dynamic resolution: x = width, y = height, z = 1 + 1/width, w = 1 + 1/height.
        /// </summary>
        public Vector4 _ScaledScreenParams
        {
            get => m_StagingVars.varsBase._ScaledScreenParams;
            set
            {
                m_StagingVars.varsBase._ScaledScreenParams = value;
                m_Dirty.varsBase |= GlobalShaderVariablesBaseDirty._ScaledScreenParams;
            }
        }

        /// <summary>
        /// [w, h, 1/w, 1/h]
        /// </summary>
        public Vector4 _ScreenSize
        {
            get => m_StagingVars.varsBase._ScreenSize;
            set
            {
                m_StagingVars.varsBase._ScreenSize = value;
                m_Dirty.varsBase |= GlobalShaderVariablesBaseDirty._ScreenSize;
            }
        }

        /// <summary>
        /// Optional override of _ScreenSize [w, h, 1/w, 1/h] for passes that need a size different from the bound render target.
        /// </summary>
        public Vector4 _ScreenSizeOverride
        {
            get => m_StagingVars.varsBase._ScreenSizeOverride;
            set
            {
                m_StagingVars.varsBase._ScreenSizeOverride = value;
                m_Dirty.varsBase |= GlobalShaderVariablesBaseDirty._ScreenSizeOverride;
            }
        }

        /// <summary>
        /// Scale and bias applied to screen coordinates (xy = scale, zw = bias), e.g. to remap into a sub-viewport under dynamic resolution.
        /// </summary>
        public Vector4 _ScreenCoordScaleBias
        {
            get => m_StagingVars.varsBase._ScreenCoordScaleBias;
            set
            {
                m_StagingVars.varsBase._ScreenCoordScaleBias = value;
                m_Dirty.varsBase |= GlobalShaderVariablesBaseDirty._ScreenCoordScaleBias;
            }
        }

        /// <summary>
        /// x = Mip Bias
        /// y = 2.0 ^ [Mip Bias]
        /// </summary>
        public Vector2 _GlobalMipBias
        {
            get => m_StagingVars.varsBase._GlobalMipBias;
            set
            {
                m_StagingVars.varsBase._GlobalMipBias = value;
                m_Dirty.varsBase |= GlobalShaderVariablesBaseDirty._GlobalMipBias;
            }
        }

        // URP 3D-only shader variables (GlobalShaderVariablesOnly3D)
        public Vector4 _URPDummy3D
        {
            get => m_StagingVars.vars3D._URPDummy3D;
            set
            {
                m_StagingVars.vars3D._URPDummy3D = value;
                m_Dirty.vars3D |= GlobalShaderVariablesOnly3DDirty._URPDummy3D;
            }
        }

        // URP 2D-only shader variables (GlobalShaderVariablesOnly2D)
        public Vector4 _URPDummy2D
        {
            get => m_StagingVars.vars2D._URPDummy2D;
            set
            {
                m_StagingVars.vars2D._URPDummy2D = value;
                m_Dirty.vars2D |= GlobalShaderVariablesOnly2DDirty._URPDummy2D;
            }
        }
    }
}