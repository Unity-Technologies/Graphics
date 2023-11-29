namespace UnityEngine.Rendering.Universal
{
    internal sealed class StpHistory : CameraHistoryItem
    {
        /// <summary>
        /// STP uses a custom history texture management system in order to simplify code sharing with other SRPs
        /// In URP's case, we potentially need two of these in order to support multi-pass XR rendering
        /// </summary>
        STP.HistoryContext[] m_historyContexts = new STP.HistoryContext[2];

        public override void OnCreate(BufferedRTHandleSystem owner, uint typeId)
        {
            base.OnCreate(owner, typeId);

            for (int eyeIndex = 0; eyeIndex < 2; ++eyeIndex)
            {
                m_historyContexts[eyeIndex] = new STP.HistoryContext();
            }
        }

        public override void Reset()
        {
            for (int eyeIndex = 0; eyeIndex < 2; ++eyeIndex)
            {
                // Clear internal data within the contexts whenever we're reset
                m_historyContexts[eyeIndex].Dispose();
            }
        }

        internal STP.HistoryContext GetHistoryContext(int eyeIndex)
        {
            // STP only supports XR multi-pass with two views
            Debug.Assert(eyeIndex < 2);

            return m_historyContexts[eyeIndex];
        }

        // Return true if the internal history data is invalid after the update operation
        internal bool Update(UniversalCameraData cameraData)
        {
            STP.HistoryUpdateInfo info;
            info.preUpscaleSize = new Vector2Int(cameraData.cameraTargetDescriptor.width, cameraData.cameraTargetDescriptor.height);
            info.postUpscaleSize = new Vector2Int(cameraData.pixelWidth, cameraData.pixelHeight);
            info.useHwDrs = false;
            info.useTexArray = cameraData.xr.enabled && cameraData.xr.singlePassEnabled;

            int eyeIndex = (cameraData.xr.enabled && !cameraData.xr.singlePassEnabled) ? cameraData.xr.multipassId : 0;

            bool hasValidHistory = GetHistoryContext(eyeIndex).Update(ref info);

            return !hasValidHistory;
        }
    }
}
