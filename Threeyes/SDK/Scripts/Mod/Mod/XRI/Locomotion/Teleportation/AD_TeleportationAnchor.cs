using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Reflection;
using Threeyes.XRI;
using Threeyes.Core;
/// <summary>
/// 
/// Todo:
/// -传送后隐藏的选项
/// </summary>
public class AD_TeleportationAnchor : TeleportationAnchor
{
    #region 
    // Reusable event args
    readonly LinkedPool<TeleportingEventArgs> m_TeleportingEventArgsEx = new LinkedPool<TeleportingEventArgs>(() => new TeleportingEventArgs(), collectionCheck: false);

    [Header("Extra Config")]
    public bool isHideOnEnter = false;//是否本次进入之后就隐藏该路标

    #endregion

    #region Fix uMod Deserialize Problem
    protected override void Awake()
    {
        base.Awake();

        if (UModTool.IsUModGameObject(this))
            CoroutineManager.StartCoroutineEx(IEReInit());
        else
            InitFunc();
    }
    IEnumerator IEReInit()
    {
        yield return null;//等待UMod初始化完成
        yield return null;//等待UMod初始化完成

        //ReInit
        base.Awake();
        OnDisable();
        OnEnable();

        InitFunc();
    }

    protected virtual void InitFunc()
    {
        if (isHideOnEnter)
        {
            teleporting.AddListener(args => Hide());
        }
    }
    protected override void OnDestroy()
    {
        /////PS:以下方法可以避免物体被销毁时，XRInteractionManager 报错（https://github.com/Unity-Technologies/XR-Interaction-Toolkit-Examples/issues/26）
        base.OnDestroy();
        OnDisable();//手动调用 UnregisterWithInteractionManager，避免InteractionManager检测不到该物体而报错
    }
    #endregion

    /// <summary>
    /// Manual teleport to this anchor
    /// 
    /// Ref：BaseTeleportationInteractable.SendTeleportRequest
    /// 
    /// PS:
    /// -隐藏后仍能正常调用
    /// </summary>
    [ContextMenu("TeleportToThis")]
    public void Teleport()
    {
        //确保即使该物体从未显示，也能够正常传送
        TeleportationProvider m_TeleportationProvider = teleportationProvider;
        if (m_TeleportationProvider == null)
        {
            if (!ComponentLocatorUtilityEx<TeleportationProvider>.TryFindComponent(out m_TeleportationProvider))
                return;
            else
            {
                teleportationProvider = m_TeleportationProvider;//缓存获得的组件
            }
        }

        var teleportRequest = new TeleportRequest
        {
            matchOrientation = matchOrientation,
            requestTime = Time.time,
        };
        IXRInteractor interactor = null; //ToUpdate:从AD_XRManager接口中获取默认的interactor
        RaycastHit raycastHit = default;
        bool success = GenerateTeleportRequest(interactor, raycastHit, ref teleportRequest);

        if (success)
        {
            UpdateTeleportRequestRotationEx(interactor, ref teleportRequest);//Warning：默认是私有方法
            success = m_TeleportationProvider.QueueTeleportRequest(teleportRequest);//发送Request结构数据后，后续的传送就与该物体无关，可以放心隐藏

            if (success && teleporting != null)
            {
                using (m_TeleportingEventArgsEx.Get(out var args))
                {
                    args.interactorObject = interactor;
                    args.interactableObject = this;
                    args.teleportRequest = teleportRequest;
                    teleporting.Invoke(args);
                }
            }
        }
    }

    void UpdateTeleportRequestRotationEx(IXRInteractor interactor, ref TeleportRequest teleportRequest)
    {
        MethodInfo methodInfo = ReflectionTool.GetMethod(this.GetType(), "UpdateTeleportRequestRotation");//通过反射的方式获取父类的方法
        object[] args = new object[] { interactor, teleportRequest };
        methodInfo.Invoke(this, args);
        teleportRequest = (TeleportRequest)args[1];//PS:因为UpdateTeleportRequestRotation中的teleportRequest参数被标记为ref，所以需要获取修改后的值并更新输入值
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
