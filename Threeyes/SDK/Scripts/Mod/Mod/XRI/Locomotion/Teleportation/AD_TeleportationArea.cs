using System.Collections;
using System.Collections.Generic;
using Threeyes.Coroutine;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class AD_TeleportationArea : TeleportationArea
{
    #region Fix uMod Deserialize Problem
    protected override void Awake()
    {
        base.Awake();

        if (UModTool.IsUModGameObject(this))
            CoroutineManager.StartCoroutineEx(IEReInit());
    }
    IEnumerator IEReInit()
    {
        yield return null;//等待UMod初始化完成
        yield return null;//等待UMod初始化完成

        //ReInit
        base.Awake();
        OnDisable();
        OnEnable();
    }
    #endregion
}