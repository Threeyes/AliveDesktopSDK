using System.Collections;
using System.Collections.Generic;
using Threeyes.Steamworks;
using UnityEngine;

public interface IAD_ShellManager :
    IAD_SerializableItemManager<AD_ShellPrefabConfigInfo, AD_ShellPrefabInfoCategory, AD_SOShellPrefabInfoGroup, AD_SOShellPrefabInfo>,
    IHubManagerWithController<IAD_ShellController>,
    IHubManagerModPreInitHandler,
    IHubManagerModInitHandler
{
}
