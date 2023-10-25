using System.Collections;
using System.Collections.Generic;
using Threeyes.RuntimeEditor;
using Threeyes.Steamworks;
using UnityEngine;
public interface IAD_RuntimeEditorManager : IRuntimeEditorManager, IHubManagerModInitHandler
{
}
public interface IAD_RuntimeEditor_ModeActiveHandler
{
    /// <summary>
    /// Enter/Exit RuntimeEditor Mode
    /// </summary>
    /// <param name="isActive"></param>
    void OnRuntimeEditorActiveChanged(bool isActive);
}