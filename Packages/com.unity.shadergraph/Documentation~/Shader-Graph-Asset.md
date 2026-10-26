# Shader Graph asset reference

A shader graph asset is a file with the `.shadergraph` extension that contains a graph you create and edit in the [**Shader Graph** window](Shader-Graph-Window.md). It is also a shader that you can select from a material's shader dropdown, as any other shader.

To access the properties of a shader graph asset in the **Inspector** window, select the asset in your project.

## Action buttons

Manage the shader code that shader graph generates.

| Property                  | Description                                                                                                                                                                  |
|---------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Open Shader Editor**    | Opens the selected asset in the [Shader Graph window](Shader-Graph-Window.md) so you can edit the graph. |
| **View Generated Shader** | Opens the shader code that the shader graph generates in a text editor or an IDE, such as Visual Studio. The code includes all possible passes and targets.                  |
| **Regenerate**            | Updates the code you edited in your text editor or IDE. This button appears only when you select **View Generated Shader**.                                                 |
| **Copy Shader**           | Copies the shader code to the clipboard.                                                                                                                                     |

## Properties

Manage shader graph assets templates.

| Property                  | Description                                                                                                                                                                  |
|---------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Use As Template**       | Marks the selected shader graph asset as a template. When enabled, the asset appears in the [Shader Graph template browser](template-browser.md), but is no longer listed in any material's **Shader** dropdown by default. |
| **Expose As Shader**      | Keeps the asset listed in a material's **Shader** dropdown so you can still use it as a shader when you also use it as a template. This property and all properties in the **Template** section are available only when you enable **Use As Template**. |
| **Name**                  | Sets the name of the template in the Shader Graph template browser.                                                                                                            |
| **Category**              | Sets the category of the template in the Shader Graph template browser.                                                                                                      |
| **Description**           | Sets the description of the template in the Shader Graph template browser.                                                                                                     |
| **Icon**                  | Sets the icon that represents the template in the Shader Graph template browser.                                                                                               |
| **Thumbnail**             | Sets the image that represents the template in the Shader Graph template browser.                                                                                              |

## Apply and Revert buttons

Changes you make to a shader graph asset's properties in the **Inspector** window aren't saved automatically. Use the **Apply** and **Revert** buttons to manage these changes.

| **Property** | **Description** |
|---|---|
| **Apply** | Saves the changes you made to the properties in the **Properties** section. This button is available only when you have unsaved changes. |
| **Revert** | Discards the changes you made to the properties in the **Properties** section and restores the previously saved values. This button is unavailable until you make changes to the properties. |

> [!NOTE]
> If you enable [**Use As Template**](#properties) and enter template field values without selecting **Apply**, selecting **Revert** discards your entries and disables **Use As Template** again, because that change wasn't previously saved.

## Additional resources

* [Creating a new shader graph asset](Create-Shader-Graph.md)
* [Shader Graph Window](Shader-Graph-Window.md)
* [Shader Graph template browser](template-browser.md)
