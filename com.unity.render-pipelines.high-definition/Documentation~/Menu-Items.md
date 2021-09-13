# Menu items

The High Definition Render Pipeline (HDRP) adds menu items to the Unity menu bar. This page shows where each menu item is and describes how each works.

## Edit

This section includes all the menu items under the **Edit** menu fold-out. To access them, go to **Edit > Render Pipeline > HD Render Pipeline**.

### Upgrade From Previous Versions

This group of menu items is useful when you upgrade HDRP to a later version.

| **Item**                                                     | **Description**                                              |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| **Upgrade HDRP Materials to Latest Version**                 | Upgrades all HDRP Materials in the project to the latest version. This is useful if HDRP's automatic Material upgrade process fails to upgrade a Material. |
| **Add Decal Layer Default to Loaded Mesh Renderers and Terrains** | Adds the **Decal Layer Default** item to every Mesh Renderer and Terrain in the currently open scene. This is useful when upgrading your HDRP project from Unity 2020.1 to 2020.2, if you want to use [Decal Layers](Decal.md#decal-layers). |
| **Add Decal Layer Default to Selected Mesh Renderers and Terrains** | Adds the **Decal Layer Default** item to every selected Mesh Renderer and Terrain in the currently open scene. This is useful when upgrading your HDRP project from Unity 2020.1 to 2020.2, if you want to use [Decal Layers](Decal.md#decal-layers). |
| **Fix Warning 'referenced script in (Game Object 'SceneIDMap') is missing' in loaded scenes** | Fixes an issue that occurs if you enter Play Mode with Reflection Probes that Unity baked prior to 2019.3. This is useful when upgrading your HDRP project from Unity 2019.2 to 2019.3. |

 

### Upgrade From Built-in pipeline

This group of menu items is useful when you upgrade your Unity Project from using the Built-in Render Pipeline to HDRP.

| **Item**                                                    | **Description**                                              |
| ----------------------------------------------------------- | ------------------------------------------------------------ |
| **Upgrade Project Materials to High Definition Materials**  | Converts every compatible Material in your project to an HDRP Material. |
| **Upgrade Selected Materials to High Definition Materials** | Converts every compatible Material currently selected in the project window to an HDRP Material. |
| **Upgrade Scene Terrains to High Definition Terrains**      | Replaces the built-in default standard terrain Material in every [Terrain](https://docs.unity3d.com/Manual/script-Terrain.html) in the scene with the HDRP default Terrain Material. |

 

### Other items

This section includes all the menu items directly under the **Edit > Rendering** menu fold-out.

| **Item**                                                     | **Description**                                              |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| **Render Selected HDRP Camera to Log EXR**                   | Exports the current [Camera](HDRP-Camera.md)'s view to a log-encoded EXR file. This is useful when [authoring lookup textures](Authoring-LUTs.md). |
| **Export HDRP Sky to Image**                                 | Exports the current sky as a static HDRI.                    |
| **Check Scene Content for HDRP Ray Tracing**                 | Checks every GameObject in the current scene and throws warnings if:<br/>&#8226; A Mesh Filter references a null Mesh.<br/>&#8226; A Mesh Renderer references a null Material.<br/>&#8226; A sub-mesh within a single Renderer reference both a transparent and opaque Material.<br/>&#8226; A Mesh has more than 32 sub-meshes.<br/>&#8226; A Mesh contains both double-sided and single-sided sub-meshes.<br/>&#8226; An LODGroup has a missing Renderer in one of its children. |
| **Fix Warning 'referenced script in (Game Object 'SceneIDMap') is missing' in loaded scenes** | Fixes an issue that occurs if you enter Play Mode with Reflection Probes that Unity baked prior to 2019.3. This is useful when upgrading your HDRP project from Unity 2019.2 to 2019.3. |
| **Generate Shader Includes**                                 | Generates HLSL code based on C# structs to synchronize data and constants between shaders and C#. For more information on this feature, see [Synchronizing shader code and C#](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/generating-shader-includes.html). |

