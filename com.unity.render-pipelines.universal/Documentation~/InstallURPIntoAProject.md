# Install into a Project

You can download and install the latest version of Universal Render Pipeline (URP) to your existing Project via the [Package Manager system](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@latest/index.html), and then install it into your Project. If you don’t have an existing Project, see documentation on [how to start a new URP Project from a template](CreateNewProjectFromTemplate.md).

To install URP into an existing Project:

1. In Unity, open your Project. In the top navigation bar, select __Window__ &gt: __Package Manager__ to open the __Package Manager__ window. Select the __All__ tab. This tab displays the list of available packages for the version of Unity that you are currently running.
2. Select **Universal RP** from the list of packages. In the top right corner of the window, select __Install__. This installs URP directly into your Project.

## Configure URP 

Before you can start using URP, you need to configure it. To do this, you need to create a Scriptable Render Pipeline Asset and adjust your Graphics settings. 

**Note:** URP uses its own [post-processing](integration-with-post-processing.md). If you have the Post Processing version 2 package installed in your Project already, you need to delete it before you install URP into your Project.

### Create the Universal Render Pipeline Asset

The [Universal Render Pipeline Asset](universalrp-asset.md) controls the global rendering and quality settings of your Project, and creates the rendering pipeline instance. The rendering pipeline instance contains intermediate resources and the render pipeline implementation.  

To create a Universal Render Pipeline Asset:

1. In the Editor, go to the Project window.
2. Right-click in the Project window, and select  __Create__ &gt; __Rendering__ &gt: __Universal Render Pipeline__&gt: __Pipeline Asset__. Alternatively, navigate to the menu bar at the top, and select __Assets__ &gt: __Create__ &gt: __Rendering__ &gt: __Universal Render Pipeline__ &gt: __Pipeline Asset__.

You can either leave the default name for the new Universal Render Pipeline Asset, or type a new one. 


### Add the Asset to your Graphics settings

To use URP, you need to add the newly created Universal Render Pipeline Asset to your Graphics settings in Unity. If you don't, Unity still tries to use the Built-in render pipeline.

To add the Universal Render Pipeline Asset to your Graphics settings:


1. Navigate to __Edit__ &gt; __Project Settings...__ &gt; __Graphics__. 
2. In the __Scriptable Render Pipeline Settings__ field, add the Universal Render Pipeline Asset you created earlier. When you add the Universal Render Pipeline Asset, the available Graphics settings immediately change. This is because you've instructed Unity to use the settings specific to URP, instead of those for the Built-in render pipeline.

