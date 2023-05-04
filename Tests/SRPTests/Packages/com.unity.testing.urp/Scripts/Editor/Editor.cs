#if OCULUS_SDK
using com.unity.cliprojectsetup;

public class Editor
{
    // We call this method using Unity's -executeMethod CLI command in the build jobs to parse setup args for OCULUS_SDK
    public static void Setup()
    {
        var cliProjectSetup = new CliProjectSetup();
        cliProjectSetup.ParseCommandLineArgs();
        cliProjectSetup.ConfigureFromCmdlineArgs();
    }
}
#endif
