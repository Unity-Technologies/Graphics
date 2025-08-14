# Manage keywords in Shader Graph

Get started with adding and managing keywords in a Shader Graph.

## Add a keyword in a Shader Graph

To add a keyword in a Shader Graph:

1. Add a keyword in the [Blackboard](Blackboard.md) and define the [keyword parameters](Keywords-reference.md) according to your needs.

1. Add a [Keyword Node](Keyword-Node.md) in the graph from the keyword you defined in the Blackboard.

Unity [declares the keyword](https://docs.unity3d.com/Manual/SL-MultipleProgramVariants-declare.html) in the final shader code.

## Make the shader behavior conditional on the keyword

To make the shader behavior conditional on the keyword in the Shader Graph:

1. Create the upstream Shader Graph branches that define the various behaviors you want to use conditionally. 

1. Connect each branch to a different input port of the Keyword Node according to the keyword value you want to use for later toggling.

1. Connect the output port of the Keyword Node to the node port you want to apply the conditional graph part to.

Unity [adds all the conditions and branches](https://docs.unity3d.com/Manual/SL-MultipleProgramVariants-make-conditionals.html) in the final shader code.

## Toggle the shader keyword in the Editor

To be able to control a keyword from the Material Inspector, make sure to enable **Generate Material Property** in the [keyword parameters](Keywords-reference.md) in the graph.

## Toggle the shader keyword in a script

To enable a Boolean keyword from a script, use `EnableKeyword` on the keyword's **Reference Name**. `DisableKeyword` disables the keyword. To learn more about Boolean keywords, refer to [Shader variants and keywords](https://docs.unity3d.com/Manual/SL-MultipleProgramVariants.html).

When controlling an Enum keyword via script with a `Material.EnableKeyword` or `Shader.EnableKeyword` function, enter the state label in the format `{REFERENCE}_{REFERENCESUFFIX}`. For example, if your reference name is MYENUM and the desired entry is OPTION1, then you would call `Material.EnableKeyword("MYENUM_OPTION1")`. When you select an option, you must also disable the other options to see the effect.

**Note:** By default, when you add a keyword, Unity adds an underscore to the start of **Reference Name**. As a result, a keyword with the name **MYENUM** has the reference name **_MYENUM**.

## Additional resources

* [Introduction to keywords in Shader Graph](Keywords-concepts.md)
* [Keyword parameter reference](Keywords-reference.md)
