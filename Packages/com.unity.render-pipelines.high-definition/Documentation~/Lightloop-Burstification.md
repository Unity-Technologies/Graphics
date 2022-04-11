# Lightloop Burstification

This technical guide explains the new changes in architecture of the lightloop introduced in HDRP v12.
The lightloop was considered a big performance bottleneck for the CPU due to its lack of parallel computation and data access patterns. The new lightloop architecture utilizes burst compiler acceleration, data access and much better performance.

## Previous Architecture

To understand exactly what has technically changed and improved, this section will explain the previous architecture at a high level.

### Step 1
The first step is frustum culling coming from the scriptable render pipeline (SRP), which converts Light game objects (GOs) to VisibleLight structs. VisibleLight struct contains a copy of the visible light, and is all performed in the engine backend, making these structs burst compatible.

### Step 2
Next, HDRP has an HDAdditionalLightData GO for additional properties not present in the regular Light component. This component is self managed, and the UI obfuscates everything, creating a nice GUI that lets HDRP have a more advanced light definition.
In this step the VisibleLight objects are taken and processed with the HDAdditioanlLightData to perform a quick rejection. This produces an intermediate list called ProcessedLightData.
The ProcessedLightData then gets sorted using a recursive quicksort (which is heavy on the CPU).

### Step 3
The last step then converts ProcessedLightData into a GPU ready struct light list. The 2nd and 3rd steps were all done linearly, in the main thread, accessing the HDAdditionalData directly. This results in performance bottlenecks because of unmanaged data accesses to the GO and recursive Quicksort call.

## Optimized Parts

### Part A
Added the concept of a render entity (HDRenderLightEntity) and a global manager for these. This basically mantains light data from HDAdditionalLightData GO as an unmanaged struct. The GO has an entity handle to this render data and is responsible to update it from the simulation side. The GO then just becomes an interface to the UI / serialization / artist. Its not used anymore by the render pipeline, with the exception of shadow processing, which will require another round of optimizations.
This effectively almost decouples the render pipeline from the GO. This allows the render pipeline then to access the HDLightRenderLightEntityData as a struct, thus unlocking burst acceleration.

### Part B
Moved the step 2 mentioned above to burst, with the exception of shadow allocation. ProcessedLightData gets accumulated using an atomic increment across all threads. This is a significant speedup on the CPU. With the exception of shadows, shadow lights still get processed patched in the main thread.

### Part C
Rewrote the sorts to be done only on uint, and on pointers for maximum speed.
Added 3 sort algorithms:
* Insertion sort for a light count <32
* Non recursive merge sort with auxiliary data for >= 32 and <= 256
* Radix sort with auxiliary data with 8 bit radix for 256 >= and above.

This is effectively almost 5x faster, and can also scale massively. The sort is still done in the main thread since there were no good gains on it being in burst.

### Part D
Move the Step 3 to burst. Keep the shadow pieces in the main thread.

## Conclusion and Future work

Improvements include:
* 50% to 70% improvement on CPU performance for lightloop.
* Better sort loops available, now in Core.
* Better decoupling of GO from render pipeline. Opens up opportunities for DOTs lights to exist.

Future work:
* Optimization of the shadow atlas allocation and matrix calculation.
* Investigation of reducing the 3 step culling algorithm to a single step.
