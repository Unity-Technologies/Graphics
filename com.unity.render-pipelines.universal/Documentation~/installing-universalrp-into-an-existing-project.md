# Installing Universal RP into an existing Project

You can download and install the latest version of Universal RP to your existing Project via the [Package Manager system](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@1.8/manual/index.html). 

To install Universal RP into an existing Project:

1. In Unity, open your Project. In the top navigation bar, click __Window__ > __Package__ Manager to open the __Package Manager__ window. Select the __All__ tab. This tab displays the list of available packages for the version of Unity that you are currently running.
2. Select Universal Render Pipeline from the list of packages. In the top right corner of the window, click __Install__. This installs Universal RP directly into your Project.

Before you can start using Universal RP, you must configure it by creating a Scriptable Render Pipeline Asset and changing your Graphics settings. To learn how, see [Configuring Universal RP for use](configuring-universalrp-for-use.md).

**Note:** Switching to Universal RP in an existing Project consumes a lot of time and resources. Universal RP uses custom lit shaders and is not compatible with the Built-in Unity lit shaders. You will have to manually change or convert many elements. Instead, consider [starting a new Project with Universal RP](creating-a-new-project-with-universalrp.md).

**Note:** The Universal Render Pipeline uses its own [post-processing](integration-with-post-processing.md). If you have the post-processing version 2 package installed in your Project already, you need to delete it before you upgrade.