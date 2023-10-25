using System.Collections;
using System.Collections.Generic;
using Threeyes.Steamworks;
using UnityEngine;

public interface IAD_CommonSettingManager : IHubManagerModInitHandler
{
    //ToUpdate

}

public interface IAD_CommonSetting_IsRunAtStartUpHandler
{
	void OnIsRunAtStartUpChanged(bool isActive);
}
//public interface IAD_CommonSetting_IsSupportMultiDisplayHandler
//{
//	void OnIsSupportMultiDisplayChanged(bool isActive);
//}
public interface IAD_CommonSetting_IsVSyncActiveHandler
{
	void OnIsVSyncActiveChanged(bool isActive);
}
public interface IAD_CommonSetting_TargetFrameRateHandler
{
	void OnTargetFrameRateChanged(int value);
}
public interface IAD_CommonSetting_LocalizationHandler
{
	void OnLocalizationChanged(string value);
}
public interface IAD_CommonSetting_QualityHandler
{
	void OnQualityChanged(string value);
}
public interface IAD_CommonSetting_ProcessPriorityHandler
{
	void OnProcessPriorityChanged(string value);
}
