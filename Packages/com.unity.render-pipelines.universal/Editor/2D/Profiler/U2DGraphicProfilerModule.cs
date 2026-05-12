using System;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEngine;
using UnityEngine.Rendering.Universal.U2D.Profiler;

namespace UnityEditor.U2D.Graphics.Profiler
{
    [Serializable]
    [ProfilerModuleMetadata("2D Graphics", IconPath = "Packages/com.unity.render-pipelines.universal/Editor/2D/PackageResources/ProfilerIcon/2D_Graphics.png")]
    class U2DGraphicProfilerModule : ProfilerModule
    {
        static readonly ProfilerCounterDescriptor[] k_Counters = new ProfilerCounterDescriptor[]
        {
            new ProfilerCounterDescriptor(ProfilerMarkers.k_U2DNormalMapProfilerCounterName, ProfilerCategory.U2D),
            new ProfilerCounterDescriptor(ProfilerMarkers.k_U2DLightProfilerCounterName, ProfilerCategory.U2D),
            new ProfilerCounterDescriptor(ProfilerMarkers.k_U2DShadowProfilerCounterName, ProfilerCategory.U2D),
            new ProfilerCounterDescriptor(ProfilerMarkers.k_U2DShadowCasterCounterName, ProfilerCategory.U2D),
            new ProfilerCounterDescriptor(ProfilerMarkers.k_U2DShadowVerticesCounterName, ProfilerCategory.U2D),
            new ProfilerCounterDescriptor(ProfilerMarkers.k_U2DLightBatchCounterName, ProfilerCategory.U2D),
            new ProfilerCounterDescriptor(ProfilerMarkers.k_U2DLightTriangleCounterName, ProfilerCategory.U2D),
        };

        public U2DGraphicProfilerModule()
            : base(k_Counters, ProfilerModuleChartType.Line)
        { }

        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            return new U2DGraphicsProfilerViewController(ProfilerWindow);
        }
    }
}
