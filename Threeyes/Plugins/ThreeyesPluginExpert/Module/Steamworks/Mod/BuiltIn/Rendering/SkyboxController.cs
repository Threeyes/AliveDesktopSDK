using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Threeyes.Coroutine;
namespace Threeyes.Steamworks
{
    /// <summary>
    /// Replace the global Skybox Texture
    /// 
    /// PS：
    /// -如果用户需要快速替换 SkyboxController，可以在Hierarchy中对该物体点击右键，然后选择：更改样式，折后选中其他SkyboxController即可
    /// 
    /// Todo:
    /// -需要通知DefaultEnvironmentController，表明要重载天空盒，让其停止修改天空盒材质
    /// -通过x的方法来更新天空盒，因为其内部有其他额外方法（如通知场景的反射探头更新，需要其有一个回调方便探头监听）
    /// -【重要】如果当前用户点击了天空盒（或者是空地方），那么就选中当前激活的SkyboxController对应物体，方便删除
    /// -Config
    ///     -AutoRotate
    ///     -其他材质上的配置都留给MaterialController
    /// </summary>
    public class SkyboxController : MonoBehaviour
    {
        public Material skyboxMaterial;

        #region Init
        ///ToUpdate:
        ///-因为ManagerHolder.EnvironmentManager.BaseActiveController 的初始化在IHubManagerModInitHandler中调用，晚于Shell/Decoration的IHubManagerModInitHandler的执行顺序，所以要延后（或者将IHubManagerModInitHandler统一放在IHubManagerModPreInitHandler）
        ///-要等待OnModInit完成，EnvironmentController初始化完成才能设置（也可以是提供当前Mod场景已经初始化完成的字段）(或者类似XR组件，等待2帧后执行)
        private void OnEnable()//在Enable时就调用，方便在摆放Ghost就能看到效果
        {
            if (hasInit)
                return;
            cacheEnum_Init = CoroutineManager.StartCoroutineEx(IEInit());
        }
        private void OnDestroy()
        {
            TryStopCoroutine_Init();
            if (!hasInit)
                return;
            if (ManagerHolder.EnvironmentManager == null)
                return;
            ManagerHolder.EnvironmentManager.BaseActiveController.UnRegisterCustomSkybox(this);
        }

        bool hasInit = false;
        protected UnityEngine.Coroutine cacheEnum_Init;
        IEnumerator IEInit()
        {
            hasInit = true;//因为OnEnable会导致多次进入，所以直接设置为true
            if (ManagerHolder.SceneManager == null)
                yield break;
            while (ManagerHolder.SceneManager.IsChangingScene)//等待场景初始化完成（主要是ActiveController被初始化）
                yield return null;

            if (ManagerHolder.EnvironmentManager == null)
                yield break;
            ManagerHolder.EnvironmentManager.BaseActiveController.RegisterCustomSkybox(this);
        }
        protected virtual void TryStopCoroutine_Init()
        {
            if (cacheEnum_Init != null)
            {
                CoroutineManager.StopCoroutineEx(cacheEnum_Init);
                cacheEnum_Init = null;
            }
        }

        #endregion
    }
}