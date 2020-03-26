# Default Settings tab

The High Definition Render Pipeline (HDRP) adds the HDRP Default Settings tab to Unity's Project Settings window. You can use this tab to set up default settings for certain features in your Project. You can:

- Assign Render Pipeline resource Assets for your HDRP Project.
- Set the verboseness of Shader variant information that Unity writes to the Console window when you build your Project.
- Set up default [Frame Settings](Frame-Settings.html) for [Cameras](HDRP-Camera.html) to use.
- Assign and edit a default [Volume Profile](Volume-Profile.html).

The HDRP Default Settings tab is part of the Project Settings window. To get to this tab, select **Edit > Project Settings** and then, in the sidebar, click **HDRP Default Settings**.

## General Settings

| Property                         | Description                                                  |
| -------------------------------- | ------------------------------------------------------------ |
| Render Pipeline Resources        | Stores references to Shaders and Materials that HDRP uses.  When you build your Unity Project, HDRP embeds all of the resources that this Asset references. It allows you to set up multiple render pipelines in a Unity Project and, when you build the Project, Unity only embeds Shaders and Materials relevant for that pipeline. This is the Scriptable Render Pipeline equivalent of Unity’s Resources folder mechanism. When you create a new HDRP Asset, Unity also creates one of these and references it in the new HDRP Asset automatically. |
| Render Pipeline Editor Resources | Stores reference resources for the Editor only. Unity does not include these when you build your Unity Project.  When you create an HDRP Asset, Unity creates an HDRP Resources Asset, and the new HDRP Asset references it automatically. |
| Shader Variant Log Level         | Use the drop-down to select what information HDRP logs about Shader variants when you build your Unity Project. • Disabled: HDRP doesn’t log any Shader variant information.• Only HDRP Shaders: Only log Shader variant information for HDRP Shaders.• All Shaders: Log Shader variant information for every Shader type. |

## Frame Settings

[Frame Settings](Frame-Settings.html) control the rendering passes that Cameras make at runtime. This section allows you to set default Frame Settings that all Cameras use if you do not enable their Custom Frame Settings checkbox. For information about what each property does, see [Frame Settings](Frame-Settings.html).

## Volume Components

You can use this section to assign and edit a [Volume Profile](Volume-Profile.html) that [Volumes](Volumes.html) use by default in your Scenes. You do not need to create a Volume for this specific Volume Profile to be active, because HDRP always processes it as if it is assigned to a global Volume in the Scene, but with the lowest priority. This means that any Volume that you add to a Scene takes priority. 

The Default Volume Profile Asset references a Volume Profile in the HDRP Package folder called DefaultSettingsVolumeProfile by default. Below it, you can add [Volume overrides](Volume-Components.html), and edit their properties. You can also assign your own Volume Profile to this property field. Be aware that this property must always reference a Volume Profile. If you assign your own Volume Profile and then delete it, HDRP automatically re-assigns the DefaultSettingsVolumeProfile from the HDRP Package folder.

The LookDev Volume Profile Asset references the Volume Profile that will be used in the [LookDev window](Look-Dev.html). It works the same way than the Default Volume profile except that in this asset you can't put a [Visual Environment Component](Override-Visual-Environment.html) or skies component because they are overwritten by the LookDev.

## Custom Post Process Orders

Use this section to select which custom post processing effect will be used in the project and in which order they will be executed.  
You have one list per post processing injection point: `After Opaque And Sky`, `Before Post Process` and `After Post Process`. See the [Custom Post Process](Custom-Post-Process.html) documentation for more details.
