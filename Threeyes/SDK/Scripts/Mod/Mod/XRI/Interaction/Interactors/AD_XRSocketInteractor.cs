using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq;
using Threeyes.KeyLock;
using Threeyes.Core;
/// <summary>
/// 功能：
/// -包含可选Key的Socket
/// 
/// Warning:
/// -Layer需要设置为IgnoreRaycast，否则会捕获鼠标事件
/// </summary>
public class AD_XRSocketInteractor : XRSocketInteractor
{
    #region Fix uMod Deserialize Problem
    ///ToAdd:
    ///-增加可选的XRI_Examples.KeyLockSystem，需要确认是官方是否集成，还是说自己要弄一个类似的模块
    protected override void Awake()
    {
        base.Awake();

        if (UModTool.IsUModGameObject(this))//仅针对UMod打包物体才需要重新初始化
            CoroutineManager.StartCoroutineEx(IEReInit());
        else
            InitFunc();
    }
    IEnumerator IEReInit()
    {
        //PS:因为Awake初始化的字段都给uMod反序列化字段替换，所以要延后等待Mod完成后再次初始化。后续的其他XR组件也是相同思路
        yield return null;//等待UMod初始化完成
        yield return null;//等待UMod初始化完成
        ReInit();
    }
    [ContextMenu("ReInit")]
    protected virtual void ReInit()
    {
        if (!this)//空的可能原因：创建后被立即销毁
        {
            Debug.LogError($"({this.GetType()}) is null!");
            return;
        }

        base.Awake();
        OnDisable();
        OnEnable();

        InitFunc();
    }

    protected virtual void InitFunc()
    {
        //方便子类添加一些需要在Awake执行的方法
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        OnDisable();
    }
    #endregion

    #region 修复Socket被占用后仍绘制无效Interactable的Mesh的问题
    protected override int socketSnappingLimit
    {
        get
        {
            return base.socketSnappingLimit;
        }
    }//最大可附着数量

    protected virtual bool DrawCantHoverMaterialOnOccupied { get { return drawCantHoverMaterialOnOccupied; } }
    [SerializeField] protected bool drawCantHoverMaterialOnOccupied = false;//是否在Socket自身空位被占满时仍绘制CantHover模型。（基类默认实现为true）

    protected override Material GetHoveredInteractableMaterial(IXRHoverInteractable interactable)
    {
        ///——以下条件不绘制m_InteractableCantHoverMeshMaterial（直接返回null即可）——
        ///#1 如果Interactable已经在其他XRSocketInteractor上附着，那就不绘制（如：物体跨越两个Pegboard）
        if (IsSelectedByOtherInteractor(interactable))
        {
            //Debug.LogError(transform.parent.name + ": interactableIsOnOtherInteractor");
            return null;
        }

        ///#2 自身已经附着有足够数量的Interactable（子类如Grid可能有多个，需要重载）
        if (!DrawCantHoverMaterialOnOccupied)
        {
            if (interactablesSelected.Count >= socketSnappingLimit)
            {
                //Debug.LogError(transform.parent.name + ": self reach max");
                return null;
            }
        }
        else
        {
            return base.GetHoveredInteractableMaterial(interactable);
        }

        return base.GetHoveredInteractableMaterial(interactable);//Fallback
    }

    protected virtual bool IsSelectedByOtherInteractor(IXRInteractable interactable)
    {
        //if (this.GetType() == typeof(AD_XRGridSocketInteractor) && interactable.transform.gameObject.name == "ShellItem_Simple Cube(Test)")
        //{
        //    Debug.LogError("Testing" /*+ "_" + CanHover(interactable) + "_" + interactionManager.CanHover(this, interactable)*/);
        //}

        //检测Selected
        if (interactable is IXRSelectInteractable selectInteractable)
        {
            bool isSelectedByOtherInteractor = selectInteractable.interactorsSelecting.Any(
            interactor =>
            {
                if (interactor is XRSocketInteractor socketInteractor)//仅检测XRSocketInteractor，忽略XRRayInteractor等
                {
                    if (socketInteractor != this && socketInteractor.IsSelecting(selectInteractable))//检测是否为非本物体Select
                        return true;
                }
                return false;
            });
            if (isSelectedByOtherInteractor)
                return true;
        }

        //检测Hovered
        //if (interactable is IXRHoverInteractable hoverInteractable)
        //{
        //    bool isHoveredByOtherInteractor = hoverInteractable.interactorsHovering.Any(
        //    interactor =>
        //    {
        //        if (interactor is XRSocketInteractor socketInteractor)
        //        {
        //            if (socketInteractor != this && socketInteractor.IsHovering(hoverInteractable))//检测是否为非本物体Hover
        //                return true;
        //        }
        //        return false;
        //    });

        //    if (isHoveredByOtherInteractor)
        //        return true;
        //}

        return false;
    }

    protected override bool ShouldDrawHoverMesh(MeshFilter meshFilter, Renderer meshRenderer, Camera mainCamera)
    {
        if(meshRenderer)//如果该物体被隐藏，或者MeshRenderer被禁用：不绘制
        {
            if (!meshRenderer.gameObject.activeInHierarchy || !meshRenderer.enabled)
                return false;
        }
     return base.ShouldDrawHoverMesh(meshFilter, meshRenderer, mainCamera);
    }
    #endregion

    #region KeyLock (Optional) （Ref：XRLockSocketInteractor）
    // The required keys to interact with this socket.
    public Lock keychainLock
    {
        get => m_Lock;
        set => m_Lock = value;
    }

    [Space]
    [SerializeField][Tooltip("The optional keys to interact with this socket.")] Lock m_Lock;
    [SerializeField][Tooltip("The optional keys to not interact with this socket.")] Lock m_ExcludeLock;

    /// <inheritdoc />
    public override bool CanHover(IXRHoverInteractable interactable)
    {
        if (!base.CanHover(interactable))
            return false;

        return CanUnlock(interactable);
    }

    /// <inheritdoc />
    public override bool CanSelect(IXRSelectInteractable interactable)
    {
        if (!base.CanSelect(interactable))
            return false;

        return CanUnlock(interactable);
    }

    protected bool CanUnlock(IXRInteractable interactable)
    {
        ///ToAdd：
        ///-后期可增加需要排除的全局InteractionLayer（如自定义的Ignore Socket），并在此判断


        var keyChain = interactable.transform.GetComponent<IKeychain>();
        bool canUnlock = m_Lock.CanUnlock(keyChain);

        if (canUnlock)
        {
            if (m_ExcludeLock.IsValid)
                canUnlock &= !m_ExcludeLock.CanUnlock(keyChain);//检查Key是否在排除清单范围
        }
        return canUnlock;
    }
    #endregion

}