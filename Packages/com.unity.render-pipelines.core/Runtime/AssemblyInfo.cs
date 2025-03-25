using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.RenderPipelines.Core.Runtime.Shared")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.Core.Editor")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.Core.Runtime.Tests")]
[assembly: InternalsVisibleTo("Unity.GraphicTests.Performance.RPCore.Runtime")]
[assembly: InternalsVisibleTo("Unity.GraphicTests.Performance.Universal.Runtime")] // access to internal ProfileIds

// Smoke test project visibility
[assembly: InternalsVisibleTo("SRPSmoke.Runtime.Tests")]
[assembly: InternalsVisibleTo("SRPSmoke.Editor.Tests")]
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor-testable")]
