# Understand custom post-processing

The High Definition Render Pipeline (HDRP) allows you to write your own post-processing effects in a script. A custom post-processing effect automatically integrates into the [Volume framework](understand-volumes.md).

You can customize the order of your custom post-processing effects at each stage in the rendering process. These stages are called injection points. To learn when HDRP executes custom post-process passes, refer to [Execution order](rendering-execution-order.md)

For an example of a custom post-processing script, refer to [Custom post-processing example scripts](custom-post-processing-scripts.md).

## Known issues and limitations

- When you rename a custom post-process class name and file, HDRP removes it from the list in HDRP Project Settings which means HDRP does not render the post-processing effect.
