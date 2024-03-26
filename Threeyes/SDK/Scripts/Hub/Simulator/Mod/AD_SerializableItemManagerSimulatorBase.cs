using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Threeyes.Core;
using Threeyes.Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class AD_SerializableItemManagerSimulatorBase<T, TControllerInterface, TDefaultController, TPrefabConfigInfo, TSOPrefabInfoGroup, TSOPrefabInfo, TBaseEleData> : AD_SerializableItemManagerBase<T, TControllerInterface, TDefaultController, TPrefabConfigInfo, TSOPrefabInfoGroup, TSOPrefabInfo, TBaseEleData>
    where T : HubManagerWithControllerBase<T, TControllerInterface, TDefaultController>
    where TControllerInterface : class, IAD_SerializableItemController<TBaseEleData>
    where TDefaultController : TControllerInterface
    where TPrefabConfigInfo : AD_PrefabConfigInfo<TSOPrefabInfoGroup, TSOPrefabInfo>, new()
    where TSOPrefabInfoGroup : AD_SOPrefabInfoGroupBase<TSOPrefabInfo>
    where TSOPrefabInfo : AD_SOPrefabInfo
    where TBaseEleData : class, IAD_SerializableItemInfo
{
    public override List<TPrefabConfigInfo> GetAllPrefabConfigInfo()
    {
        List<TPrefabConfigInfo> listPrefabConfigInfo = new List<TPrefabConfigInfo>();

        //#1 优先添加场景的配置文件
        TPrefabConfigInfo prefabConfigInfo_SceneMod = GetSceneModPrefabConfigInfo();
        if (prefabConfigInfo_SceneMod.HasElement)
            listPrefabConfigInfo.Add(prefabConfigInfo_SceneMod);

        //#2 添加SDK的配置文件
        listPrefabConfigInfo.Add(sdkPrefabConfigInfo);

        //ToAdd:添加模型Mod的配置文件

        return listPrefabConfigInfo;
    }
    protected abstract TPrefabConfigInfo GetSceneModPrefabConfigInfo();

    #region IHubManagerModInitHandler
    public override void OnModInit(Scene scene, ModEntry modEntry)
    {
        //设置Mod环境（以下实现仅供Simulator使用）
        modController = scene.GetComponents<TControllerInterface>().FirstOrDefault();
        //defaultController.gameObject.SetActive(modController == null);//两者互斥
        ActiveController.OnModControllerInit();//初始化

        ///ToUpdate:
        ///-子类增加类似InitControllerWithDatas.InitWithDefaultDatas的类似实现，并且能够传入测试数据:
        ///     -Shell：调用ActiveController.RefreshBase(自定义测试数据, true)
        ///     -Decoration：默认，让Modder直接拖拽到场景中，或者提供简单的添加/删除UI
        InitWithDefaultDatas();
    }
    #endregion
}