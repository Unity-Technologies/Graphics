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
                if (DebugActionManager.instance.GetAction(DebugActionManager.DebugAction.PreviousDebugPanel) != 0.0f)
                {
                    DebugMenuManager.instance.PreviousDebugPanel();
                }

                if (DebugActionManager.instance.GetAction(DebugActionManager.DebugAction.NextDebugPanel) != 0.0f)
                {
                    DebugMenuManager.instance.NextDebugPanel();
                }

                if (DebugActionManager.instance.GetAction(DebugActionManager.DebugAction.Validate) != 0.0f)
                {
                    DebugMenuManager.instance.OnValidate();
                }

                if (DebugActionManager.instance.GetAction(DebugActionManager.DebugAction.MakePersistent) != 0.0f)
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
