using System.Runtime.CompilerServices;

// Make visible to tests
[assembly: InternalsVisibleTo("Unity.PathTracing.Editor.Tests")]
[assembly: InternalsVisibleTo("Unity.PathTracing.Runtime.Tests")]
[assembly: InternalsVisibleTo("Unity.Testing.UnifiedRayTracing.Runtime")]
[assembly: InternalsVisibleTo("Unity.Testing.UnifiedRayTracing.Performance")]
[assembly: InternalsVisibleTo("Unity.UnifiedRayTracing.Editor.Tests")]
[assembly: InternalsVisibleTo("Assembly-CSharp-editor-testable")]

// Make visible internally to packages using the AccelStructAdapter, TODO: either make AccelStructAdapter public use intermediate Assembly to filter out exposed internals
[assembly: InternalsVisibleTo("Unity.PathTracing.Runtime")]
[assembly: InternalsVisibleTo("Unity.PathTracing.Editor")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.Core.Runtime")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.Core.Editor")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.HighDefinition.Runtime")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.Universal.Runtime")]
