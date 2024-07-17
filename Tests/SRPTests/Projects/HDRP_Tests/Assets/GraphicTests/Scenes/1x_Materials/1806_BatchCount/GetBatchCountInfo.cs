using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
#endif

public class GetBatchCountInfo : MonoBehaviour
{
    public TextMesh fPrepassText;
    public TextMesh dPrepassText;
    public TextMesh gbufferText;
    public TextMesh forwardText;
    public TextMesh shadowText;

    // Update is called once per frame
    bool previousProfilerValue = false;
    private bool previousGraphicsJobsValue = false;

    void OnEnable()
    {
        previousProfilerValue = ProfilerDriver.enabled;
        ProfilerDriver.enabled = true;
#if UNITY_EDITOR
        previousGraphicsJobsValue = PlayerSettings.graphicsJobs;
        ProfilerDriver.ClearAllFrames();
#endif

    }

#if UNITY_EDITOR
    private void Update()
    {
        // Leave some time for things to init in the profiler
        if (Time.timeSinceLevelLoad < 0.1f)
            return;

        int currentFrameIndex = Time.frameCount;

        currentFrameIndex = ProfilerDriver.GetPreviousFrameIndex(currentFrameIndex);
        var frameData = ProfilerDriver.GetRawFrameDataView(currentFrameIndex, 0);
        if (frameData == null)
            return;

        if (!frameData.valid)
            return;

        var applyShaderId = frameData.GetMarkerId("SRPBRender.ApplyShader");
        var applyShaderShadowId = frameData.GetMarkerId("SRPBShadow.ApplyShader");

        if (applyShaderId == -1 || applyShaderShadowId == -1)
            return;

        fPrepassText.text = GetApplyShaderCountInsideMarker("ForwardDepthPrepass", applyShaderId).ToString();
        dPrepassText.text = GetApplyShaderCountInsideMarker("DeferredDepthPrepass", applyShaderId).ToString();
        gbufferText.text = GetApplyShaderCountInsideMarker("GBuffer", applyShaderId).ToString();
        forwardText.text = GetApplyShaderCountInsideMarker("ForwardOpaque", applyShaderId).ToString();
        shadowText.text = GetApplyShaderCountInsideMarker("RenderShadowMaps", applyShaderShadowId).ToString();

        int GetApplyShaderCountInsideMarker(string markerName, int applyId)
        {
            int applyCount = 0;

            var mainTrackedMarker = frameData.GetMarkerId(markerName);
            if (applyId == FrameDataView.invalidMarkerId || mainTrackedMarker == FrameDataView.invalidMarkerId)
                return 0;

            int sampleCount = frameData.sampleCount;
            int cursor = 0;
            while (cursor < sampleCount)
            {
                if (mainTrackedMarker == frameData.GetSampleMarkerId(cursor))
                {
                    var nextSibling = frameData.GetSampleChildrenCountRecursive(cursor) + cursor;
                    while (cursor < nextSibling)
                    {
                        if (applyId == frameData.GetSampleMarkerId(cursor))
                            applyCount++;
                        cursor++;
                    }
                }
                cursor++;
            }

            return applyCount;
        }
    }
#endif

    void OnDisable()
    {
#if UNITY_EDITOR
         PlayerSettings.graphicsJobs = previousGraphicsJobsValue;
#endif
        ProfilerDriver.enabled = previousProfilerValue;
    }
}
