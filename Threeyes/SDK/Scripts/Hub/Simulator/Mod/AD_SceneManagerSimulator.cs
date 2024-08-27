using System.Collections.Generic;
using Threeyes.RuntimeSerialization;
using Threeyes.GameFramework;
public class AD_SceneManagerSimulator : SceneManagerSimulator
    , IAD_SceneManager
{
    #region IAD_SceneManager
    public AD_ShellPrefabConfigInfo ShellPrefabConfigInfo
    {
        get { return new AD_ShellPrefabConfigInfo(new AD_ShellPrefabInfoCategory(curWorkshopItemInfo.title, curListShellGroup)); }
    }
    public AD_DecorationPrefabConfigInfo DecorationPrefabConfigInfo
    {
        get { return new AD_DecorationPrefabConfigInfo(new AD_DecorationPrefabInfoCategory(curWorkshopItemInfo.title, curListDecorationGroup)); }
    }
    #endregion

    ///Todo:
    ///-参考AD_SceneManager.InitModFunc，在加载场景时需要扫描读取对应的WorkshopItemInfo，并通过WorkshopItemTool.LoadAsset_UnityProject来读取对应的资源
    SOAssetPack curSOAssetPack;
    SOAssetPackInfo curSOAssetPackInfo;
    public List<AD_SOShellPrefabInfoGroup> curListShellGroup = new List<AD_SOShellPrefabInfoGroup>();
    public List<AD_SODecorationPrefabInfoGroup> curListDecorationGroup = new List<AD_SODecorationPrefabInfoGroup>();
    protected override void InitMod(ModEntry modEntry)
    {
        curSOAssetPack = WorkshopItemTool.LoadAsset_UnityProject<SOAssetPack>(curWorkshopItemInfo);//尝试加载当前的soAssetPack
        if (curSOAssetPack)
        {
            curSOAssetPackInfo = new SOAssetPackInfo(curWorkshopItemInfo.ModFileUID, curSOAssetPack);
            SOAssetPackManager.Add(curSOAssetPackInfo);//添加到全局中
        }

        curListShellGroup = WorkshopItemTool.LoadAssets_UnityProject<AD_SOShellPrefabInfoGroup>(curWorkshopItemInfo);
        curListDecorationGroup = WorkshopItemTool.LoadAssets_UnityProject<AD_SODecorationPrefabInfoGroup>(curWorkshopItemInfo);

        base.InitMod(modEntry);
    }
}