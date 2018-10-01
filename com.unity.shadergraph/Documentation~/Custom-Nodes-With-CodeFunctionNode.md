# Custom Nodes

With the recent release of [Shader Graph](Shader-Graph.md) it is now easier than ever to create custom shaders in Unity. However, regardless of how many different [Nodes](Node.md) we offer by default, we can’t possibly cater for everything you might want to make. For this reason we have developed a [Custom Node API](Scripting-API.md) for you to use to make new [Nodes](Node.md) in C#. This allows you to extend [Shader Graph](Shader-Graph.md) to suit your needs.

In this article we will take a look at one of the ways you can accomplish this. It is the simplest way to create custom [Nodes](Node.md) that create shader functions. We call it the [Code Function Node](CodeFunctionNode.md). Let’s take a look at how to create a new [Node](Node.md) using this method.

Lets start by creating a new C# script. For this example I have named the script `MyCustomNode`. To use the [Code Function Node API](CodeFunctionNode.md) you need to include (or add the class to.md) the namespace `UnityEditor.ShaderGraph` and inherit from the base class [CodeFunctionNode](CodeFunctionNode.md).

![01](images\Custom-Nodes-With-CodeFunctionNode01.png)

The first thing you will notice is that `MyCustomNode` is highlighted with an error. If we hover over the message we see that we need to implement an inherited member called [GetFunctionToConvert](CodeFunctionNode.GetFunctionToConvert.md). The base class [CodeFunctionNode](CodeFunctionNode.md) handles most of the work that needs to be done to tell the [Shader Graph](Shader-Graph.md) how to process this [Node](Node.md) but we still need to tell it what the resulting function should be, this is how we will do this.

The method [GetFunctionToConvert](CodeFunctionNode.GetFunctionToConvert.md) uses **Reflection** to convert another method into an instance of **MethodInfo** that [CodeFunctionNode](CodeFunctionNode.md) can convert for use in [Shader Graph](Shader-Graph.md). This simply allows us to write the shader function we want in a more intuitive way.

For more information on Reflection see: [Reflection (C#.md)](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/reflection.md)

Add the namespace `System.Reflection` and the override function [GetFunctionToConvert](CodeFunctionNode.GetFunctionToConvert.md) as shown in the image below. Note the string that reads `MyCustomFunction`, this will be the name of the function that is written into the final shader. This can be named whatever you wish to suit the function you are writing and can be anything that doesn’t begin with a numeric character, but for the rest of this article we will assume its name is `MyCustomFunction`.

![02](Images/Custom-Nodes-With-CodeFunctionNode02.png)

Now our script errors are resolved we can start working on the functionality of our new [Node](Node.md)! Before we continue we should name it. To do this add a public constructor for the class with no arguments. In it, set the variable name to a string that contains the title of your [Node](Node.md). This will be displayed in the title bar of the [Node](Node.md) when it appears in a graph. For this example we will call the [Node](Node.md) `My Custom Node`.

![03](Images/Custom-Nodes-With-CodeFunctionNode03.png)

Next we will define the [Node’s](Node.md) function itself. If you are familiar with **Reflection** you will notice that the method [GetFunctionToConvert](CodeFunctionNode.GetFunctionToConvert.md) is attempting to access a method in this class called `MyCustomFunction`. This is the method that will define the shader function itself.

Lets create a new static method of return type **string** with the same name as the string in the method [GetFunctionToConvert](CodeFunctionNode.GetFunctionToConvert.md). In the case of this tutorial that will be `MyCustomFunction`. In the arguments of this method we can define what [Ports](Port.md) we want the [Node](Node.md) to have, these will map directly to the arguments in the final shader function. We do this by adding an argument of a type supported in [Shader Graph](Shader-Graph.md) with a [Slot Attribute](CodeFunctionNode.SlotAttribute.md). For now lets add two argument of type **DynamicDimensionVector** called `A` and `B` and another out argument of type **DynamicDimensionVector** called `Out`. Then we will add a default [Slot Attribute](CodeFunctionNode.SlotAttribute.md) to each of these arguments. Each [Slot Attribute](CodeFunctionNode.SlotAttribute.md) needs a unique index and a [Binding](CodeFunctionNode.Binding.md), which we will set to **None**.

![04](Images/GettingStarted/Custom-Nodes-With-CodeFunctionNode04.png)

For a full list of [Types](CodeFunctionNode-Port-Types.md) and [Bindings](CodeFunctionNode.Binding.md) that are supported see the [CodeFunctionNode](CodeFunctionNode.md) API documentation.

In this method we will define the contents of the shader function in the return string. This needs to contain the braces of the shader function and the HLSL code we wish to include. For this example lets define `Out = A + B;`. The method we just created should look like this:

![05](Images/Custom-Nodes-With-CodeFunctionNode05.png)

This is exactly the same C# code that is used in the [Add Node](Add-Node.md) that comes with [Shader Graph](Shader-Graph.md).

There is one last thing we need to do before we have a working [Node](Node.md). That is tell it where to appear in the [Create Node Menu](Create-Node-Menu.md). We do this by adding the Title **Attribute** above the class. In this we define an string array that describes where it should appear in the hierarchy in the menu. The last string in this array defines what the [Node](Node.md) should be called in the [Create Node Menu](Create-Node-Menu.md). For this example we will call the [Node](Node.md) `My Custom Node` and place it in the folder `Custom`.

![06](Images/Custom-Nodes-With-CodeFunctionNode06.png)

Now we have a working [Node](Node.md)! If we return to Unity, let the script compile then open [Shader Graph](Shader-Graph.md) we will see the new [Node](Node.md) in the [Create Node Menu](Create-Node-Menu.md). 

![07](Images/Custom-Nodes-With-CodeFunctionNode07.png)

Create an instance of the [Node](Node.md) in the [Shader Graph](Shader-Graph.md). You will see it has the [Ports](Port.md) we defined with the same names and [Types](Data-Types.md) as the arguments to the `MyCustomFunction` class.

![08](Images/Custom-Nodes-With-CodeFunctionNode08.png)

Now we can create all kinds of different [Nodes](Node.md) by using different [Port types](Data-Types.md) and [Bindings](Port-Bindings.md). The return string of the method can contain any valid HLSL in a regular Unity shader. Here is a [Node](Node.md) that returns the smallest of the three input values:

![09](Images/Custom-Nodes-With-CodeFunctionNode09.png)

And here is a [Node](Node.md) that inverts normals based on a **Boolean** input. Note in this example how the [Port](Port.md) `Normal` has a [Binding](Port-Bindings.md) for **WorldSpaceNormal**. When there is no [Edge](Edge.md) connected to this [Port](Node.md) it will use the mesh’s world space normal vector by default. For more information see the [Port Binding](Port-Bindings.md) documentation. Also note how when using a concrete output type like **Vector 3** we have to define it before we return the shader function.

![10](Images/Custom-Nodes-With-CodeFunctionNode10.png)

Now you are ready to try making [Nodes](Node.md) in [Shader Graph](Shader-Graph.md) using [Code Function Node](CodeFunctionNode.md)! But this is, of course, just the beginning. There is much more you can do in [Shader Graph](Shader-Graph.md) to customize the system. For more information see the rest of this documentation and the [Scripting API](Scripting-API.md).
