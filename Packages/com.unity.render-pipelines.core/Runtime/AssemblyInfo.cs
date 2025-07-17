using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.RenderPipelines.Core.Runtime.Shared")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.Core.Editor")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.Core.Runtime.Tests")]
[assembly: InternalsVisibleTo("Unity.GraphicTests.Performance.RPCore.Runtime")]
[assembly: InternalsVisibleTo("Unity.GraphicTests.Performance.Universal.Runtime")] // access to internal ProfileIds

// Access to SamplingResources for the PathTracing package, to be removed when its content will be moved to RP Core
[assembly: InternalsVisibleTo("Unity.Rendering.PathTracing.Runtime.Tests")]
[assembly: InternalsVisibleTo("Unity.PathTracing.Runtime.Tests")]
[assembly: InternalsVisibleTo("Unity.PathTracing.Editor.Tests")]
[assembly: InternalsVisibleTo("Unity.PathTracing.Runtime")]
[assembly: InternalsVisibleTo("Unity.PathTracing.Editor")]

// Smoke test project visibility
[assembly: InternalsVisibleTo("SRPSmoke.Runtime")]
[assembly: InternalsVisibleTo("SRPSmoke.Runtime.Tests")]
[assembly: InternalsVisibleTo("SRPSmoke.Editor.Tests")]
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor-testable")]
