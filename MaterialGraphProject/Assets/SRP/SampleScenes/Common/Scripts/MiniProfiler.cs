using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Profiling;

public class MiniProfiler : MonoBehaviour {

    public bool m_Enable = true;

    private int frameCount = 0;
    private const int kAverageFrameCount = 64;
    private float m_AccDeltaTime;
    private float m_AvgDeltaTime;
 //   private	bool m_UseNewBatcher = false;

    internal class RecorderEntry
    {
        public string name;
        public float time;
        public int count;
        public float avgTime;
        public float avgCount;
        public float accTime;
        public int accCount;
        public Recorder recorder;
    };

    RecorderEntry[] recordersList =
    {
        new RecorderEntry() { name="RenderLoop.Draw" },
//        new RecorderEntry() { name="BatchRenderer.Flush" },
        new RecorderEntry() { name="Shadows.Draw" },
//        new RecorderEntry() { name="BatchRenderer.ApplyShaderPass" },
//            new RecorderEntry() { name="Batch.DrawStatic" },
//        new RecorderEntry() { name="DrawBuffersBatchMode" },
/*
        new RecorderEntry() { name="NewBatch.Instances" },
        new RecorderEntry() { name="NewBatch.Elements" },
        new RecorderEntry() { name="NewBatch.DrawRanges" },
        new RecorderEntry() { name="NewBatch.S-Instances" },
        new RecorderEntry() { name="NewBatch.S-Elements" },
        new RecorderEntry() { name="NewBatch.S-DrawRanges" },
*/


/*
        new RecorderEntry() { name="gBatchGBufferObj" },
        new RecorderEntry() { name="gBatchGBufferBatch" },
        new RecorderEntry() { name="gBatchShadowObj" },
        new RecorderEntry() { name="gBatchShadowBatch" },
*/

    /*
            new RecorderEntry() { name="Map_PerDraw_Buffer" },
            new RecorderEntry() { name="Unmap_PerDraw_Buffer" },
            new RecorderEntry() { name="DrawBuffersBatchMode" },
            new RecorderEntry() { name="DrawBatchIndexed" },
            new RecorderEntry() { name="BatchRenderer.ApplyShaderPass" },
            new RecorderEntry() { name="PerformFlushProperties" },
    */


    /*
            new RecorderEntry() { name="GUI.Repaint" },
            new RecorderEntry() { name="PrepareValues" },
            new RecorderEntry() { name="ApplyGpuProgram" },
            new RecorderEntry() { name="WriteParameters" },
            new RecorderEntry() { name="FlushBuffers" },
            new RecorderEntry() { name="BindBuffers" },
            new RecorderEntry() { name="Gfx.ProcessCommand" },
    */
};

    void Awake()
    {
        for (int i=0;i<recordersList.Length;i++)
        {
            var sampler = Sampler.Get(recordersList[i].name);
            if ( sampler != null )
            {
                recordersList[i].recorder = sampler.GetRecorder();
            }
        }
    }


    void Update()
    {

        if (m_Enable)
        {

            // get timing & update average accumulators
            for (int i = 0; i < recordersList.Length; i++)
            {
                recordersList[i].time = recordersList[i].recorder.elapsedNanoseconds / 1000000.0f;
                recordersList[i].count = recordersList[i].recorder.sampleBlockCount;
                recordersList[i].accTime += recordersList[i].time;
                recordersList[i].accCount += recordersList[i].count;
            }

            m_AccDeltaTime += Time.deltaTime;

            frameCount++;
            // time to time, update average values & reset accumulators
            if (frameCount >= kAverageFrameCount)
            {
                for (int i = 0; i < recordersList.Length; i++)
                {
                    recordersList[i].avgTime = recordersList[i].accTime * (1.0f / kAverageFrameCount);
                    recordersList[i].avgCount = recordersList[i].accCount * (1.0f / kAverageFrameCount);
                    recordersList[i].accTime = 0.0f;
                    recordersList[i].accCount = 0;

                }

                m_AvgDeltaTime = m_AccDeltaTime / kAverageFrameCount;
                m_AccDeltaTime = 0.0f;
                frameCount = 0;
            }
        }

    }

    void OnGUI()
    {

        if (m_Enable)
        {
/*
			GUI.changed = false;
			if ( !Application.isEditor )
			{
				m_UseNewBatcher = GUI.Toggle(new Rect(10, 30, 200, 20), m_UseNewBatcher, "Use new render batch");
				if (GUI.changed)
				{
					UnityEngine.Experimental.Rendering.ScriptableRenderContext.UseNewBatchRenderer(m_UseNewBatcher);
				}
			}
*/
            GUI.color = new Color(1, 1, 1, .75f);
            float w = 500, h = 24 + (recordersList.Length+1) * 16 + 8;

            GUILayout.BeginArea(new Rect(10, 50, w, h), "Mini Profiler", GUI.skin.window);
            string sLabel = System.String.Format("<b>{0:F2} FPS ({1:F2}ms)</b>\n", 1.0f / m_AvgDeltaTime, Time.deltaTime * 1000.0f);
            for (int i = 0; i < recordersList.Length; i++)
            {
                sLabel += string.Format("{0:F2}ms (*{1:F2})\t({2:F2}ms *{3:F2})\t<b>{4}</b>\n", recordersList[i].avgTime, recordersList[i].avgCount, recordersList[i].time, recordersList[i].count, recordersList[i].name);
            }
            GUILayout.Label(sLabel);
            GUILayout.EndArea();
        }
    }

}
