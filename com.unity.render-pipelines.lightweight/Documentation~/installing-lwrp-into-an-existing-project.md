**Note:** While LWRP is in preview, this documentation might not reflect the end-result 100%, and is therefore subject to change.

# Installing LWRP into an existing Project

You can download and install the latest version of LWRP to your existing Project via the [Package Manager system](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@1.8/manual/index.html). 

To install LWRP into an existing Project:

1. In Unity, open your Project. In the top navigation bar, click __Window__ > __Package__ Manager to open the __Package Manager__ window. Select the __All__ tab. This tab displays the list of available packages for the version of Unity that you are currently running.
2. Select Lightweight Render Pipeline from the list of packages. In the top right corner of the window, click __Install__. This installs LWRP directly into your Project.

**Note:** Before you can start using LWRP, you must configure it by creating a Scriptable Render Pipeline Asset and changing your Graphics settings. To learn how, see [Configuring LWRP for use](configuring-lwrp-for-use.md).

**Note:** Switching to LWRP in an existing Project consumes a lot of time and resources. LWRP uses custom lit shaders and is not compatible with the built-in Unity lit shaders. You will have to manually change or convert many elements. Instead, consider [starting a new Project with LWRP](creating-a-new-project-with-lwrp.md).

