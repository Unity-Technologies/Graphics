using RecipeEngine.Api.Platforms;
using RecipeEngine.Api.Settings;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Modules.Wrench.Settings;
using RecipeEngine.Platforms;

namespace Graphics.Cookbook.Settings;

public class GraphicsSettings : AnnotatedSettingsBase
{
    // Path from the root of the repository where packages are located.
    readonly string[] PackagesRootPaths = {"."};
    readonly static string s_PackageName = "com.unity.postprocessing";

    // update this to list all packages in this repo that you want to release.
    Dictionary<string, PackageOptions> PackageOptions = new()
    {
        {
            s_PackageName,
            new PackageOptions() { ReleaseOptions = new ReleaseOptions() { IsReleasing = true } },
        }
    };


    // You can either use a platform.json file or specify custom yamato VM images for each package in code.
    private readonly Dictionary<SystemType, Platform> ImageOverrides = new()
    {
        {
            SystemType.Windows,
            new Platform(new Agent("package-ci/win10:default", FlavorType.BuildLarge, ResourceType.Vm), SystemType.Windows)
        },
        {
            SystemType.MacOS,
            new Platform(new Agent("package-ci/macos-13:default", FlavorType.BuildExtraLarge, ResourceType.VmOsx),
                SystemType.MacOS)
        },
        {
            SystemType.Ubuntu,
            new Platform(new Agent("package-ci/ubuntu-18.04:default", FlavorType.BuildLarge, ResourceType.Vm),
                SystemType.Ubuntu)
        }
    };

    public GraphicsSettings()
    {
        Wrench = new WrenchSettings(
            PackagesRootPaths,
            PackageOptions
        );

        Wrench.Packages[s_PackageName].AdditionalEditorVersions =
        [
            "2019.4",
        ];
        Wrench.Packages[s_PackageName].EditorPlatforms = ImageOverrides;
    }

    public WrenchSettings Wrench { get; private set; }
}
