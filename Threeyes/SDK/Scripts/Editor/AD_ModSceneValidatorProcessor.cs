#if UNITY_EDITOR
using UMod.BuildEngine;
using UMod.BuildPipeline;
using UMod.BuildPipeline.Build;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

using Threeyes.Persistent;
using Threeyes.Editor;
using Threeyes.Steamworks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/// <summary>
/// 功能：检查场景设置是否正确
///
/// 具体流程：
/// -检测AD_AlliveDesktop有无正常引用Item文件夹内的AssetPack
/// -检测FileSystemController、DecorationController等必要组件是否存在场景（通过接口而不是具体类判断）
/// 
/// Ref:TanksModTools.ModSceneValidatorProcessor
/// </summary>
[UModBuildProcessor(".unity", -100)]
public class AD_ModSceneValidatorProcessor : BuildEngineProcessor
{
    public override void ProcessAsset(BuildContext context, BuildPipelineAsset asset)
    {
        if (!asset.FullPath.Contains(SOWorkshopItemInfo.SceneName))//只处理与ItemScene名称相关
            return;

        // Load the scene into the editor
        Scene scene = EditorSceneManager.OpenScene(asset.FullPath);
        bool validScene = true;
        string errorInfo = null;
        //——AD——
        var arrayAD = scene.GetComponents<AD_AliveDesktop>();
        if (arrayAD.Count() == 0 || arrayAD.Count() > 1)
        {
            validScene = false;
            errorInfo += $"-One and only one [{nameof(AD_AliveDesktop)}] Component should exists in scene!\r\n";
        }
        else
        {
            AD_AliveDesktop aliveDesktop = arrayAD.FirstOrDefault();
            if (aliveDesktop)
            {
                if (aliveDesktop.soAssetPack == null)
                {
                    validScene = false;
                    errorInfo += $"-{nameof(AD_AliveDesktop)}'s [soAssetPack] field is null!\r\n";
                }
            }
        }

        //——IAD_FileSystemController——
        if (scene.GetComponents<IAD_FileSystemController>().Count() != 1)
        {
            validScene = false;
            errorInfo += $"-One and only one component that inherits [{nameof(IAD_FileSystemController)}] should exists in scene!\r\n";
        }

        //——IAD_DecorationController——
        if (scene.GetComponents<IAD_DecorationController>().Count() != 1)
        {
            validScene = false;
            errorInfo += $"-One and only one component that inherits [{nameof(IAD_DecorationController)}] should exists in scene!\r\n";
        }


        //——ThreeyesPlugins——
        //PD:
        //1.检查重复的Key (PS: PDController会忽略无效的Key，而且PD会提醒，因此不需要检查）
        Dictionary<string, IPersistentData> dicKeyPD = new Dictionary<string, IPersistentData>();
        foreach (IPersistentData pd in scene.GetComponents<IPersistentData>(true))
        {
            if (!dicKeyPD.ContainsKey(pd.Key))
            {
                dicKeyPD[pd.Key] = pd;
            }
            else
            {
                validScene = false;
                IPersistentData pdCorrupt = dicKeyPD[pd.Key];

                errorInfo += $"-Same PD Key [{pd.Key}] in gameobjects:  {(pdCorrupt as Component)?.gameObject.name} & {(pd as Component)?.gameObject.name}!\r\n";
                break;
            }
        }

        // Check for valid scene
        if (validScene == false)
        {
            //ToAdd: 提醒对应的Mod场景名
            string sceneErrorHeader = $"Build mod scene [{EditorPathTool.AbsToUnityRelatePath(asset.FullPath)}] failed with error:\r\n";
            errorInfo = sceneErrorHeader + errorInfo;
            context.FailBuild(errorInfo);
        }
    }
}
#endif