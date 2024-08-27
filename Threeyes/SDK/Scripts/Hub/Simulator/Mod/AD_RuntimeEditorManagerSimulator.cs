using System.Collections;
using System.Collections.Generic;
using Threeyes.Core;
using Threeyes.RuntimeSerialization;
using Threeyes.GameFramework;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AD_RuntimeEditorManagerSimulator : InstanceBase<AD_RuntimeEditorManagerSimulator>, IAD_RuntimeEditorManager
{
    public bool IsActive
    {
        get { return isActive; }
        internal set
        {
            isActive = value;
        }
    }

    public SOAssetPack soAssetPack_SDK;//SDK目录下的所有资源引用
    readonly string AssetPackScope = "SDK";

    //# Runtime
    SOAssetPackInfo sOAssetPackInfo;
    bool isActive = false;

    protected override void SetInstanceFunc()
    {
        base.SetInstanceFunc();
        GameFrameworkTool.RegisterManagerHolder(this);

        // 添加SDK目录下的SOAssetPack（只初始化一次）
        sOAssetPackInfo = new SOAssetPackInfo(AssetPackScope, soAssetPack_SDK);
        SOAssetPackManager.Add(sOAssetPackInfo);
    }
    private void OnDestroy()
    {
        SOAssetPackManager.Remove(sOAssetPackInfo);
    }

    public void OnModInit(Scene scene, ModEntry modEntry)
    {
    }

    public void OnModDeinit(Scene scene, ModEntry modEntry)
    {
    }
}
