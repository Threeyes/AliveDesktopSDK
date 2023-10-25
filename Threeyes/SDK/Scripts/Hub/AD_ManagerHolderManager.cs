using System.Collections;
using System.Collections.Generic;
using Threeyes.Steamworks;
using UnityEngine;

public class AD_ManagerHolderManager : ManagerHolderManager
{
    protected override void InitWorkshopItemInfoFactory()
    {
        SteamworksTool.RegisterManagerHolder(AD_WorkshopItemInfoFactory.Instance);
    }

    protected override List<IHubManagerModPreInitHandler> GetListManagerModPreInitOrder()
    {
        return new List<IHubManagerModPreInitHandler>()
        {
            AD_ManagerHolder.FileSystemManager,
            AD_ManagerHolder.DecorationManager
        };
    }

    protected override List<IHubManagerModInitHandler> GetListManagerModInitOrder()
    {
        //ToUpdate
        return new List<IHubManagerModInitHandler>()
        {
            AD_ManagerHolder.CommonSettingManager,

            AD_ManagerHolder.RuntimeEditorManager,//需要优先初始化SOAssetPack等数据，然后FileSystem/DecorationManager才能正常初始化
            AD_ManagerHolder.FileSystemManager,
            AD_ManagerHolder.DecorationManager,
            AD_ManagerHolder.EnvironmentManager,
            AD_ManagerHolder.PostProcessingManager,
            AD_ManagerHolder.XRManager//最后才执行传送等行为
        };
    }

}