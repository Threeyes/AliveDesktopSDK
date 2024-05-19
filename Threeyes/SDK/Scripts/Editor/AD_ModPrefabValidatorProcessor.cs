#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UMod.BuildEngine;
using UMod.BuildPipeline;
using UMod.BuildPipeline.Build;
using UnityEditor;
using UnityEngine;
using Threeyes.Core;
using Threeyes.Core.Editor;

[UModBuildProcessor(".prefab", -101)]//仅处理Prefab文件
public class AD_ModPrefabValidatorProcessor : BuildEngineProcessor
{
    static bool IsCancelBuildWhenNestedPrefabExists = false;//是否在检查到有NestedPrefab存在就停止打包，否则仅警告（后期可以在SOSetting中设置）
    public override void ProcessAsset(BuildContext context, BuildPipelineAsset asset)
    {
        bool isObjValid = true;
        string errorInfo = null;

        Object objInst = asset.LoadedObject;
        if (IsTargetNestedPrefab(objInst))//如果检查到项目中有Nested prefab就报错(针对UMod在遇到部分NestedPrefab就会无法正常加载所有脚本的情况，可以通过激活IsCancelBuildWhenNestedPrefabExists来调试)
        {
            isObjValid = false;
            errorInfo = $"{asset.Name} ({asset.RelativePath}) is a nested Prefab, which may cause Mod to fail to load properly at runtime, please unpack all its child prefabs!";
        }

        if (isObjValid == false)
        {
            if (IsCancelBuildWhenNestedPrefabExists)// #报错，并暂停打包
            {
                //ToAdd: 提醒对应的Mod名
                string errorInfoHeader = $"<color=orange>Build mod failed with error:</color>\r\n";
                errorInfo = errorInfoHeader + errorInfo;
                errorInfo += "\r\n";
                context.FailBuild(errorInfo);
            }
            else
            {
                Debug.LogWarning($"<color=orange>{errorInfo}</color>");//仅打印警告(缺点：会被清掉，但可以通过打开EditorLog查看)
            }
        }
    }

    private static bool IsTargetNestedPrefab(Object selectedObj)
    {
        if (!PrefabUtility.IsPartOfAnyPrefab(selectedObj))
            return false;

        bool isTargetNestedPrefab = false;//标记目标Prefab是否为Nested Prefab
        if (selectedObj is GameObject selectGO)
        {
            Transform tfSelect = selectGO.transform;
            tfSelect.ForEachChildTransform(tf =>
            {
                if (isTargetNestedPrefab)//避免重复调用
                    return;

                GameObject go = tf.gameObject;
                if (PrefabUtility.IsPartOfAnyPrefab(go))//可能会包括实时添加的物体
                {
                    GameObject goNearestGO = PrefabUtility.GetNearestPrefabInstanceRoot(go);//如果是实时添加的物体，则会返回null；否则就代表其是其他Prefab的实例
                    if (goNearestGO != null)
                    {
                        isTargetNestedPrefab = true;
                        //Debug.LogError($"The nearest go for {go} is: {goNearestGO} _{goNearestGO == null}");
                    }
                }
            }, false, true);

            //Debug.LogError(selectedObj + " is Prefab");
            //PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot
        }
        return isTargetNestedPrefab;
    }
}
#endif