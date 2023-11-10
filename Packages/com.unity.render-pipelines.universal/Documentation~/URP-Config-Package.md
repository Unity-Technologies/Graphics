# The Universal Render Pipeline Config package

The Universal Render Pipeline (URP) uses a separate [package](https://docs.unity3d.com/Manual/Packages.html) to control the settings of some of its features.

For example, you can use it to:

* Change the max number of visible light.

## Using the URP Config package

To use the URP Config package in your URP Project, you need to create a local copy of it and make your Project's package manifest reference it like so:

* In your Project's directory, move and rename the folder "**/Library/PackageCache/com.unity.render-pipelines.universal-config@[versionnumber]**" to "**/Packages/com.unity.render-pipelines.universal-config**".

**Note**: Using the Package Manager to upgrade your URP package does not automatically upgrade the local config package. To manually upgrade the local config package:

1. Make a copy of your current config package.
2. Delete the **com.unity.render-pipelines.universal-config** folder in your **Packages/** folder.
3. Copy again the folder from the **Library/PackageCache/** like mentionned above.
4. Apply your modifications by hand to the new config package.

## Configuring URP using the config package

You can edit the **ShaderConfig.cs** file to configure the properties of your URP Project. If you edit this file, you must also update the equivalent **ShaderConfig.cs.hlsl** header file (which URP Shaders use) so that it mirrors the definitions that you set in **ShaderConfig.cs**. You can update the **ShaderConfig.cs.hlsl** file in two ways. You can either make Unity generate the **ShaderConfig.cs.hlsl** file from the **ShaderConfig.cs** file, which makes sure that the two files are synchronized, or edit the **ShaderConfig.cs.hlsl** file directly, which is faster but it is up to you to synchronize the files when you make changes.

To ensure that the two files are synchronized, you should follow the first method. To do this:

1. Go to **Packages > com.unity.render-pipelines.universal-config > Runtime** and open **ShaderConfig.cs**.
2. Edit the values of the properties that you want to change and then save the file.
3. Back in Unity, select **Edit > RenderPipeline > Generate Include Files**.
4. Unity automatically configures your Project and Shaders to use the new configuration.
