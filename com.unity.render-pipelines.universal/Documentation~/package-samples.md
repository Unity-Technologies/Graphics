# Package samples

The Universal Render Pipeline (URP) comes with a set of samples to help you get started.

A sample is a set of assets that you can import into your Unity project and use as a base to build upon or learn how to use a feature. A package sample can contain anything from a single C# script to multiple scenes.

## Importing package samples

Before you import any package samples for URP, be aware that they require your project to be URP-compatible. A project is URP-compatible if you [created it from a template](creating-a-new-project-with-urp.md) or manually [installed and set up URP in it](InstallURPIntoAProject.md). If the project is not URP-compatible, errors can occur when you import a package sample.

To import package samples, use the [Unity Package Manager window](https://docs.unity3d.com/Manual/upm-ui.html):

1. Go to **Window** > **Package Manager** and, in the [packages list view](https://docs.unity3d.com/Manual/upm-ui-list.html), select **Universal RP**.
2. In the package [details view](https://docs.unity3d.com/Manual/upm-ui-details.html), find the **Samples** section.
3. Find the sample you want to import and click the **Import** button next to it.

Unity imports URP package samples into `Assets/Samples/Universal RP/<package version>/<sample name>`.

## Opening package samples

To open a package sample:

1. Go to `Assets/Samples/Universal RP/<package version>/`. Here there is a folder for each URP package sample you have imported.
2. Find the folder that contains the package sample you want and open it. The folder has the same name that the package sample has in the Unity Package Manager window.

## Package samples list

The package samples that URP provides are:

* URP Package Samples: A collection of example shaders, C# scripts, and other assets you can build upon or use in your application. For more information, see [URP Package Samples](package-sample-urp-package-samples.md).
