# Shader Graph template browser

The Shader Graph template browser allows you to create a new shader graph from an existing template.

To access the Shader Graph template browser, right-click in your Project window and select **Create** > **Shader Graph** > **From Template**.

**Note**: The template browser displays only templates that are compatible with the current project.

![The template browser](images/template-browser.png)

| Label | Name | Description |
| :--- | :--- | :--- |
| **A** | Template list | Lists all the available templates you can select and start from to create a new shader graph. |
| **B** | Template details | Displays a picture and description of the selected template. |
| **C** | Search and filtering tool | Filters the template list using the [Unity Search](https://docs.unity3d.com/Manual/search-overview.html) functionality. Type text to search templates by name or select **Add** (+) to filter templates based on specific characteristics.<br/>In addition to some of the default Unity Search options, Shader Graph includes the following filters:<ul><li>**Category**: Filters by template grouping category.</li><li>**material**: Filters by the target material type.</li><li>**renderpipeline**: Filters by render pipeline.</li><li>**vfx**: Filters by Visual Effect Graph support.</li></ul> |
| **D** | Sorting tool | Sorts the templates within their respective categories. The categories remain listed in alphabetical order. The options are:<ul><li>**Sort By Name**: Lists templates in alphabetical order.</li><li>**Sort By Order**: Lists templates in Shader Graph's default order. </li><li>**Sort By Modification Date**: Lists the last modified templates first.</li><li>**Sort By Last Used**: Lists the last used templates first.</li><li>**Sort By Favorite**: Lists templates marked as favorites first.</li></ul>**Note**: To mark a template as a favorite, hover over the template in the list and select the gray star that appears. To remove a template as a favorite, select the star again. |
| **E** | **Cancel** | Closes the window and cancels the shader graph asset creation. |
| **F** | **Create** | Creates a new shader graph asset based on the selected template. |

## Create a custom shader graph template

You can create your own shader graph templates to have them available in the template browser. You can share these templates with your team to maintain consistency across shaders, for example in projects with unique lighting setups or specific shader requirements.

To create a custom shader graph template, follow these steps:

1. In the **Project** window, select the shader graph asset you want to use as a template.

1. In the **Inspector** window, select **Use As Template**.

1. Expand the **Template** section.

1. Optional: Set the metadata that describes the template in the template browser: **Name**, **Category**, **Description**, **Icon**, and **Thumbnail**.

> [!NOTE]
> By default, when you convert an existing shader graph asset into a template, you can no longer assign it to any materials through the [Material Inspector](https://docs.unity3d.com/Manual/class-Material.html). However, it remains active for materials that were already using it. To make the shader graph asset available again for any materials, enable the **Expose As Shader** option in the shader graph asset Inspector.

## Additional resources

* [Create a new shader graph](Create-Shader-Graph.md)
* [Shader Graph Asset reference](Shader-Graph-Asset.md)
