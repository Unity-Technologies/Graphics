using System;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    public partial struct GPUSort
    {
        private const uint kWorkGroupSize = 1024;

        private string[] m_StageNames;
        private LocalKeyword[] m_Keywords;

        enum Stage
        {
            LocalBMS,
            LocalDisperse,
            BigFlip,
            BigDisperse
        }

        private SystemResources resources;

        public GPUSort(SystemResources resources)
        {
            this.resources = resources;

            m_Keywords = new LocalKeyword[4]
            {
                new(resources.computeAsset, "STAGE_BMS"),
                new(resources.computeAsset, "STAGE_LOCAL_DISPERSE"),
                new(resources.computeAsset, "STAGE_BIG_FLIP"),
                new(resources.computeAsset, "STAGE_BIG_DISPERSE")
            };

            m_StageNames = new[]
            {
                Enum.GetName(typeof(Stage), Stage.BigDisperse),
                Enum.GetName(typeof(Stage), Stage.BigFlip),
                Enum.GetName(typeof(Stage), Stage.LocalDisperse),
                Enum.GetName(typeof(Stage), Stage.LocalBMS)
            };
        }

        void DispatchStage(CommandBuffer cmd, Args args, uint h, Stage stage)
        {
            Assert.IsTrue(args.workGroupCount != -1);
            Assert.IsNotNull(resources.computeAsset);

            // When the is no geometry, instead of computing the distance field, we clear it with a big value.
            using (new ProfilingScope(cmd, new ProfilingSampler(m_StageNames[(int)stage])))
            {
    #if false
                m_SortCS.enabledKeywords = new[]  { keywords[(int)stage] };
    #else
                // Unfortunately need to configure the keywords like this. Might be worth just having a kernel per stage.
                foreach (var k in m_Keywords)
                    cmd.SetKeyword(resources.computeAsset, k, false);
                cmd.SetKeyword(resources.computeAsset, m_Keywords[(int)stage], true);
    #endif

                cmd.SetComputeIntParam(resources.computeAsset, "_H", (int)h);
                cmd.SetComputeBufferParam(resources.computeAsset, 0, "_KeyBuffer", args.resources.sortBufferKeys);
                cmd.SetComputeBufferParam(resources.computeAsset, 0, "_ValueBuffer", args.resources.sortBufferValues);
                cmd.DispatchCompute(resources.computeAsset, 0, args.workGroupCount, 1, 1);
            }
        }

        internal static int DivRoundUp(int x, int y) => (x + y - 1) / y;

        public void Dispatch(CommandBuffer cmd, Args args)
        {
            var n = args.count;

            cmd.CopyBuffer(args.inputKeys,   args.resources.sortBufferKeys);
            cmd.CopyBuffer(args.inputValues, args.resources.sortBufferValues);

            args.workGroupCount = Math.Max(1, DivRoundUp((int)n, (int)kWorkGroupSize * 2));

            uint h = Math.Min(kWorkGroupSize * 2, args.maxDepth);

            DispatchStage(cmd, args, h, Stage.LocalBMS);

            h *= 2;

            for (; h <= Math.Min(n, args.maxDepth); h *= 2)
            {
                DispatchStage(cmd, args, h, Stage.BigFlip);

                for (uint hh = h / 2; hh > 1; hh /= 2)
                {
                    if (hh <= kWorkGroupSize * 2)
                    {
                        DispatchStage(cmd, args, hh, Stage.LocalDisperse);
                        break;
                    }

                    DispatchStage(cmd, args, hh, Stage.BigDisperse);
                }
            }
        }

    }
}
