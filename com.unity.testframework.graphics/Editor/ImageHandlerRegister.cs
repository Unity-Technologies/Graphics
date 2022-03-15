using UnityEditor;
using UnityEditor.Networking.PlayerConnection;

[InitializeOnLoad]
public static class ImageHandlerRegister
{
    static ImageHandlerRegister()
    {
        EditorConnection.instance.Initialize();
        EditorConnection.instance.Register(FailedImageMessage.MessageId, ImageHandler.instance.HandleFailedImageEvent);

        AssemblyReloadEvents.beforeAssemblyReload += Unregister;
    }

    private static void Unregister()
    {
        EditorConnection.instance.Unregister(FailedImageMessage.MessageId, ImageHandler.instance.HandleFailedImageEvent);
        AssemblyReloadEvents.beforeAssemblyReload -= Unregister;
    }
}
