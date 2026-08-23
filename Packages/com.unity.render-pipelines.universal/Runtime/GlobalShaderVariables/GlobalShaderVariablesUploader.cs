using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.Rendering.Universal
{
    internal sealed class GlobalShaderVariablesUploader : IUniversalGlobalShaderVariablesUploader, IUniversal2DGlobalShaderVariablesUploader
    {
        // None of these vars depend on the target UV origin (y-flip), so a single default covers both backbuffer and offscreen rendering.
        // URP passes fill the default pre-Record, then we freeze the default and pass it as staging during the execute timeline between passes,
        // keeping modifications into account at staging level.
        private GlobalShaderVariables m_DefaultVars;

        // The var groups this uploader owns: pushes never touch the others, whoever owns them is responsible for them.
        private readonly GlobalShaderVariablesGroup m_OwnedGroups;

        private GlobalShaderVariables m_StagingVars;

        // Which staging fields changed since the last push. Setters mark unconditionally rather than comparing values.
        private GlobalShaderVariablesDirty m_StagingDirty;

        // The list of all vars setup by URP pre record, to avoid setGlobal them later when we reset to default.
        private GlobalShaderVariablesDirty m_DefaultDirty;

        // True between BeginFill and EndFill, i.e. while the caller writes the values of the camera being recorded.
        private bool m_IsFillingDefaults;

        internal GlobalShaderVariablesUploader(GlobalShaderVariablesGroup ownedGroups)
        {
            m_OwnedGroups = ownedGroups;
            m_StagingDirty.Clear();
            m_DefaultDirty.Clear();
        }

        /// <summary>
        /// Opens the fill window: the caller is about to write the values of this camera through the accessors.
        /// Paired with <see cref="EndFill"/>, one pair per camera render since the uploader outlives them.
        /// </summary>
        internal void BeginFill()
        {
            m_IsFillingDefaults = true;
        }

        /// <summary>
        /// Closes the fill window, freezing the staged state as the defaults of this camera. Call it once the
        /// record-time fill wrote everything the camera uses: recording completes before any pass executes, so the
        /// defaults are ready before the first push.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no fill window is open, so either called twice or without a matching <see cref="BeginFill"/>.
        /// Closing twice would promote execute-time overrides to defaults, making a later PushDefaultToGlobal restore
        /// values the fill never produced; closing without opening means the camera state was never filled.
        /// </exception>
        internal void EndFill()
        {
            if (!m_IsFillingDefaults)
                throw new InvalidOperationException("No global shader variables fill window is open. EndFill() must be called exactly once per BeginFill(), see GlobalShaderVariablesUploader.");

            m_IsFillingDefaults = false;

            // Only the owned groups: the others are never filled nor pushed, copying them would move bytes for nothing.
            if ((m_OwnedGroups & GlobalShaderVariablesGroup.Base) != 0) m_DefaultVars.varsBase = m_StagingVars.varsBase;
            if ((m_OwnedGroups & GlobalShaderVariablesGroup.Only3D) != 0) m_DefaultVars.vars3D = m_StagingVars.vars3D;
            if ((m_OwnedGroups & GlobalShaderVariablesGroup.Only2D) != 0) m_DefaultVars.vars2D = m_StagingVars.vars2D;

            m_DefaultDirty = m_StagingDirty;

            // Staging now matches the defaults, nothing is pending anymore.
            m_StagingDirty.Clear();
        }

        // Only pushes what changed since the last push, staging is assumed to still match engine state for the rest.
        public void PushGlobal(IBaseCommandBuffer cmd) => PushGlobal(cmd, GlobalShaderVariablesGroup.All);

        internal void PushGlobal(IBaseCommandBuffer cmd, GlobalShaderVariablesGroup groups)
        {
            groups &= m_OwnedGroups;

            GlobalShaderVariablesDirty toPush = m_StagingDirty;
            if ((groups & GlobalShaderVariablesGroup.Base) == 0) toPush.varsBase = GlobalShaderVariablesBaseDirty.None;
            if ((groups & GlobalShaderVariablesGroup.Only3D) == 0) toPush.vars3D = GlobalShaderVariablesOnly3DDirty.None;
            if ((groups & GlobalShaderVariablesGroup.Only2D) == 0) toPush.vars2D = GlobalShaderVariablesOnly2DDirty.None;

            if (!toPush.isDirty)
                return;

            m_StagingVars.SetGlobals(cmd, toPush, groups);

            m_StagingDirty.varsBase &= ~toPush.varsBase;
            m_StagingDirty.vars3D &= ~toPush.vars3D;
            m_StagingDirty.vars2D &= ~toPush.vars2D;
        }

        public void ResetToDefault() => ResetToDefault(GlobalShaderVariablesGroup.All);

        internal void ResetToDefault(GlobalShaderVariablesGroup groups)
        {
            groups &= m_OwnedGroups;

            // Nothing to restore when the fill never wrote the group, and nothing to copy either.
            if ((groups & GlobalShaderVariablesGroup.Base) != 0 && m_DefaultDirty.varsBase != GlobalShaderVariablesBaseDirty.None)
            {
                m_StagingVars.varsBase = m_DefaultVars.varsBase;
                m_StagingDirty.varsBase |= m_DefaultDirty.varsBase;
            }

            if ((groups & GlobalShaderVariablesGroup.Only3D) != 0 && m_DefaultDirty.vars3D != GlobalShaderVariablesOnly3DDirty.None)
            {
                m_StagingVars.vars3D = m_DefaultVars.vars3D;
                m_StagingDirty.vars3D |= m_DefaultDirty.vars3D;
            }

            if ((groups & GlobalShaderVariablesGroup.Only2D) != 0 && m_DefaultDirty.vars2D != GlobalShaderVariablesOnly2DDirty.None)
            {
                m_StagingVars.vars2D = m_DefaultVars.vars2D;
                m_StagingDirty.vars2D |= m_DefaultDirty.vars2D;
            }
        }

        public void PushDefaultToGlobal(IBaseCommandBuffer cmd) => PushDefaultToGlobal(cmd, GlobalShaderVariablesGroup.All);

        internal void PushDefaultToGlobal(IBaseCommandBuffer cmd, GlobalShaderVariablesGroup groups)
        {
            ResetToDefault(groups);
            PushGlobal(cmd, groups);
        }

        /// <summary>
        /// [t/20, t, t*2, t*3]
        /// </summary>
        public Vector4 _Time
        {
            get => m_StagingVars.varsBase._Time;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.varsBase._Time = value;
                m_StagingDirty.varsBase |= GlobalShaderVariablesBaseDirty._Time;
            }
        }

        /// <summary>
        /// [sin(t/8), sin(t/4), sin(t/2), sin(t)]
        /// </summary>
        public Vector4 _SinTime
        {
            get => m_StagingVars.varsBase._SinTime;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.varsBase._SinTime = value;
                m_StagingDirty.varsBase |= GlobalShaderVariablesBaseDirty._SinTime;
            }
        }

        /// <summary>
        /// [cos(t/8), cos(t/4), cos(t/2), cos(t)]
        /// </summary>
        public Vector4 _CosTime
        {
            get => m_StagingVars.varsBase._CosTime;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.varsBase._CosTime = value;
                m_StagingDirty.varsBase |= GlobalShaderVariablesBaseDirty._CosTime;
            }
        }

        /// <summary>
        /// [dt, 1/dt, smoothdt, 1/smoothdt]
        /// </summary>
        public Vector4 unity_DeltaTime
        {
            get => m_StagingVars.varsBase.unity_DeltaTime;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.varsBase.unity_DeltaTime = value;
                m_StagingDirty.varsBase |= GlobalShaderVariablesBaseDirty.unity_DeltaTime;
            }
        }

        /// <summary>
        /// [t, sin(t), cos(t)]
        /// </summary>
        public Vector4 _TimeParameters
        {
            get => m_StagingVars.varsBase._TimeParameters;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.varsBase._TimeParameters = value;
                m_StagingDirty.varsBase |= GlobalShaderVariablesBaseDirty._TimeParameters;
            }
        }

        /// <summary>
        /// [t, sin(t), cos(t)]
        /// </summary>
        public Vector4 _LastTimeParameters
        {
            get => m_StagingVars.varsBase._LastTimeParameters;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.varsBase._LastTimeParameters = value;
                m_StagingDirty.varsBase |= GlobalShaderVariablesBaseDirty._LastTimeParameters;
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
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.varsBase._ScreenParams = value;
                m_StagingDirty.varsBase |= GlobalShaderVariablesBaseDirty._ScreenParams;
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
        /// z = x/far
        /// w = 1/far
        /// </summary>
        public Vector4 _ZBufferParams
        {
            get => m_StagingVars.varsBase._ZBufferParams;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.varsBase._ZBufferParams = value;
                m_StagingDirty.varsBase |= GlobalShaderVariablesBaseDirty._ZBufferParams;
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
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.varsBase.unity_OrthoParams = value;
                m_StagingDirty.varsBase |= GlobalShaderVariablesBaseDirty.unity_OrthoParams;
            }
        }

        /// <summary>
        /// [w / RTHandle.maxWidth, h / RTHandle.maxHeight] : xy = currFrame, zw = prevFrame
        /// </summary>
        public Vector4 _RTHandleScale
        {
            get => m_StagingVars.varsBase._RTHandleScale;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.varsBase._RTHandleScale = value;
                m_StagingDirty.varsBase |= GlobalShaderVariablesBaseDirty._RTHandleScale;
            }
        }

        /// <summary>
        /// Like _ScreenParams but scaled by the current render scale / dynamic resolution: x = width, y = height, z = 1 + 1/width, w = 1 + 1/height.
        /// </summary>
        public Vector4 _ScaledScreenParams
        {
            get => m_StagingVars.varsBase._ScaledScreenParams;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.varsBase._ScaledScreenParams = value;
                m_StagingDirty.varsBase |= GlobalShaderVariablesBaseDirty._ScaledScreenParams;
            }
        }

        /// <summary>
        /// [w, h, 1/w, 1/h]
        /// </summary>
        public Vector4 _ScreenSize
        {
            get => m_StagingVars.varsBase._ScreenSize;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.varsBase._ScreenSize = value;
                m_StagingDirty.varsBase |= GlobalShaderVariablesBaseDirty._ScreenSize;
            }
        }

        /// <summary>
        /// Optional override of _ScreenSize [w, h, 1/w, 1/h] for passes that need a size different from the bound render target.
        /// </summary>
        public Vector4 _ScreenSizeOverride
        {
            get => m_StagingVars.varsBase._ScreenSizeOverride;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.varsBase._ScreenSizeOverride = value;
                m_StagingDirty.varsBase |= GlobalShaderVariablesBaseDirty._ScreenSizeOverride;
            }
        }

        /// <summary>
        /// Scale and bias applied to screen coordinates (xy = scale, zw = bias), e.g. to remap into a sub-viewport under dynamic resolution.
        /// </summary>
        public Vector4 _ScreenCoordScaleBias
        {
            get => m_StagingVars.varsBase._ScreenCoordScaleBias;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.varsBase._ScreenCoordScaleBias = value;
                m_StagingDirty.varsBase |= GlobalShaderVariablesBaseDirty._ScreenCoordScaleBias;
            }
        }

        /// <summary>
        /// x = Mip Bias
        /// y = 2.0 ^ [Mip Bias]
        /// </summary>
        public Vector2 _GlobalMipBias
        {
            get => m_StagingVars.varsBase._GlobalMipBias;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.varsBase._GlobalMipBias = value;
                m_StagingDirty.varsBase |= GlobalShaderVariablesBaseDirty._GlobalMipBias;
            }
        }

        // URP 3D-only shader variables (GlobalShaderVariablesOnly3D)
        public Vector4 _URPDummy3D
        {
            get => m_StagingVars.vars3D._URPDummy3D;
            set
            {
                m_StagingVars.vars3D._URPDummy3D = value;
                m_StagingDirty.vars3D |= GlobalShaderVariablesOnly3DDirty._URPDummy3D;
            }
        }

        // URP 2D-only shader variables (GlobalShaderVariablesOnly2D)
        public Vector4 _URPDummy2D
        {
            get => m_StagingVars.vars2D._URPDummy2D;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars2D._URPDummy2D = value;
                m_StagingDirty.vars2D |= GlobalShaderVariablesOnly2DDirty._URPDummy2D;
            }
        }
    }
}
