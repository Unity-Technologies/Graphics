# VFX Graph template window

The VFX Graph template window allows you to create a new VFX graph from an existing template with a predefined effect. You can use these templates as a starting point for your own effects.

There are multiple ways to [access the VFX Graph template window](#access-the-vfx-graph-template-window).

**Note**: The template browser displays only templates that are compatible with the current project.

![The template window](Images/templates-window.png)

| Label | Name | Description |
| :--- | :--- | :--- |
| **A** | Template list | Lists all the available templates you can select and start from to create a new VFX graph.<br/><br/>**Note**: the **Install Learning Templates** button imports the [Learning Templates sample](sample-learningTemplates.md) from the Visual Effect Graph package. |
| **B** | Template details | Displays a picture and description of the selected template. |
| **C** | Search and filtering tool | Filters the template list using the [Unity Search](https://docs.unity3d.com/Manual/search-overview.html) functionality. Type text to search templates by name or select **Add** (+) to filter templates based on specific characteristics.<br/>In addition to some of the default Unity Search options, Visual Effect Graph allows you to filter the list by template grouping using **Category**. |
| **D** | Sorting tool | Sorts the templates within their respective categories. The categories remain listed in alphabetical order. The options are:<ul><li>**Sort By Name**: Lists templates in alphabetical order.</li><li>**Sort By Order**: Lists templates in VFX Graph's default order. </li><li>**Sort By Modification Date**: Lists the last modified templates first.</li><li>**Sort By Last Used**: Lists the last used templates first.</li><li>**Sort By Favorite**: Lists templates marked as favorites first.</li></ul>**Note**: To mark a template as a favorite, hover over the template in the list and select the gray star that appears. To remove a template as a favorite, select the star again. |
| **E** | **Cancel** | Closes the window and cancels the VFX graph asset creation. |
| **F** | **Create** | Creates a new VFX graph asset based on the selected template. |

## Access the VFX Graph template window

### From the Project window

1. Right-click in your Project window.

1. Select **Create** > **Visual Effects** > **Visual Effect Graph**.

### From the VFX Graph editor toolbar

![toolbar](Images/templates-window-toolbar.png)

1. In the VFX Graph window's toolbar, select the drop-down arrow besides the **Add** (+) button.

1. Select one of the available options:
    * **Create from template** to create a new asset file from a template.
    * **Insert template** to insert a template in the current graph.

Once you complete a template insertion, Unity places it at the center of the VFX Graph window's workspace.    

> [!TIP]
> To create a new VFX graph asset, you can also hold the `CTRL` key while you directly select **Add** (+) in the toolbar.

### From the VFX Graph workspace

1. Right-click in the VFX Graph window's workspace.

1. Select **Insert template**.

Once you complete the VFX graph asset creation, Unity inserts the template at the right-click position.

## Create a custom VFX graph template

You can create your own VFX graph templates to have them available in the template browser.

To create a custom VFX graph template, follow these steps:

1. In the **Project** window, select the VFX graph asset you want to use as a template.

1. In the **Inspector** window, select **Use as Template**.

1. Expand the **Template** section.

1. Optional: Set the metadata that describes the template in the template browser: **Name**, **Category**, **Description**, **Icon**, and **Thumbnail**.

## Additional resources

* [Create a new VFX graph](GettingStarted.md)
* [Visual Effect Graph Asset reference](VisualEffectGraphAsset.md)
