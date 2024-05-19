#if UNITY_EDITOR
using UMod.BuildEngine;
using UMod.BuildPipeline;
using UMod.BuildPipeline.Build;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

using Threeyes.Persistent;
using Threeyes.Steamworks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Threeyes.Core;
using Threeyes.Core.Editor;
/// <summary>
/// 功能：检查场景设置是否正确
///
/// 具体流程：
/// -检测AD_AlliveDesktop有无正常引用Item文件夹内的AssetPack
/// -检测FileSystemController、DecorationController等必要组件是否存在场景（通过接口而不是具体类判断）
/// 
/// Ref:TanksModTools.ModSceneValidatorProcessor
/// </summary>
[UModBuildProcessor(".unity", -100)]//仅处理Scene文件
public class AD_ModSceneValidatorProcessor : BuildEngineProcessor
{
    public override void ProcessAsset(BuildContext context, BuildPipelineAsset asset)
    {
        ///ToAdd:
        ///-判断当前WorkshopItemInfo.itemType，如果不是Scene则忽略下述的判断
        ///     -获取当前 WorkshopItemInfo 的方法：
        ///         -通过SOManagerInst.CurWorkshopItemInfo获取实例，前提是每次打包时ItemManagerWindow会设置该字段，因为有可能会调用BuildAll
        ///         -根据Scene文件查找WorkshopItemInfo，参考SceneSimulator

        bool validScene = true;
        string errorInfo = null;
        //# 场景Mod的Scene文件
        if (asset.FullPath.GetFileNameWithoutExtension() == SOWorkshopItemInfo.SceneName)//只处理与ItemScene名称相关（如果Model里面包含场景，只要不是与Scene同名，则不会被处理）
        {
            // Load the scene into the editor
            Scene scene = EditorSceneManager.OpenScene(asset.FullPath);

            //——AD_AliveDesktop——
            var arrayAD = scene.GetComponents<AD_AliveDesktop>();
            if (arrayAD.Count() == 0 || arrayAD.Count() > 1)
            {
                validScene = false;
                errorInfo += $"-One and only one [{nameof(AD_AliveDesktop)}] Component should exists in scene!\r\n";
            }
            //——IAD_FileSystemController——
            if (scene.GetComponents<IAD_ShellController>().Count() != 1)
            {
                validScene = false;
                errorInfo += $"-One and only one component that inherits [{nameof(IAD_ShellController)}] should exists in scene!\r\n";
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
        }
        // Check for valid scene
        if (validScene == false)
        {
            //ToAdd: 提醒对应的Mod场景名（尝试从context.ModAssetsPath中可直接获取）
            string errorInfoHeader = $"<color=orange>Build mod scene [{EditorPathTool.AbsToUnityRelatePath(asset.FullPath)}] failed with error</color>:\r\n";//使用颜色，更加突出
            errorInfo = errorInfoHeader + errorInfo;
            errorInfo += "\r\n";
            context.FailBuild(errorInfo);
        }
    }
}
#endif