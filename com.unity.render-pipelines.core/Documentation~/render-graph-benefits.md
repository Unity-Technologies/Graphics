# Benefits of the render graph system

## Efficient memory management

When you manage resource allocation manually, you have to account for scenarios when every rendering feature is active at the same time and thus allocate for the worst-case scenario. When particular rendering features are not active, the resources to process them are there, but the render pipeline does not use them. A render graph only allocates resources that the frame actually uses. This reduces the memory footprint of the render pipeline and means that there is no need to create complicated logic to handle resource allocation. Another benefit of efficient memory management is that, because a render graph can reuse resources efficiently, there are more resources available to create features for your render pipeline.

## Automatic synchronization point generation

Asynchronous compute queues can run in parallel to the regular graphic workload and, as a result, may reduce the overall GPU time it takes to process a render pipeline. However, it can be difficult to manually define and maintain synchronization points between an asynchronous compute queue and the regular graphics queue. A render graph automates this process and, using the high-level declaration of the render pipeline, generates correct synchronization points between the compute and graphics queue.

## Maintainability

One of the most complex issues in render pipeline maintenance is the management of resources. Because a render graph manages resources internally, it makes it much easier to maintain your render pipeline. Using the RenderGraph API, you can write efficient self-contained rendering modules that declare their input and output explicitly and are able to plug in anywhere in a render pipeline.