using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using Threeyes.Config;
using Threeyes.Core;
using Threeyes.RuntimeSerialization;
using Threeyes.Steamworks;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Utilities;
/// <summary>
/// 基于进度的Interactable（如Slider、Lever）
/// 
/// ToUpdate：
/// -改名为更通用的字段
/// 
/// </summary>
public abstract class AD_XRProgressInteractable<TContainer, TConfig, TPropertyBag, TValue> : AD_XRBaseInteractable<TContainer, TConfig, TPropertyBag>
    where TContainer : Component, IConfigurableComponent<TConfig>
    where TConfig : ProgressInteractableConfigInfo<TValue>, new()
    where TPropertyBag : ComponentPropertyBag<TContainer>, new()
{
    #region Property & Field
    [SerializeField]
    [Tooltip("The object that is visually grabbed and manipulated")]
    protected Transform tfHandle = null;

    [SerializeField] protected XRInteractionUpdateOrder.UpdatePhase targeUpdatePhase = XRInteractionUpdateOrder.UpdatePhase.Fixed;//在哪个时段进行更新（针对Rigidbody建议设置为Fixed）

    //——以下参数适用于修改Rigidbody下的子物体（如抽屉）——
    [Header("Nested Rigidbody Setting")]
    [SerializeField] protected bool tempSetParentKinematicOnGrab = true;//在抓取时，临时将父物体的Rigidbody设置为Kinematic，可以避免父物体异常抖动
    [SerializeField] protected bool tempUseKinematicOnGrab = true;//在抓取时临时使用Kinematic抓取（如果Handle没有Rigidbody则自行添加，并在取消抓取时销毁）。优势是能够更精确的影响其他刚体，且防止被外力影响。建议tempSetParentKinematicOnGrab也要设置为true，否则会导致父子冲突而抖动。如果为false，则使用Transform。
    #endregion

    #region Init
    //void Start()
    //{
    //    //Init（暂不需要，因为要考虑到RS等延迟初始化情况，且组件通常会通过OnValidate将Handle同步到Value最新值）
    //    NotifyValueAndUpdatePos(Config.value);
    //}

    protected override void OnEnable()
    {
        base.OnEnable();
        selectEntered.AddListener(StartGrab);
        selectExited.AddListener(EndGrab);

        cacheIsHandleHasExistRigidbody = tfHandle.GetComponent<Rigidbody>() != null;
    }

    protected override void OnDisable()
    {
        selectEntered.RemoveListener(StartGrab);
        selectExited.RemoveListener(EndGrab);
        base.OnDisable();
    }
    public override void UpdateSetting()
    {
        ////【注释原因】：因为主要是通过XR组件而不是Inspector来更新数据，所以不用回调来修改，而是主动调用。等后期适配RuntimeEditable后再取消注释
        ////SetModel(Config.Value, true);
        ////NotifyEvent(Config.Value);
    }
    #endregion
  #region AttachTransform （Ref：XRGrabInteractable）（需要保留XRGrabInteractable类似字段的命名规范）
    /// <summary>
    /// The grab pose will be based on the pose of the Interactor when the selection is made.
    /// Unity will create a dynamic attachment point for each Interactor that selects this component.
    /// </summary>
    /// <remarks>
    /// A child GameObject will be created for each Interactor that selects this component to serve as the attachment point.
    /// These are cached and part of a shared pool used by all instances of <see cref="XRGrabInteractable"/>.
    /// Therefore, while a reference can be obtained by calling <see cref="GetAttachTransform"/> while selected,
    /// you should typically not add any components to that GameObject unless you remove them after being released
    /// since it won't always be used by the same Interactable.
    /// （在Interactor选中的点创建动态的AttachTransform，可以避免自行指定的麻烦）
    /// 
    /// Ref: XRGrabInteractable
    /// </remarks>
    /// <seealso cref="attachTransform"/>
    /// <seealso cref="InitializeDynamicAttachPose"/>
    [SerializeField] bool m_UseDynamicAttach = true;


    [SerializeField]
    [Tooltip("The attachment point Unity uses on this Interactable (will use this object's position if none set).")]
    [HideIf(nameof(m_UseDynamicAttach))][AllowNesting] protected Transform m_AttachTransform;//

    //PS: 因为该类的子类通常只需要获取抓取点的位置，所以以下参数暂不需要暴露
    bool m_MatchAttachPosition = true;
    bool m_MatchAttachRotation = true;
    bool m_SnapToColliderVolume = true;

    readonly Dictionary<IXRSelectInteractor, Transform> m_DynamicAttachTransforms = new Dictionary<IXRSelectInteractor, Transform>();
    static readonly UnityEngine.Pool.LinkedPool<Transform> s_DynamicAttachTransformPool = new UnityEngine.Pool.LinkedPool<Transform>(OnCreatePooledItem, OnGetPooledItem, OnReleasePooledItem, OnDestroyPooledItem);

    static Transform OnCreatePooledItem()
    {
        var item = new GameObject().transform;
        item.localPosition = Vector3.zero;
        item.localRotation = Quaternion.identity;
        item.localScale = Vector3.one;

        return item;
    }

    static void OnGetPooledItem(Transform item)
    {
        if (item == null)
            return;

        item.hideFlags &= ~HideFlags.HideInHierarchy;
    }

    static void OnReleasePooledItem(Transform item)
    {
        if (item == null)
            return;

        // Don't clear the parent of the GameObject on release since there could be issues
        // with changing it while a parent GameObject is deactivating, which logs an error.
        // By keeping it under this interactable, it could mean that GameObjects in the pool
        // have a chance of being destroyed, but we check that the GameObject we obtain from the pool
        // has not been destroyed. This means potentially more creations of new GameObjects, but avoids
        // the issue with reparenting.

        // Hide the GameObject in the Hierarchy so it doesn't pollute this Interactable's hierarchy
        // when it is no longer used.
        item.hideFlags |= HideFlags.HideInHierarchy;
    }

    static void OnDestroyPooledItem(Transform item)
    {
        if (item == null)
            return;

        Destroy(item.gameObject);
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (!IsValidGrabInteractor(args.interactorObject))
            return;

        base.OnSelectEntered(args);
    }
    protected override void OnSelectEntering(SelectEnterEventArgs args)
    {
        if (!IsValidGrabInteractor(args.interactorObject))
            return;

        // Setup the dynamic attach transform.
        // Done before calling the base method so the attach pose captured is the dynamic one.
        var dynamicAttachTransform = CreateDynamicAttachTransform(args.interactorObject);
        InitializeDynamicAttachPoseInternal(args.interactorObject, dynamicAttachTransform);
        base.OnSelectEntering(args);
    }
    protected override void OnSelectExiting(SelectExitEventArgs args)
    {
        if (!IsValidGrabInteractor(args.interactorObject))
            return;

        base.OnSelectExiting(args);
    }
    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        if (!IsValidGrabInteractor(args.interactorObject))
            return;

        base.OnSelectExited(args);
        ReleaseDynamicAttachTransform(args.interactorObject);
    }

    Transform CreateDynamicAttachTransform(IXRSelectInteractor interactor)
    {
        Transform dynamicAttachTransform;

        do
        {
            dynamicAttachTransform = s_DynamicAttachTransformPool.Get();
        } while (dynamicAttachTransform == null);

#if UNITY_EDITOR
        dynamicAttachTransform.name = $"[{interactor.transform.name}] Dynamic Attach";
#endif

        //设置DynamicAttach的父物体（应该改跟随tfHandle移动）
        dynamicAttachTransform.SetParent(/*transform*/ tfHandle, false);

        return dynamicAttachTransform;
    }

    void InitializeDynamicAttachPoseInternal(IXRSelectInteractor interactor, Transform dynamicAttachTransform)
    {
        // InitializeDynamicAttachPose expects it to be initialized with the static pose first
        InitializeDynamicAttachPoseWithStatic(interactor, dynamicAttachTransform);
        InitializeDynamicAttachPose(interactor, dynamicAttachTransform);
    }

    void InitializeDynamicAttachPoseWithStatic(IXRSelectInteractor interactor, Transform dynamicAttachTransform)
    {
        m_DynamicAttachTransforms.Remove(interactor);
        var staticAttachTransform = GetAttachTransform(interactor);
        m_DynamicAttachTransforms[interactor] = dynamicAttachTransform;

        // Base the initial pose on the Attach Transform.
        // Technically we could just do the final else statement, but setting the local position and rotation this way
        // keeps the position and rotation seen in the Inspector tidier by exactly matching instead of potentially having small
        // floating point offsets.
        if (staticAttachTransform == transform)
        {
            dynamicAttachTransform.localPosition = Vector3.zero;
            dynamicAttachTransform.localRotation = Quaternion.identity;
        }
        else if (staticAttachTransform.parent == transform)
        {
            dynamicAttachTransform.localPosition = staticAttachTransform.localPosition;
            dynamicAttachTransform.localRotation = staticAttachTransform.localRotation;
        }
        else
        {
            dynamicAttachTransform.SetPositionAndRotation(staticAttachTransform.position, staticAttachTransform.rotation);
        }
    }

    void ReleaseDynamicAttachTransform(IXRSelectInteractor interactor)
    {
        // Skip checking m_UseDynamicAttach since it may have changed after being grabbed,
        // and we should ensure it is released. We instead check Count first as a faster way to avoid hashing
        // and the Dictionary lookup, which should handle when it was never enabled in the first place.
        if (m_DynamicAttachTransforms.Count > 0 && m_DynamicAttachTransforms.TryGetValue(interactor, out var dynamicAttachTransform))
        {
            if (dynamicAttachTransform != null)
                s_DynamicAttachTransformPool.Release(dynamicAttachTransform);

            m_DynamicAttachTransforms.Remove(interactor);
        }
    }

    /// <summary>
    /// Unity calls this method automatically when initializing the dynamic attach pose.
    /// Used to override <see cref="matchAttachPosition"/> for a specific interactor.
    /// </summary>
    /// <param name="interactor">The interactor that is initiating the selection.</param>
    /// <returns>Returns whether to match the position of the interactor's attachment point when initializing the grab.</returns>
    /// <seealso cref="matchAttachPosition"/>
    /// <seealso cref="InitializeDynamicAttachPose"/>
    protected virtual bool ShouldMatchAttachPosition(IXRSelectInteractor interactor)
    {
        if (!m_MatchAttachPosition)
            return false;

        // We assume the static pose should always be used for sockets.
        // For Ray Interactors that bring the object to hand (Force Grab enabled), we assume that property
        // takes precedence since otherwise this interactable wouldn't move if we copied the interactor's attach position,
        // which would violate the interactor's expected behavior.
        if (interactor is XRSocketInteractor ||
            interactor is XRRayInteractor rayInteractor && rayInteractor.useForceGrab)
            return false;

        return true;
    }

    /// <summary>
    /// Unity calls this method automatically when initializing the dynamic attach pose.
    /// Used to override <see cref="matchAttachRotation"/> for a specific interactor.
    /// </summary>
    /// <param name="interactor">The interactor that is initiating the selection.</param>
    /// <returns>Returns whether to match the rotation of the interactor's attachment point when initializing the grab.</returns>
    /// <seealso cref="matchAttachRotation"/>
    /// <seealso cref="InitializeDynamicAttachPose"/>
    protected virtual bool ShouldMatchAttachRotation(IXRSelectInteractor interactor)
    {
        // We assume the static pose should always be used for sockets.
        // Unlike for position, we allow a Ray Interactor with Force Grab enabled to match the rotation
        // based on the property in this behavior.
        return m_MatchAttachRotation && !(interactor is XRSocketInteractor);
    }

    /// <summary>
    /// Unity calls this method automatically when initializing the dynamic attach pose.
    /// Used to override <see cref="snapToColliderVolume"/> for a specific interactor.
    /// </summary>
    /// <param name="interactor">The interactor that is initiating the selection.</param>
    /// <returns>Returns whether to adjust the dynamic attachment point to keep it on or inside the Colliders that make up this object.</returns>
    /// <seealso cref="snapToColliderVolume"/>
    /// <seealso cref="InitializeDynamicAttachPose"/>
    protected virtual bool ShouldSnapToColliderVolume(IXRSelectInteractor interactor)
    {
        return m_SnapToColliderVolume;
    }

    /// <summary>
    /// Unity calls this method automatically when the interactor first initiates selection of this interactable.
    /// Override this method to set the pose of the dynamic attachment point. Before this method is called, the transform
    /// is already set as a child GameObject with inherited Transform values.
    /// </summary>
    /// <param name="interactor">The interactor that is initiating the selection.</param>
    /// <param name="dynamicAttachTransform">The dynamic attachment Transform that serves as the attachment point for the given interactor.</param>
    /// <remarks>
    /// This method is only called when <see cref="useDynamicAttach"/> is enabled.
    /// </remarks>
    /// <seealso cref="useDynamicAttach"/>
    protected virtual void InitializeDynamicAttachPose(IXRSelectInteractor interactor, Transform dynamicAttachTransform)
    {
        var matchPosition = ShouldMatchAttachPosition(interactor);
        var matchRotation = ShouldMatchAttachRotation(interactor);
        if (!matchPosition && !matchRotation)
            return;

        // Copy the pose of the interactor's attach transform
        var interactorAttachTransform = interactor.GetAttachTransform(this);
        var position = interactorAttachTransform.position;
        var rotation = interactorAttachTransform.rotation;

        // Optionally constrain the position to within the Collider(s) of this Interactable
        if (matchPosition && ShouldSnapToColliderVolume(interactor) &&
            XRInteractableUtility.TryGetClosestPointOnCollider(this, position, out var distanceInfo))
        {
            position = distanceInfo.point;
        }

        if (matchPosition && matchRotation)
            dynamicAttachTransform.SetPositionAndRotation(position, rotation);
        else if (matchPosition)
            dynamicAttachTransform.position = position;
        else
            dynamicAttachTransform.rotation = rotation;
    }
    #endregion
    #region XRBaseInteractable
    public override Transform GetAttachTransform(IXRInteractor interactor)
    {
        //#1 优先返回DynamicAttach
        if (m_UseDynamicAttach && interactor is IXRSelectInteractor selectInteractor &&
    m_DynamicAttachTransforms.TryGetValue(selectInteractor, out var dynamicAttachTransform))
        {
            if (dynamicAttachTransform != null)
                return dynamicAttachTransform;

            m_DynamicAttachTransforms.Remove(selectInteractor);
            Debug.LogWarning($"Dynamic Attach Transform created by {this} for {interactor} was destroyed after being created." +
                " Continuing as if Use Dynamic Attach was disabled for this pair.", this);
        }

        //#2 Fallback
        return m_AttachTransform != null ? m_AttachTransform : GetFallbackAttachTransform(interactor);
    }
    protected virtual Transform GetFallbackAttachTransform(IXRInteractor interactor)
    {
        return tfHandle;
    }
    #endregion

    #region IRuntimeSerializableComponent
    public override void DeserializeFunc(TPropertyBag propertyBag)
    {
        base.DeserializeFunc(propertyBag);

        //#Restore
        SetModel(Config.Value, true);//还原时需要忽略刚体
        NotifyEvent(Config.Value);
    }
    #endregion

    #region UnityMethod
    protected virtual void LateUpdate()//先统一在LateUpdate运行
    {
        if (!tfHandle)
            return;

        if (!isGrabbing)
            LateUpdate_NoGrabbingState();//非抓取模式
    }
    /// <summary>
    /// 在非抓取模式下更新状态
    /// 
    /// 用途：
    /// -在物体受到外力影响而变化时，更新Value（如使用AD_XRHinge的椅子受外力而转向，此时需要将值同步给Value）
    /// </summary>
    protected virtual void LateUpdate_NoGrabbingState() { }
    #endregion

    #region Interaction
    //Runtime
    protected Transform tfHandleParent { get { return tfHandle.parent; } }//Handle的父物体，用于计算局部坐标系
    protected virtual bool IsDestroryHandleRigidbodyOnEndGrab
    {
        get
        {
            //检查Handle是否有Joint，如果有就不销毁（因为Joint依赖Rigidbody）
            bool hasJointComp = cacheParentRigidbody && cacheParentRigidbody.GetComponent<Joint>() != null;
            if (hasJointComp)
                return false;

            return true;
        }
    }

    protected IXRSelectInteractor cacheInteractor;
    protected Vector3 lastGrabLocalPos;//(用户按需使用)
    protected Vector3 lastGrabPos;//(用户按需使用)
    protected bool isGrabbing = false;
    //HandleParent (Rigidbody相关）
    protected Rigidbody cacheParentRigidbody;
    protected bool cacheParentRigidbodyKinematic = false;

    //Handle (Rigidbody相关）
    protected bool cacheIsHandleHasExistRigidbody;//Handle的刚体是否有提前增加的刚体（只需要设置一次）
    protected Rigidbody cacheHandleRigidbody;
    protected bool cacheHandleRigidbody_isKinematic;
    protected RigidbodyInterpolation cacheHandleRigidbody_interpolation;
    protected CollisionDetectionMode cacheHandleRigidbody_collisionDetectionMode;

    protected virtual void StartGrab(SelectEnterEventArgs args)
    {
        if (!IsValidGrabInteractor(args.interactorObject))
            return;
        isGrabbing = true;
        StartGrabFunc(args);
    }
    protected virtual void StartGrabFunc(SelectEnterEventArgs args)
    {
        ///——Init——
        cacheInteractor = args.interactorObject;

        //# 在抓取时同时锁定父物体的刚体（如有），类似于XRGrabable的实现。可以避免父物体鬼畜抖动
        if (tempSetParentKinematicOnGrab)
        {
            cacheParentRigidbody = tfHandle.FindFirstComponentInParent<Rigidbody>(false);
            if (cacheParentRigidbody)
            {
                cacheParentRigidbodyKinematic = cacheParentRigidbody.isKinematic;
                cacheParentRigidbody.isKinematic = true;
            }
        }

        //# 临时使用Kinect进行抓取，可以确保抓取途中与其他刚体进行碰撞（取消抓取后会销毁)
        if (tempUseKinematicOnGrab)
        {
            //#Cache
            cacheHandleRigidbody = tfHandle.AddComponentOnce<Rigidbody>();
            cacheHandleRigidbody_isKinematic = cacheHandleRigidbody.isKinematic;
            cacheHandleRigidbody_interpolation = cacheHandleRigidbody.interpolation;
            cacheHandleRigidbody_collisionDetectionMode = cacheHandleRigidbody.collisionDetectionMode;

            cacheHandleRigidbody.isKinematic = true;
            cacheHandleRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            cacheHandleRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            //rigidbody.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY//ToAdd:还要临时锁轴向，否则会抖动(容易出错)
        }

        StartGrabBeginFunc(args);//开始准备首次抓取的代码
        ProcessInteractableFunc();
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);

        if (!isGrabbing)//避免无效的xRSelectInteractor（如Socket）导致意外进入此方法
            return;

        if (updatePhase == this.targeUpdatePhase)
        {
            if (isSelected)
            {
                ProcessInteractableFunc();
            }
        }
    }
    protected virtual void EndGrab(SelectExitEventArgs args)
    {
        if (!IsValidGrabInteractor(args.interactorObject))
            return;
        EndGrabFunc(args);

        //#Reset
        cacheInteractor = null;//Clear
        isGrabbing = false;
    }
    protected virtual void EndGrabFunc(SelectExitEventArgs args)
    {
        ///——Reset——
        if (tempUseKinematicOnGrab)
        {
            if (cacheHandleRigidbody)
            {
                if (!cacheIsHandleHasExistRigidbody && IsDestroryHandleRigidbodyOnEndGrab)//刚体是临时添加的：销毁
                {
                    Destroy(cacheHandleRigidbody);
                    cacheHandleRigidbody = null;
                }
                else//不销毁：还原状态
                {
                    cacheHandleRigidbody.isKinematic = cacheHandleRigidbody_isKinematic;
                    cacheHandleRigidbody.interpolation = cacheHandleRigidbody_interpolation;
                    cacheHandleRigidbody.collisionDetectionMode = cacheHandleRigidbody_collisionDetectionMode;
                }
            }
        }

        if (tempSetParentKinematicOnGrab)
        {
            if (cacheParentRigidbody)
            {
                cacheParentRigidbody.isKinematic = cacheParentRigidbodyKinematic;
                cacheParentRigidbody = null;
            }
        }
    }

    /// <summary>
    /// 首次调用UpdateProcessInteractable前的初始化代码（如记录初始位置）
    /// </summary>
    /// <param name="args"></param>
    protected virtual void StartGrabBeginFunc(SelectEnterEventArgs args)
    {
        //Save grab begin pos（基类都存储该值，按需使用）
        lastGrabLocalPos = GetGrabLocalPosition();
        lastGrabPos = GetGrabPosition();
    }

    /// <summary>
    /// 更新抓取时的交互，类似Update/LateUpdate
    /// </summary>
    protected abstract void ProcessInteractableFunc();

    /// <summary>
    /// 只有值有变化时才更新，避免频繁调用
    /// </summary>
    /// <param name="value"></param>
    protected virtual void UpdateValueIfChanged(TValue value)
    {
        if (!value.Equals(Config.Value))
        {
            Config.Value = value;
            NotifyEvent(value);
        }
    }

    /// <summary>
    /// 设置模型到值对应的状态
    /// </summary>
    /// <param name="modelValue">该值可以是基于参数进行裁剪或优化的值，不一定与Value相同</param>
    /// <param name="ignoreRigidbody">是否忽略刚体，在初始化时需要忽略，否则可能会无法还原到正常位置</param>
    protected abstract void SetModel(TValue modelValue, bool ignoreRigidbody = false);
    protected abstract void NotifyEvent(TValue value);
    #endregion

    #region Utility
    /// <summary>
    /// 检查Interactor是否能抓取此物体
    /// </summary>
    /// <param name="xRSelectInteractor"></param>
    /// <returns></returns>
    protected virtual bool IsValidGrabInteractor(IXRSelectInteractor xRSelectInteractor)
    {
        if (xRSelectInteractor == null)
            return false;
        if (xRSelectInteractor is XRSocketInteractor)//排除Socket
            return false;
        return true;
    }

    /// <summary>
    /// 获取基于Handle父物体坐标的Interactor抓取局部坐标
    /// </summary>
    /// <returns></returns>
    protected Vector3 GetGrabLocalPosition()
    {
        //Debug.LogError("[ToDelete2]: attachTransformToThis: " + attachTransformToThis);
        return tfHandleParent.InverseTransformPoint(InteractorAttachTransform.position);//基于父物体坐标系，计算抓取点的局部坐标
    }
    protected Vector3 GetGrabPosition()
    {
        return InteractorAttachTransform.position;//抓取点
    }

    /// <summary>
    /// 返回的是如[Left Controller Stabilized Attach]的空物体（由Controller控制方向等），其初始位置由该组件的GetAttachTransform提供
    /// Warning:调用前需要确保m_Interactor不为空
    /// </summary>
    protected Transform InteractorAttachTransform { get { return cacheInteractor.GetAttachTransform(this); } }
    #endregion

    #region Editor

    protected virtual void OnDrawGizmosSelected() { }
    #endregion
}

[Serializable]
public abstract class ProgressInteractableConfigInfo<TValue> : SerializableComponentConfigInfoBase
{
    public abstract TValue Value { get; set; }
}