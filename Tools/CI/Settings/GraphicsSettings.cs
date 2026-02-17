using RecipeEngine.Api.Settings;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Modules.Wrench.Settings;

namespace Graphics.Cookbook.Settings;

public class GraphicsSettings : AnnotatedSettingsBase
{
    // Path from the root of the repository where packages are located.
    readonly string[] PackagesRootPaths = {"."};

    // update this to list all packages in this repo that you want to release.
    Dictionary<string, PackageOptions> PackageOptions = new()
    {
        {
            "com.unity.testing.visualeffectgraph",
            new PackageOptions() { ReleaseOptions = new ReleaseOptions() { IsReleasing = false } }
        },
        {
            "com.unity.testing.xr",
            new PackageOptions() { ReleaseOptions = new ReleaseOptions() { IsReleasing = false } }
        },
        {
            "com.unity.shaderanalysis",
            new PackageOptions() { ReleaseOptions = new ReleaseOptions() { IsReleasing = false } }
        },
        {
            "com.unity.testing.brg",
            new PackageOptions() { ReleaseOptions = new ReleaseOptions() { IsReleasing = false } }
        },
        {
            "com.unity.template.universal",
            new PackageOptions() { ReleaseOptions = new ReleaseOptions() { IsReleasing = false } }
        },
        {
            "com.unity.visualeffectgraph",
            new PackageOptions() { ReleaseOptions = new ReleaseOptions() { IsReleasing = false } }
        },
        {
            "com.unity.shadergraph",
            new PackageOptions() { ReleaseOptions = new ReleaseOptions() { IsReleasing = false } }
        },
        {
            "com.unity.testing.urp-upgrade",
            new PackageOptions() { ReleaseOptions = new ReleaseOptions() { IsReleasing = false } }
        },
        {
            "com.unity.template.hdrp-blank",
            new PackageOptions() { ReleaseOptions = new ReleaseOptions() { IsReleasing = false } }
        },
        {
            "com.unity.testing.urp",
            new PackageOptions() { ReleaseOptions = new ReleaseOptions() { IsReleasing = false } }
        },
        {
            "com.unity.render-pipelines.core",
            new PackageOptions() { ReleaseOptions = new ReleaseOptions() { IsReleasing = false } }
        },
        {
            "com.unity.template.hd",
            new PackageOptions() { ReleaseOptions = new ReleaseOptions() { IsReleasing = false } }
        },
        {
            "com.unity.postprocessing",
            new PackageOptions() { ReleaseOptions = new ReleaseOptions() { IsReleasing = true } }
        },
        {
            "com.unity.render-pipelines.high-definition-config",
            new PackageOptions() { ReleaseOptions = new ReleaseOptions() { IsReleasing = false } }
        },
        {
            "com.unity.render-pipelines.universal",
            new PackageOptions() { ReleaseOptions = new ReleaseOptions() { IsReleasing = false } }
        },
        {
            "local.mock-hmd.references",
            new PackageOptions() { ReleaseOptions = new ReleaseOptions() { IsReleasing = false } }
        },
        {
            "com.unity.testing.hdrp",
            new PackageOptions() { ReleaseOptions = new ReleaseOptions() { IsReleasing = false } }
        },
        {
            "com.unity.render-pipelines.high-definition",
            new PackageOptions() { ReleaseOptions = new ReleaseOptions() { IsReleasing = false } }
        },
        {
            "com.unity.render-pipelines.universal-config",
            new PackageOptions() { ReleaseOptions = new ReleaseOptions() { IsReleasing = false } }
        }
    };

    public GraphicsSettings()
    {
        Wrench = new WrenchSettings(
            PackagesRootPaths,
            PackageOptions
        );
    }

    public WrenchSettings Wrench { get; private set; }
}
