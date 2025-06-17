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
            "com.unity.postprocessing",
            new PackageOptions() { ReleaseOptions = new ReleaseOptions() { IsReleasing = true } }
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
