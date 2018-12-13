#Visual Effect Graph

Welcome to the documentation page of the Visual Effect Graph. Here you will be able to access a variety of topics to be start working with the new Unity Visual Effect toolchain.

![](Images/vfxeditor-title.png)

> ### Disclaimer
>
> This documentation refers to features that available as *preview* and that are under continuous development. This section is a early, incomplete, work-in progress and, as such, contents and features described here are highly likely subject to change.

### Requirements

The visual effects editor comes along with the HD ScriptableRenderPipeline  and ShaderGraph, it is supported starting 2018.3. It requires the following:

* Unity 2018.3 or newer
* Compute Shader and HD Render Pipeline support for your build target
* Windows, Linux or Mac editor
* HD Render PIpeline configured project with package version 4.3.0 or newer

## Install and Configure your project

Make sure your project is already configured with HD Render Pipeline before proceeding, and upgrade your HD Render Pipeline version to 4.3.0-preview or newer.

* To install Visual Effect Graph into your project you can use the Package Manager UI to add the package by selecting the "All Packages" View. 

* As this package resides in the preview packages you can display it by using the "Show Preview Packages" option in the advanced dropdown.

* Then navigate to the Visual Effect Graph, and install the same version as your HD Render Pipeline package.

![](Images/install-vfx-package.gif)

> Note : *If your HD Render Pipeline package is too old (for instance 3.3.0) you will need to upgrade it to a newer version, for instance 4.3.0.*

> Note : *The packages meant to be used with 2018.3 are all under the 4.x version track. Packages under the 5.x version track are meant to be used with 2019.x*

## Getting Started

To get started right away with Visual Effect Graph, you can visit [this page](Visual-Effect-Graph-Getting-Started) that sums up most of the features you need to know about.

## Documentation

This section covers the different aspects of the Visual Effects, its philosophy and the main concepts you will encounter while working with the graph, and the game objects.

* [Assets and Game Objects](Visual-Effect-Assets-and-GameObjects)
* [The Visual Effect Graph Window](The-Visual-Effect-Graph-Window)
* [Systems, Contexts and Blocks](Systems-Contexts-and-Blocks)
* [Attributes, Properties and Settings](Attributes-Properties-and-Settings)
* [Parameters and Events](Parameters-and-Events)

## API and Feature Reference

* [Context Reference](Contexts)
* [Block Library Reference](Blocks)
* [Operators reference](Operators)
* [C# Component API](CSharp-API)

