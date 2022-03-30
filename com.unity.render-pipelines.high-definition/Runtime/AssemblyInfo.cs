using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.RenderPipelines.HighDefinition.Editor")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.HighDefinition.Editor.Tests")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.HighDefinition.Runtime.Tests")]
[assembly: InternalsVisibleTo("HDRP_TestRunner")]
[assembly: InternalsVisibleTo("Unity.TextMeshPro.Editor")]
[assembly: InternalsVisibleTo("Unity.Industrial.Materials.AVRD.Runtime")]
[assembly: InternalsVisibleTo("Unity.Industrial.Materials.AVRD.Editor")]

//custom-begin: Expose internal guts to dependencies
[assembly: InternalsVisibleTo("Unity.DemoTeam.Playables")]
[assembly: InternalsVisibleTo("PropertyMaster")]
[assembly: InternalsVisibleTo("HDRP_TestRunner")]
//custom-end

