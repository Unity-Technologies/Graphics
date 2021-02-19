using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine;

public class OverrideDrsFitlerCustomPass : CustomPass
{
    //protected override void Execute(CustomPassContext ctx)
    protected override void OnRenderEvent(CustomPassRenderEventContext ctx)
    {
        if (ctx.eventType == CustomPassRenderEventContext.EventType.OnExecute)
        {
            var cameraDrsFilterComponent = ctx.hdCamera.camera.GetComponentInParent<SetCameraDrsFilter>();
            if (cameraDrsFilterComponent != null)
            {
                DynamicResolutionHandler.instance.SetCurrentCameraRequest(cameraDrsFilterComponent.EnableDrs);
                DynamicResolutionHandler.instance.filter = cameraDrsFilterComponent.DrsFilter;
            }
        }
    }
}
