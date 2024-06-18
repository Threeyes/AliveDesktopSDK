using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AD_DecorationManagerSimulator : AD_SerializableItemManagerSimulatorBase<AD_DecorationManagerSimulator, IAD_DecorationController, AD_DefaultDecorationController, AD_DecorationPrefabConfigInfo, AD_DecorationPrefabInfoCategory, AD_SODecorationPrefabInfoGroup, AD_SODecorationPrefabInfo, AD_DecorationItemInfo>
    , IAD_DecorationManager
{
    #region Interface
    public void DeleteElement(IAD_DecorationItem item)
    {
        if (ActiveController == null) return;

        //ToAdd:增加Undo

        ActiveController.DeleteElement(item);//由父物体负责删除，并移除相关引用
    }
    #endregion

    protected override void InitWithDefaultDatas()
    {
        //尝试查找已有的预设子物体并进行初始化
        ActiveController.RelinkElemets();
        ActiveController.InitExistElements();
    }

    protected override AD_DecorationPrefabConfigInfo GetSceneModPrefabConfigInfo()
    {
        return AD_ManagerHolder.SceneManager.DecorationPrefabConfigInfo;
    }
}