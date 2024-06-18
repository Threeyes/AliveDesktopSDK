using System.Collections;
using System.Collections.Generic;
using Threeyes.Steamworks;
using UnityEngine;

public interface IAD_DecorationManager :
    IAD_SerializableItemManager<AD_DecorationPrefabConfigInfo, AD_DecorationPrefabInfoCategory, AD_SODecorationPrefabInfoGroup, AD_SODecorationPrefabInfo>,
    IHubManagerWithController<IAD_DecorationController>,
    IHubManagerModPreInitHandler,
    IHubManagerModInitHandler
{
    void DeleteElement(IAD_DecorationItem item);
}
