namespace UnityEngine.Rendering
{
    class GPUCopy
    {
        ComputeShader m_Shader;
        int k_SampleKernel_xyzw2x_8;
        int k_SampleKernel_xyzw2x_1;

        public GPUCopy(ComputeShader shader)
        {
            m_Shader = shader;
            k_SampleKernel_xyzw2x_8 = m_Shader.FindKernel("KSampleCopy4_1_x_8");
            k_SampleKernel_xyzw2x_1 = m_Shader.FindKernel("KSampleCopy4_1_x_1");
        }

        static readonly int _RectOffset = Shader.PropertyToID("_RectOffset");
        static readonly int _Result1 = Shader.PropertyToID("_Result1");
        static readonly int _Source4 = Shader.PropertyToID("_Source4");
        static int[] _IntParams = new int[2];

        void SampleCopyChannel(
            CommandBuffer cmd,
            RectInt rect,
            int _source,
            RenderTargetIdentifier source,
            int _target,
            RenderTargetIdentifier target,
            int slices,
            int kernel8,
            int kernel1)
        {
            RectInt main, topRow, rightCol, topRight;
            unsafe
            {
                RectInt* dispatch1Rects = stackalloc RectInt[3];
                int dispatch1RectCount = 0;
                RectInt dispatch8Rect = new RectInt(0, 0, 0, 0);

                if (TileLayoutUtils.TryLayoutByTiles(
                    rect,
                    8,
                    out main,
                    out topRow,
                    out rightCol,
                    out topRight))
                {
                    if (topRow.width > 0 && topRow.height > 0)
                    {
                        dispatch1Rects[dispatch1RectCount] = topRow;
                        ++dispatch1RectCount;
                    }
                    if (rightCol.width > 0 && rightCol.height > 0)
                    {
                        dispatch1Rects[dispatch1RectCount] = rightCol;
                        ++dispatch1RectCount;
                    }
                    if (topRight.width > 0 && topRight.height > 0)
                    {
                        dispatch1Rects[dispatch1RectCount] = topRight;
                        ++dispatch1RectCount;
                    }
                    dispatch8Rect = main;
                }
                else if (rect.width > 0 && rect.height > 0)
                {
                    dispatch1Rects[dispatch1RectCount] = rect;
                    ++dispatch1RectCount;
                }

                cmd.SetComputeTextureParam(m_Shader, kernel8, _source, source);
                cmd.SetComputeTextureParam(m_Shader, kernel1, _source, source);
                cmd.SetComputeTextureParam(m_Shader, kernel8, _target, target);
                cmd.SetComputeTextureParam(m_Shader, kernel1, _target, target);

                if (dispatch8Rect.width > 0 && dispatch8Rect.height > 0)
                {
                    var r = dispatch8Rect;
                    // Use intermediate array to avoid garbage
                    _IntParams[0] = r.x;
                    _IntParams[1] = r.y;
                    cmd.SetComputeIntParams(m_Shader, _RectOffset, _IntParams);
                    cmd.DispatchCompute(m_Shader, kernel8, (int)Mathf.Max(r.width / 8, 1), (int)Mathf.Max(r.height / 8, 1), slices);
                }

                for (int i = 0, c = dispatch1RectCount; i < c; ++i)
                {
                    var r = dispatch1Rects[i];
                    // Use intermediate array to avoid garbage
                    _IntParams[0] = r.x;
                    _IntParams[1] = r.y;
                    cmd.SetComputeIntParams(m_Shader, _RectOffset, _IntParams);
                    cmd.DispatchCompute(m_Shader, kernel1, (int)Mathf.Max(r.width, 1), (int)Mathf.Max(r.height, 1), slices);
                }
            }
        }

        public void SampleCopyChannel_xyzw2x(CommandBuffer cmd, RTHandle source, RTHandle target, RectInt rect)
        {
            Debug.Assert(source.rt.volumeDepth == target.rt.volumeDepth);
            SampleCopyChannel(cmd, rect, _Source4, source, _Result1, target, source.rt.volumeDepth, k_SampleKernel_xyzw2x_8, k_SampleKernel_xyzw2x_1);
        }
    }
}
