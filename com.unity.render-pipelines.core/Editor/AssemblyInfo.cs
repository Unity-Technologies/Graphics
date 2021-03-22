using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // permit using internal interfaces with Moq
[assembly: InternalsVisibleTo("Unity.RenderPipelines.Core.Editor.Tests")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.Universal.Editor")] // remove as needed when API can be made public
[assembly: InternalsVisibleTo("Unity.RenderPipelines.UniversalUpgradeProjectTests")]
