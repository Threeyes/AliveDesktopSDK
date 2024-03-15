using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AD_DecorationManagerSimulator : AD_SerializableItemManagerSimulatorBase<AD_DecorationManagerSimulator, IAD_DecorationController, AD_DefaultDecorationController, AD_DecorationPrefabConfigInfo, AD_SODecorationPrefabInfoGroup, AD_SODecorationPrefabInfo, AD_DecorationItemInfo>
    , IAD_DecorationManager
{
    protected override void InitWithDefaultDatas()
    {
        //尝试查找已有的预设子物体并进行初始化
        ActiveController.RelinkElemets();
        ActiveController.InitExistElements();
    }
}