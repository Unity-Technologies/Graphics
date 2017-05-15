using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering
{
    [ExecuteInEditMode]
    public class DebugMenuUpdater : MonoBehaviour
    {

        void Update()
        {
            DebugMenuManager.instance.Update();
            DebugActionManager.instance.Update();

            if (DebugActionManager.instance.GetAction(DebugActionManager.DebugAction.EnableDebugMenu) != 0.0f)
            {
                DebugMenuManager.instance.ToggleMenu();
            }

            if (DebugMenuManager.instance.isEnabled)
            {
                if (DebugActionManager.instance.GetAction(DebugActionManager.DebugAction.PreviousDebugMenu) != 0.0f)
                {
                    DebugMenuManager.instance.PreviousDebugMenu();
                }

                if (DebugActionManager.instance.GetAction(DebugActionManager.DebugAction.NextDebugMenu) != 0.0f)
                {
                    DebugMenuManager.instance.NextDebugMenu();
                }

                if (DebugActionManager.instance.GetAction(DebugActionManager.DebugAction.Validate) != 0.0f)
                {
                    DebugMenuManager.instance.OnValidate();
                }

                if (DebugActionManager.instance.GetAction(DebugActionManager.DebugAction.Persistent) != 0.0f)
                {
                    DebugMenuManager.instance.OnMakePersistent();
                }

                float moveHorizontal = DebugActionManager.instance.GetAction(DebugActionManager.DebugAction.MoveHorizontal);
                if (moveHorizontal != 0.0f)
                {
                    DebugMenuManager.instance.OnMoveHorizontal(moveHorizontal);
                }

                float moveVertical = DebugActionManager.instance.GetAction(DebugActionManager.DebugAction.MoveVertical);
                if (moveVertical != 0.0f)
                {
                    DebugMenuManager.instance.OnMoveVertical(moveVertical);
                }
            }
        }
    }
}
