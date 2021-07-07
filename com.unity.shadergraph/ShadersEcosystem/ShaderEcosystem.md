# ShaderEcoSystem

This is a rough draft for the api and questions for the new shader ecosystem based upon the idea of blocks, templates, and template linkers. This is primarily for 1.0 (2021.2) where templates will be created from legacy targets. We'd like the template and block api to be solid for 2.0 (block based linkage) and just require the SRPs to be refactored.

Table of Contents:
- [Basic Types](#basictypes)
- [Blocks](#blocks)
- [Template](#template)
- [Template Provider](#template-provider)
- [Template Linker](#template-linker)
- [Shader Generator](#shader-generator)
    - [Active Fields](#active-fields)
- [Block Linker](#block-linker)
- [Surface Shaders](#surface-shaders)
- [Notes](#long-term-notes)


---
## Basic Types

Here's a high level description of some simple types used in the rest of this document. This is not indicative of the final api for interfacing with these types, but more of the data that's available. A lot of this is based upon the current Shader Sandbox prototype. Many questions exist still.

```
// Simple helper to represent having two names on most everything
class ShaderName
{
    string displayName;
    string referenceName;
}
```

```
// Variables can be represented all over the place
class ShaderVariable
{
    ShaderType shaderType;
    ShaderName name;
    // Do we need some high level attributes / metadata too?
}
```
```
// Variables can be represented all over the place
class ShaderType
{
    ShaderName name;
    List<ShaderVariable> fields;
    // This is very high level for simplicity, the sandbox has a more robust version of this
    BaseType {Scalar, Vector, Matrix, Struct, ...}
}
```
```
// A parameters need to know the in/out mode
class ShaderParameter : ShaderVariable
{
    ParameterFlags flags {None, Input, Output}
}
```
```
class ShaderFunction
{
    ShaderName name;
    List<ShaderParameter> parameters;
    ShaderType returnType;
    // Probably also includes:
    Includes, body, dependencies (functions called)
}
```
```
// A property is a variable with a bit more information, mainly about how it's exposed
class ShaderProperty : ShaderVariable
{
    PropertyType flags{None, Input, Output, Property, Required}
    Attributes?
}
```
Note: Properties are actually more complicated due to handling block inputs, material properties, and shader properties, all of which can be declared very differently.

Finer details will be fleshed out [here](./ShaderCore.md)

---
## Blocks:

A block is a collection of functions, types, properties, etc... that is meant to be re-used and composited in a pass with other blocks.
High level Api:

```
class EntryPointDescriptor
{
    // These could be determined by inspection from the function
    List<ShaderProperty> properties; // Can we determine varyings from these?
    string functionName;
}
class BlockDescriptor
{
    ShaderName shaderName;
    List<ShaderType> types;
    List<ShaderFunction> functions;
    // Needed long-term to denote how a block actually gets linked
    // (e.g. what entry point function for the code gen to call)
    EntryPointDescriptor entryPoint;
     // Can be null to mean this can't be overridden. Must be unique within a pass.
    string customizationPointName = null;

    List<ShaderInclude> includes;
    List<ShaderDefine> defines;
    List<ShaderKeyword> keywords; // Variants
    List<ShaderPragma> pragmas;

    // For version 2.0
    // Blocks will have some way long term to mark stage requirements
    StageRequirements {None, Vertex, Fragment, ...}
}
```
Each block has a bunch of types, functions, defines, etc...

Each block has only one entry point. We may have more than one entry point in the text format, but I propose that this is internally split into multiple blocks as this makes the internals much cleaner.

The entry point properties is just high-level information about the entry point function, in particular the inputs, outputs, etc... from the function. This could all be in theory inspected from the function depending on what that syntax looks like. In particular, we need to be able to represent all of the ideas needed for a block (inputs, outputs, properties, varyings, etc...) and not all of these may make sense in the function signature.

The customization point name is used for interface matching when linking.

Blocks might have more information but it might not be needed in the runtime:
- overrides
- required blocks
- package requirements
- block type (hlsl, glsl)?

----------------------
Blocks should ideally be only created once and shared multiple times. This means blocks can be registered somewhere and re-used for all shaders. Something very high level like:
```
class BlockRegistry
{
    internal Dictionary<string, BlockDescriptor> registeredBlocks;

    public void AddBlock(BlockDescriptor block);
    public IEnumerable<BlockDescriptor> GetBlocks();
    public BlockDescriptor FindBlock(string blockName);
}
```
Blocks may need some form of namespacing to deal with name collisions.

---
## Template

The template represents one sub-shader in shader lab. It contains a collection of blocks, some of which are exposed for customization

First some helpers:
```
// Just a way to group some current common data that exists both at the pass and template level. Not needed in 1.0 but will be needed in 2.0.
class OptionalCommand
{
    List<Tag> tags;
    List<Command> commands; // RenderState
    // Use/Grab pass?
}
```
```
// Each pass is a collection of blocks
class PassDescriptor
{
    ShaderName name;
    
    // Blocks need to have some way to know what stage they're in so that
    // the long-term block linking can work correctly (e.g. variant generation).
    // Block descriptors are meant to be instantiated only once so this 
    // information can't be put in the block (since a block could go in multiple 
    // stages if stage agnostic). This could maybe be changed to some higher level 
    // descriptor object (e.g. rename BlockDescriptor to Block and make a 
    // descriptor just contain this info). Ideas are left up for debate
    Dictionary<ShaderStage, List<BlockDescriptor>> blocksPerStage;
    // The above could be explicitly laid out, a list, etc...
    // just some way to denote we have blocks per stage.

    // Open question, will tessellation or other stages cause issues with blocks?

    // -------- Needed in 2.0
    List<OptionalCommand> optionalCommands;
}
```

```
class CustomizationPoint
{
    // Maps to the block descriptor's customization point name
    string customizationPointName;
    List<ShaderProperty> properties;
}
```

```
// Template matches to a SubShader
class Template
{
    ShaderName name;
    List<PassDescriptor> passDescriptors;

    // All entry points for consideration.
    // This is used for reflection so SG can inspect what's expected
    IEnumerable<CustomizationPoint> customizationPoints;

    // The linker to use for this template. See the linker section for more details.
    // This will be filled out by the linker provider.
    ITemplateLinker linker;

    // -------- Needed in 2.0

    // Currently some shaderlab commands exist at the subshader level (tags, etc...).
    // This may be something that can be moved around
    List<OptionalCommand> optionalCommands;
}
```

Each template has all of the passes and blocks used to make it up.

Customization points are explicitly laid out here to signify what's expected within a template. This information is nice for SG to know what the expected API is for a customization point. These customization points could be dynamically constructed by inspecting the blocks within all passes and generating a union of properties.

See the [template provider](#template-provider) section for more details about the template linker.

-------
Open Questions:
- Do templates have some CustomizationPoint assembled at the top level for easy inspection, marking which entry points can be overridden, etc... or is this dynamically assembled?

---
## Template Provider

Some api is needed to build templates. This will need some api for dynamically configuring the templates based on some simple parameters.

```
interface ITemplateProvider
{
    // Some method is needed arbitrary settings per target.
    // This is not clean but may be what's required due to SRP divergences
    virtual void ConfigureSetting(string name, string value);

    virtual IEnumerable<Template> GetTemplates();
}
```

A provider can dynamically build templates however it wants based upon it's configuration. For instance, URP lit does not include the DepthOnly pass if zWriteControl is Disabled.

Some more info about configurations can be found [here](TemplateProvider.md). The easiest interface to configure everything across all SRPs is to generically push string name/values (such as "AlphaClipping", "true"). The exact interface here is up for debate.

Long term we want to have templates built more explicitly, but in the short term we'd have a way to build templates from legacy targets. This will likely be having the existing targets implement the `ITemplateProvider` interface.

This information extracted in the short term only needs to contain enough information for reflection, not for full code gen. The provider will need to also provide the template with enough information for it to link later, this includes marking what linker to use.

--- 
### Legacy Block Configuration
In the short term, the provider could choose to only provide the configuration points, but it might make sense to actually configure three blocks per shader stage as well:
- Pre Configuration Point
- Configuration Point
- Post Configuration Point

This will reflect what 2.0 will likely be split into in the SRPs and may give a little extra information (expected outputs, available inputs, etc...) that tools like shader graph can use. In particular, this is useful to shader graph currently due to how it filters [active fields](##active-fields). The old system should not be visible at all to shader graph, but instead it can inspect the outputs of the pre block and the inputs of the post block and use that to filter the same.

---
## Template Linker

The template linker is responsible for building a shader from a template and a collection of blocks. The simple api looks something like:
```
// Used to handle the input/output name remappings in the template
class BlockLinkOverride
{
    string sourceName;
    string destName;
}
```
```
class BlockLink
{
    string customizationPointName
    BlockDescriptor blockToLink;

    // Need some way to specify which passes to use this in.
    // Could either be a single pass (and duplicate entries for multiple passes) or a list of passes or some flags
    string passName;

    // This is used to handle how names are remapped within the blocks.
    // This is not 100% needed for the first pass, but is very useful. In particular because the
    // input/output names already don't match (e.g. ObjectSpacePosition -> Position).
    internal List<BlockLinkOverride> inputOverrides;
    internal List<BlockLinkOverride> outputOverrides;
}
```
```
interface ITemplateLinker
{
    virtual void Link(ShaderBuilder builder, Template template, List<BlockLink> blockLinks);
}
```
Note: this constructs a sub-shader, not a shader. This could be changed if we want to take a list of templates.

The linker is also a virtual api and how we currently will generically handle switching between legacy targets and the eventual new block based SRP templates. In particular, to start we'd have a legacy linker:

```
// Legacy linker that knows how to handle old targets, splice points, etc...
class LegacyTemplateLinker : ITemplateLinker
{
    // Somehow the linker will need to know about the target that was used to create the template.
    // This can be uniquely constructed by each IProvider if need be.
    // As targets can dynamically construct subshaders, this may need to be the generated sub-shader descriptor
    LegacyTemplateLinker(SubShaderDescriptor legacySubShaderDescriptor)

    override void Link(ShaderBuilder builder, Template template, List<BlockLink> blockLinks);
}
```

Some mechanism will be needed to connect the provider and linker together so legacy information can be built. In particular the linkier will need some way to grab information only needed for the legacy pathway such as the splice template file paths.

The legacy linker will need to always generate a wrapper function around the user's actual function. This is important in particular for surface shaders and for the input/output name remappings. This needs to be done inside the linker because:
- the generated block shouldn't change between linkers (legacy/new)
- how default fields (required fields) are handled to enable pass-through
- the special input/output type structs required by legacy
- this may also allow us to hide or fix some names now if we want.

As an additional note, ShaderGraph may have to change it's generation right now due to how it generates the surface functions since it generates a signature that matches the overload that the linker will want to create.

----
Open Questions:
- How are Templates/Providers/Linkers connected? The current proposal is to have the provider set linker data on the template
- How are blocks merged? E.g. if there's conflicting keywords, pragmas, etc... how are they merged? Last wins?
- How are duplicate types handled? Are they deduplicated based on name? Are names shadowed? It's expected several blocks would share the same types (e.g. varyings are shared between vertex/pixel).
- A lot of structs in a target use generic precision qualifiers, the precision needs to be baked out at some point (in the linking or in the generation step)
- How are inputs/outputs determined/matched for entry points? Automatically from - blocks? From top level descriptors?
- Properties need extra metadata about how they're declared (not declared, global, per material, hybrid)

Answered Questions:
- How are varyings built? These will likely need to be hard-coded into the legacy linker in the same way they are now
- [Active fields](#active-fields) are fairly complicated and convoluted.
- Field/keyword/etc... dependencies. Can we bake these out or at least hide them inside the linker. We currently do a lot of stuff like add defines if a field is used e.g. using world space position defines VARYINGS_NEED_POSITION_WS

---
## Shader Generator

Linking all of templates together will produce a bunch of sub-shaders, but some common class needs to exist to create the actual shader from the sub-shaders. In particular, this needs to declare the properties section. To do this the merged list of properties across all templates needs to be generated. This should be doable without any new api, but from only inspecting the data in the templates and block links.

High level this needs to look something like:
```
internal class TemplateDescription
{
    public Template template;
    public List<BlockLink> blockLinks = new List<BlockLink>();
}

internal class ShaderDescription
{
    public string name;
    public List<TemplateDescription> templateDescriptions = new List<TemplateDescription>();
    public string fallbackShader = @"FallBack ""Hidden/Shader Graph/FallbackError""";
}

internal class ShaderGenerator
{
    internal void Generate(ShaderStringBuilder builder, SandboxShaderDescription shaderDesc);
}
```
Basically each shader is constructed of a set of templates plus blocks. Blocks know what passes they operate on so that can be filtered internally. The generator can then declare the top-level shader structure and the merged material properties by inspecting all of the blocks in the templates and all of the blocks in the block links.

The name and structure of these classes is subject to change.

---
### Active Fields

Active fields are very confusing and are used to prune a lot of stuff.
Currently:
- Per pass shader graph knows what outputs blocks are used.
- These outputs blocks are used to get active fields from targets.
- Shader graph uses active blocks to get nodes and extract active fields (e.g. this might determine world position is needed)
- Active fields are used to filter structs, defines, etc... (e.g. surface inputs)

The above is an over simplification as there's some intricate ordering details that make this extra confusing.

For 1.0 we'd like to entirely remove this from the API. Shader graph can track its own fields. Internally the legacy linker can use its old active fields and reconstruct the active fields from the blocks. It's currently hard to determine if there will be issues with this approach

See [active fields](ActiveFields.md) for a deeper dive.

After some experimenting it should be possible to hide active fields by generating blocks with the necessary data from the target. In particular, if each shader stage is split into 3 parts: pre/main/post then shader graph can inspect the outputs of the pre block and the inputs of the post block and use this same info to do field culling.

The legacy linker should be able to take the shader graph blocks and knowing if it's vertex/pixel and input/output re-map the name to an actual active field and do everything identically on the backend.


---
## Block Linker

The long-term block linker should be very simple to define as it just iterates the blocks and dynamically builds up the connections between blocks, stages, etc...
Some things can be collected and grouped at the top (e.g. keywords) but some can bracket a block (defines, includes, etc...).

Should a block undef any defines when it's done so you don't have state bleeding?

---
## Surface Shaders

Surface shaders need some way to interact with this whole system. High level they need to be able to:
- look up a template providers (basically sub-shader names)
- customize each provider via some parameters to generate unique templates
- get the correct linker for each template
- link the user's blocks to each template

Something high-level like
```
class ParsedTemplateDescription
{
    string templateIdentifier;
    List<Tuple<string, string>> configurationParams;
    List<BlockLink> blocksToLink;
}
class ParsedSurfaceShader
{
    List<BlockDescription> blocks;
    List<TemplateDescriptions> templateDescriptions
}
void BuildSurfaceShader(string file)
{
    var parsedSurfaceShader = Parse(file);
    var builder = new ShaderBuilder();
    var shaderDesc = new ShaderDescription();
    foreach(var parsedTemplateDesc in parsedSurfaceShader.templateDescriptions)
    {
        var templateProvider = FindTemplateProvider(parsedTemplateDesc.templateIdentifier);
        foreach(var param in parsedTemplateDesc.configurationParams)
            templateProvider.ConfigureSettings(param.value0, param.value1);

        var templates = templateProvider.GetTemplates();
        foreach(var template in templates)
        {
            var templateDesc = new SandboxTemplateDescription();
            templateDesc.template = template;
            // Build the parsed links into actual links. This might need to reorg some data, add pass specifications, etc...
            templateDesc.blockLinks.AddRange(BuildBlocks(parsedTemplateDesc.blocksToLink));
            shaderDesc.templateDescriptions.Add(templateDesc);
        }
    }

    var shaderBuilder = new ShaderStringBuilder();
    var generator = new ShaderGenerator();
    generator.Generate(shaderBuilder, shaderDesc);
    var shaderCode = shaderBuilder.ToCode();
}
```

---
## Long-term notes


Actual api likely needs to be safe and readonly, might require using a builder pattern. E.g.:
```
public class ShaderFunction
{
    private ShaderFunction();
    //readonly params, etc...

    public class Builder()
    {
        public Builder();
        public ShaderFunction Build();
    }
}
var builder = new ShaderFunction.Builder();
// add name, params, etc... on builder
var function = builder.Build();
```



