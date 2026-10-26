using System;
using System.Runtime.CompilerServices;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    internal sealed class GlobalShaderVariablesUploader : IUniversalGlobalShaderVariablesUploader, IUniversal2DGlobalShaderVariablesUploader
    {
        // None of these vars depend on the target UV origin (y-flip), so a single default covers both backbuffer and offscreen rendering.
        // URP passes fill the default pre-Record, then we freeze the default and pass it as staging during the execute timeline between passes,
        // keeping modifications into account at staging level.
        private GlobalShaderVariables m_DefaultVars;
        // Staging vars, we keep them up to date across the Render Graph execute timeline, between passes, as long as everyone uses the uploader.
        // They can be equal to default vars or completely differ from them.
        private GlobalShaderVariables m_StagingVars;

        // Which staging fields changed since the last push. Setters mark unconditionally rather than comparing values.
        private GlobalShaderVariablesDirty m_StagingDirty;
        // The list of all vars setup by URP pre record, to avoid setGlobal them later when we reset to default.
        private GlobalShaderVariablesDirty m_DefaultDirty;

        // The var groups this uploader owns: pushes never touch the others, whoever owns them is responsible for them.
        private readonly GlobalShaderVariablesGroup m_OwnedGroups;

        // True between BeginFill and EndFill, i.e. while the caller writes the values of the camera being recorded.
        private bool m_IsFillingDefaults;

        /// Persistent Constant Buffer Mode only ///

        // Whether the defaults buffer already holds the defaults frozen by the last EndFill.
        // Upload is recorded once per camera render, on the first push that actually needs the defaults bound.
        private bool m_IsDefaultBufferUploaded;

        // Whether the defaults buffer is the last one this uploader bound to the slot, which makes a defaults push a
        // no-op. Cleared when the override buffer takes the slot over, and once per camera render in EndFill.
        private bool m_IsDefaultBufferBound;

        // Groups whose staging drifted away from the frame defaults. Empty means the default buffer already holds
        // exactly what an upload would write.
        private GlobalShaderVariablesGroup m_DivergedGroups;

        // Binding slot of the persistent constant buffer, its name matches the CBUFFER_START() one in GlobalShaderVariables.hlsl.
        // Deliberately kept private: we must use the uploader to update this buffer.
        private static readonly int k_GlobalShaderVariablesID = Shader.PropertyToID("GlobalShaderVariables");

        // The two GPU copies of the CB, only allocated once the uploader actually runs in persistent CB mode. 
        // If staging == default, we use the default constant buffer.
        // If staging diverges, we use the override constant buffer.
        // Keeping the defaults in their own buffer is what makes a defaults push cheap: they are uploaded once per
        // camera render and every later restore is at most a rebind, whatever the overrides wrote to the other buffer
        // since, and nothing at all when no override took the slot in between.
        private ConstantBuffer<GlobalShaderVariables> m_DefaultConstantBuffer;
        private ConstantBuffer<GlobalShaderVariables> m_OverrideConstantBuffer;

        ////////////////////////////////////////////

        /// <summary>
        /// Determines whether to push a persistent constant buffer or to fill dynamic constant buffers with loose uniforms APIs (cmd.SetGlobalFloat, ...)
        /// </summary>
        /// <remark>
        /// Refreshed by the renderer every frame: the setting behind it can be toggled at any time in the Editor.
        /// Switching mode is safe mid-flight because every frame starts with a PushDefaultToGlobal, which re-establishes
        /// all the variables in the mode currently selected.
        /// </remark>
        internal bool useConstantBuffer { get; set; }

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
        /// Releases the GPU buffers backing the persistent constant buffer mode. Renderers must call it when they are
        /// disposed: the uploader owns these buffers, ConstantBuffer.ReleaseAll() does not know about them.
        /// </summary>
        internal void Release()
        {
            m_DefaultConstantBuffer?.Release();
            m_DefaultConstantBuffer = null;

            m_OverrideConstantBuffer?.Release();
            m_OverrideConstantBuffer = null;

            m_IsFillingDefaults = false;
            m_IsDefaultBufferUploaded = false;
            m_IsDefaultBufferBound = false;
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
            m_IsDefaultBufferUploaded = false;
            m_IsDefaultBufferBound = false;

            // Only the owned groups: the others are never filled nor pushed, copying them would move bytes for nothing.
            if ((m_OwnedGroups & GlobalShaderVariablesGroup.Base) != 0) m_DefaultVars.varsBase = m_StagingVars.varsBase;
            if ((m_OwnedGroups & GlobalShaderVariablesGroup.Only3D) != 0) m_DefaultVars.vars3D = m_StagingVars.vars3D;
            // URP 2D-only globals, empty so disabled.
            // if ((m_OwnedGroups & GlobalShaderVariablesGroup.Only2D) != 0) m_DefaultVars.vars2D = m_StagingVars.vars2D;

            m_DefaultDirty = m_StagingDirty;

            // Staging now matches the defaults, nothing is pending anymore.
            m_StagingDirty.Clear();
            m_DivergedGroups = GlobalShaderVariablesGroup.None;
        }

        public void PushDefaultToGlobal(IBaseCommandBuffer cmd) => PushDefaultToGlobal(cmd, GlobalShaderVariablesGroup.All);

        internal void PushDefaultToGlobal(IBaseCommandBuffer cmd, GlobalShaderVariablesGroup groups)
        {
            ResetToDefault(groups);
            PushGlobal(cmd, groups);
        }

        // Only pushes what changed since the last push, staging is assumed to still match engine state for the rest.
        public void PushGlobal(IBaseCommandBuffer cmd) => PushGlobal(cmd, GlobalShaderVariablesGroup.All);

        internal void PushGlobal(IBaseCommandBuffer cmd, GlobalShaderVariablesGroup groups)
        {
            groups &= m_OwnedGroups;

            GlobalShaderVariablesDirty toPush = m_StagingDirty;
            if ((groups & GlobalShaderVariablesGroup.Base) == 0) toPush.varsBase = GlobalShaderVariablesBaseDirty.None;
            if ((groups & GlobalShaderVariablesGroup.Only3D) == 0) toPush.vars3D = GlobalShaderVariablesOnly3DDirty.None;
            // URP 2D-only globals, empty so disabled.
            // if ((groups & GlobalShaderVariablesGroup.Only2D) == 0) toPush.vars2D = GlobalShaderVariablesOnly2DDirty.None;

            if (!toPush.isDirty)
                return;
            
            if (useConstantBuffer)
            {
                PushConstantBuffer((BaseCommandBuffer)cmd);
            }
            else
            {
                m_StagingVars.SetGlobals(cmd, toPush, groups);
            }

            m_StagingDirty.varsBase &= ~toPush.varsBase;
            m_StagingDirty.vars3D &= ~toPush.vars3D;
            // URP 2D-only globals, empty so disabled.
            // m_StagingDirty.vars2D &= ~toPush.vars2D;
        }

        /// <summary>
        /// Binds the GPU copy of the staged state, uploading it first unless the defaults buffer already holds it, and
        /// records nothing at all when that buffer is both up to date and still bound.
        /// </summary>
        /// <remark>
        /// A constant buffer is uploaded as a whole, so this ignores the group filtering the loose uniform path does:
        /// what is bound is always the full staged struct.
        /// </remark>
        private void PushConstantBuffer(BaseCommandBuffer cmd)
        {
            // No group drifted away from the defaults, so staging still is the defaults, byte for byte.
            if (m_DivergedGroups == GlobalShaderVariablesGroup.None)
            {
                m_DefaultConstantBuffer ??= new ConstantBuffer<GlobalShaderVariables>(ComputeBufferMode.Dynamic);

                // Pushing the default CB once per Begin/EndFilling (i.e once per camera per frame).
                if (!m_IsDefaultBufferUploaded)
                {
                    m_DefaultConstantBuffer.UpdateData(cmd, m_StagingVars);
                    m_IsDefaultBufferUploaded = true;
                }

                // If default buffer has been bound already and no override buffer has been bound since, free no-op!
                if (!m_IsDefaultBufferBound)
                {
                    m_DefaultConstantBuffer.SetGlobal(cmd, k_GlobalShaderVariablesID);
                    m_IsDefaultBufferBound = true;
                }

                return;
            }

            // Overrides go to their own buffer, so they never invalidate the defaults one.
            m_OverrideConstantBuffer ??= new ConstantBuffer<GlobalShaderVariables>(ComputeBufferMode.Dynamic);
            m_OverrideConstantBuffer.PushGlobal(cmd, m_StagingVars, k_GlobalShaderVariablesID);

            // The override buffer owns the slot now, so the next defaults push has to bind again.
            m_IsDefaultBufferBound = false;
        }

        public void ResetToDefault() => ResetToDefault(GlobalShaderVariablesGroup.All);

        internal void ResetToDefault(GlobalShaderVariablesGroup groups)
        {
            groups &= m_OwnedGroups;

            // Nothing to restore when the fill never wrote the group, and nothing to copy either. The diverged bit is
            // only cleared where the defaults were really copied back: a group the fill skipped keeps whatever staging
            // holds for it, so claiming it matches the defaults would drop that value on the constant buffer path.
            if ((groups & GlobalShaderVariablesGroup.Base) != 0 && m_DefaultDirty.varsBase != GlobalShaderVariablesBaseDirty.None)
            {
                m_StagingVars.varsBase = m_DefaultVars.varsBase;
                m_StagingDirty.varsBase |= m_DefaultDirty.varsBase;
                m_DivergedGroups &= ~GlobalShaderVariablesGroup.Base;
            }

            if ((groups & GlobalShaderVariablesGroup.Only3D) != 0 && m_DefaultDirty.vars3D != GlobalShaderVariablesOnly3DDirty.None)
            {
                m_StagingVars.vars3D = m_DefaultVars.vars3D;
                m_StagingDirty.vars3D |= m_DefaultDirty.vars3D;
                m_DivergedGroups &= ~GlobalShaderVariablesGroup.Only3D;
            }

            // URP 2D-only globals, empty so disabled.
            // if ((groups & GlobalShaderVariablesGroup.Only2D) != 0 && m_DefaultDirty.vars2D != GlobalShaderVariablesOnly2DDirty.None)
            // {
            //     m_StagingVars.vars2D = m_DefaultVars.vars2D;
            //     m_StagingDirty.vars2D |= m_DefaultDirty.vars2D;
            //     m_DivergedGroups &= ~GlobalShaderVariablesGroup.Only2D;
            // }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MarkBaseDirty(GlobalShaderVariablesBaseDirty flag)
        {
            m_StagingDirty.varsBase |= flag;
            m_DivergedGroups |= GlobalShaderVariablesGroup.Base;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Mark3DDirty(GlobalShaderVariablesOnly3DDirty flag)
        {
            m_StagingDirty.vars3D |= flag;
            m_DivergedGroups |= GlobalShaderVariablesGroup.Only3D;
        }

        // URP 2D-only globals, empty so disabled.
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // private void Mark2DDirty(GlobalShaderVariablesOnly2DDirty flag)
        // {
        //     m_StagingDirty.vars2D |= flag;
        //     m_DivergedGroups |= GlobalShaderVariablesGroup.Only2D;
        // }

        /// <summary>
        /// Fills the environment variables from the current render settings.
        /// </summary>
        public void SetEnvironmentVars()
        {
            var ambientSH = RenderSettings.ambientProbe;
            var linearGlossyEnvColor = new Color(ambientSH[0, 0], ambientSH[1, 0], ambientSH[2, 0]) * RenderSettings.reflectionIntensity;

            _GlossyEnvironmentColor = CoreUtils.ConvertLinearToActiveColorSpace(linearGlossyEnvColor);
            _SubtractiveShadowColor = CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.subtractiveShadowColor);
            _GlossyEnvironmentCubeMap_HDR = ReflectionProbe.defaultTextureHDRDecodeValues;
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
                MarkBaseDirty(GlobalShaderVariablesBaseDirty._Time);
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
                MarkBaseDirty(GlobalShaderVariablesBaseDirty._SinTime);
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
                MarkBaseDirty(GlobalShaderVariablesBaseDirty._CosTime);
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
                MarkBaseDirty(GlobalShaderVariablesBaseDirty.unity_DeltaTime);
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
                MarkBaseDirty(GlobalShaderVariablesBaseDirty._TimeParameters);
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
                MarkBaseDirty(GlobalShaderVariablesBaseDirty._LastTimeParameters);
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
                MarkBaseDirty(GlobalShaderVariablesBaseDirty._ScreenParams);
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
                MarkBaseDirty(GlobalShaderVariablesBaseDirty._ZBufferParams);
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
                MarkBaseDirty(GlobalShaderVariablesBaseDirty.unity_OrthoParams);
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
                MarkBaseDirty(GlobalShaderVariablesBaseDirty._RTHandleScale);
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
                MarkBaseDirty(GlobalShaderVariablesBaseDirty._ScaledScreenParams);
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
                MarkBaseDirty(GlobalShaderVariablesBaseDirty._ScreenSize);
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
                MarkBaseDirty(GlobalShaderVariablesBaseDirty._ScreenSizeOverride);
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
                MarkBaseDirty(GlobalShaderVariablesBaseDirty._ScreenCoordScaleBias);
            }
        }

        /// <summary>
        /// Ambient sky color of the gradient ambient mode.
        /// </summary>
        public Vector4 unity_AmbientSky
        {
            get => m_StagingVars.varsBase.unity_AmbientSky;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.varsBase.unity_AmbientSky = value;
                MarkBaseDirty(GlobalShaderVariablesBaseDirty.unity_AmbientSky);
            }
        }

        /// <summary>
        /// Ambient equator color of the gradient ambient mode.
        /// </summary>
        public Vector4 unity_AmbientEquator
        {
            get => m_StagingVars.varsBase.unity_AmbientEquator;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.varsBase.unity_AmbientEquator = value;
                MarkBaseDirty(GlobalShaderVariablesBaseDirty.unity_AmbientEquator);
            }
        }

        /// <summary>
        /// Ambient ground color of the gradient ambient mode.
        /// </summary>
        public Vector4 unity_AmbientGround
        {
            get => m_StagingVars.varsBase.unity_AmbientGround;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.varsBase.unity_AmbientGround = value;
                MarkBaseDirty(GlobalShaderVariablesBaseDirty.unity_AmbientGround);
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
                MarkBaseDirty(GlobalShaderVariablesBaseDirty._GlobalMipBias);
            }
        }

        /// <summary>
        /// Inverse width of the LOD cross fade dithering texture.
        /// </summary>
        public float _DitheringTextureInvSize
        {
            get => m_StagingVars.varsBase._DitheringTextureInvSize;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.varsBase._DitheringTextureInvSize = value;
                MarkBaseDirty(GlobalShaderVariablesBaseDirty._DitheringTextureInvSize);
            }
        }

        /// <summary>
        /// Ambient color used for glossy reflections when no reflection probe applies.
        /// </summary>
        public Vector4 _GlossyEnvironmentColor
        {
            get => m_StagingVars.varsBase._GlossyEnvironmentColor;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.varsBase._GlossyEnvironmentColor = value;
                MarkBaseDirty(GlobalShaderVariablesBaseDirty._GlossyEnvironmentColor);
            }
        }

        /// <summary>
        /// Shadow color of the subtractive mixed lighting mode.
        /// </summary>
        public Vector4 _SubtractiveShadowColor
        {
            get => m_StagingVars.varsBase._SubtractiveShadowColor;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.varsBase._SubtractiveShadowColor = value;
                MarkBaseDirty(GlobalShaderVariablesBaseDirty._SubtractiveShadowColor);
            }
        }

        /// <summary>
        /// HDR decode instructions of the default reflection cubemap.
        /// </summary>
        public Vector4 _GlossyEnvironmentCubeMap_HDR
        {
            get => m_StagingVars.varsBase._GlossyEnvironmentCubeMap_HDR;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.varsBase._GlossyEnvironmentCubeMap_HDR = value;
                MarkBaseDirty(GlobalShaderVariablesBaseDirty._GlossyEnvironmentCubeMap_HDR);
            }
        }

        // URP 3D-only shader variables (GlobalShaderVariablesOnly3D)
        /// <summary>
        /// World to light matrix of the main light cookie.
        /// </summary>
        public Matrix4x4 _MainLightWorldToLight
        {
            get => m_StagingVars.vars3D._MainLightWorldToLight;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._MainLightWorldToLight = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._MainLightWorldToLight);
            }
        }

        /// <summary>
        /// Direction of the main light with w = 0 for a directional light, position with w = 1 otherwise.
        /// </summary>
        public Vector4 _MainLightPosition
        {
            get => m_StagingVars.vars3D._MainLightPosition;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._MainLightPosition = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._MainLightPosition);
            }
        }

        /// <summary>
        /// Color of the main light. In Forward+, w stores whether it uses subtractive mixed mode.
        /// </summary>
        public Vector4 _MainLightColor
        {
            get => m_StagingVars.vars3D._MainLightColor;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._MainLightColor = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._MainLightColor);
            }
        }

        /// <summary>
        /// Occlusion probe channels of the main light.
        /// </summary>
        public Vector4 _MainLightOcclusionProbes
        {
            get => m_StagingVars.vars3D._MainLightOcclusionProbes;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._MainLightOcclusionProbes = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._MainLightOcclusionProbes);
            }
        }

        /// <summary>
        /// Rendering layer mask of the main light.
        /// </summary>
        public uint _MainLightLayerMask
        {
            get => m_StagingVars.vars3D._MainLightLayerMask;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._MainLightLayerMask = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._MainLightLayerMask);
            }
        }

        /// <summary>
        /// Forward+ cluster parameters, read through the URP_FP_* macros in Input.hlsl.
        /// [zBinScale, zBinOffset, lightCount, directionalLightCount]
        /// </summary>
        public Vector4 _FPParams0
        {
            get => m_StagingVars.vars3D._FPParams0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._FPParams0 = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._FPParams0);
            }
        }

        /// <summary>
        /// [tileScale.x, tileScale.y, tileCountX, wordsPerTile]
        /// </summary>
        public Vector4 _FPParams1
        {
            get => m_StagingVars.vars3D._FPParams1;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._FPParams1 = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._FPParams1);
            }
        }

        /// <summary>
        /// [zBinCount, tileCount, 0, 0]
        /// </summary>
        public Vector4 _FPParams2
        {
            get => m_StagingVars.vars3D._FPParams2;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._FPParams2 = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._FPParams2);
            }
        }

        /// <summary>
        /// Non jittered view projection matrix of this camera, for motion vectors. Unread in XR single pass, where the
        /// shader macros redirect to the Stereo array instead.
        /// </summary>
        public Matrix4x4 _NonJitteredViewProjMatrix
        {
            get => m_StagingVars.vars3D._NonJitteredViewProjMatrix;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._NonJitteredViewProjMatrix = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._NonJitteredViewProjMatrix);
            }
        }

        /// <summary>
        /// Previous frame non jittered view projection matrix of this camera, for motion vectors. Unread in XR single
        /// pass, where the shader macros redirect to the Stereo array instead.
        /// </summary>
        public Matrix4x4 _PrevViewProjMatrix
        {
            get => m_StagingVars.vars3D._PrevViewProjMatrix;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._PrevViewProjMatrix = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._PrevViewProjMatrix);
            }
        }

        /// <summary>
        /// Number of additional lights the shader may read, in x. The arrays it indexes stay loose uniforms.
        /// </summary>
        public Vector4 _AdditionalLightsCount
        {
            get => m_StagingVars.vars3D._AdditionalLightsCount;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._AdditionalLightsCount = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._AdditionalLightsCount);
            }
        }

        /// <summary>
        /// Sets the five world to shadow matrices at once: they share a single dirty flag because SetGlobals pushes
        /// them as one matrix array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMainLightWorldToShadow(in MainLightShadowMatrices matrices)
        {
            m_StagingVars.vars3D._MainLightWorldToShadow = matrices;
            Mark3DDirty(GlobalShaderVariablesOnly3DDirty._MainLightWorldToShadow);
        }

        /// <summary>
        /// Bounding sphere of the first cascade, xyz = center and w = radius.
        /// </summary>
        public Vector4 _CascadeShadowSplitSpheres0
        {
            get => m_StagingVars.vars3D._CascadeShadowSplitSpheres0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._CascadeShadowSplitSpheres0 = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._CascadeShadowSplitSpheres0);
            }
        }

        /// <summary>
        /// Bounding sphere of the second cascade, xyz = center and w = radius.
        /// </summary>
        public Vector4 _CascadeShadowSplitSpheres1
        {
            get => m_StagingVars.vars3D._CascadeShadowSplitSpheres1;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._CascadeShadowSplitSpheres1 = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._CascadeShadowSplitSpheres1);
            }
        }

        /// <summary>
        /// Bounding sphere of the third cascade, xyz = center and w = radius.
        /// </summary>
        public Vector4 _CascadeShadowSplitSpheres2
        {
            get => m_StagingVars.vars3D._CascadeShadowSplitSpheres2;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._CascadeShadowSplitSpheres2 = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._CascadeShadowSplitSpheres2);
            }
        }

        /// <summary>
        /// Bounding sphere of the fourth cascade, xyz = center and w = radius.
        /// </summary>
        public Vector4 _CascadeShadowSplitSpheres3
        {
            get => m_StagingVars.vars3D._CascadeShadowSplitSpheres3;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._CascadeShadowSplitSpheres3 = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._CascadeShadowSplitSpheres3);
            }
        }

        /// <summary>
        /// Squared radii of the four cascade bounding spheres.
        /// </summary>
        public Vector4 _CascadeShadowSplitSphereRadii
        {
            get => m_StagingVars.vars3D._CascadeShadowSplitSphereRadii;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._CascadeShadowSplitSphereRadii = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._CascadeShadowSplitSphereRadii);
            }
        }

        /// <summary>
        /// Soft shadow sampling offsets, xy = offset0 and zw = offset1.
        /// </summary>
        public Vector4 _MainLightShadowOffset0
        {
            get => m_StagingVars.vars3D._MainLightShadowOffset0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._MainLightShadowOffset0 = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._MainLightShadowOffset0);
            }
        }

        /// <summary>
        /// Soft shadow sampling offsets, xy = offset2 and zw = offset3.
        /// </summary>
        public Vector4 _MainLightShadowOffset1
        {
            get => m_StagingVars.vars3D._MainLightShadowOffset1;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._MainLightShadowOffset1 = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._MainLightShadowOffset1);
            }
        }

        /// <summary>
        /// xy = 1/width and 1/height, zw = width and height of the main light shadowmap.
        /// </summary>
        public Vector4 _MainLightShadowmapSize
        {
            get => m_StagingVars.vars3D._MainLightShadowmapSize;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._MainLightShadowmapSize = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._MainLightShadowmapSize);
            }
        }

        /// <summary>
        /// Soft shadow sampling offsets, xy = offset0 and zw = offset1.
        /// </summary>
        public Vector4 _AdditionalShadowOffset0
        {
            get => m_StagingVars.vars3D._AdditionalShadowOffset0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._AdditionalShadowOffset0 = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._AdditionalShadowOffset0);
            }
        }

        /// <summary>
        /// Soft shadow sampling offsets, xy = offset2 and zw = offset3.
        /// </summary>
        public Vector4 _AdditionalShadowOffset1
        {
            get => m_StagingVars.vars3D._AdditionalShadowOffset1;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._AdditionalShadowOffset1 = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._AdditionalShadowOffset1);
            }
        }

        /// <summary>
        /// xy = 1/width and 1/height, zw = width and height of the additional lights shadowmap.
        /// </summary>
        public Vector4 _AdditionalShadowmapSize
        {
            get => m_StagingVars.vars3D._AdditionalShadowmapSize;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._AdditionalShadowmapSize = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._AdditionalShadowmapSize);
            }
        }

        /// <summary>
        /// LightCookieShaderFormat of the main light cookie texture, None when it has no cookie.
        /// </summary>
        public float _MainLightCookieTextureFormat
        {
            get => m_StagingVars.vars3D._MainLightCookieTextureFormat;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._MainLightCookieTextureFormat = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._MainLightCookieTextureFormat);
            }
        }

        /// <summary>
        /// LightCookieShaderFormat of the additional lights cookie atlas.
        /// </summary>
        public float _AdditionalLightsCookieAtlasTextureFormat
        {
            get => m_StagingVars.vars3D._AdditionalLightsCookieAtlasTextureFormat;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._AdditionalLightsCookieAtlasTextureFormat = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._AdditionalLightsCookieAtlasTextureFormat);
            }
        }

        /// <summary>
        /// Highest value a rendering layer mask can hold, given the configured mask size.
        /// </summary>
        public uint _RenderingLayerMaxInt
        {
            get => m_StagingVars.vars3D._RenderingLayerMaxInt;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._RenderingLayerMaxInt = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._RenderingLayerMaxInt);
            }
        }

        /// <summary>
        /// 1 when Adaptive Probe Volume data is loaded and usable, 0 otherwise.
        /// </summary>
        public uint _EnableProbeVolumes
        {
            get => m_StagingVars.vars3D._EnableProbeVolumes;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                m_StagingVars.vars3D._EnableProbeVolumes = value;
                Mark3DDirty(GlobalShaderVariablesOnly3DDirty._EnableProbeVolumes);
            }
        }

        // URP 2D-only shader variables (GlobalShaderVariablesOnly2D), empty so disabled.
        // public Vector4 _URPDummy2D
        // {
        //     get => m_StagingVars.vars2D._URPDummy2D;
        //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //     set
        //     {
        //         m_StagingVars.vars2D._URPDummy2D = value;
        //         Mark2DDirty(GlobalShaderVariablesOnly2DDirty._URPDummy2D);
        //     }
        // }
    }
}
