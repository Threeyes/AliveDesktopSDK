using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.State;
using UnityEngine.Animations.Rigging;
using System;
using Threeyes.Core;
/// <summary>
/// 管理Simulator模式下的按键监听及Rig/Camera控制
/// 
/// Todo:
/// -因为父类有部分字段无法访问，所以先embebed包，将字段暴露，修改完成后再通过反射设置【已经在Forum申请公开私有字段（https://forum.unity.com/threads/make-xrdevicesimulator-easier-to-override.1505804/）】
/// -可以不删除Simulator的Action，而是清空其中的硬件绑定，让其无法达到条件（经测试不行，会报错。改为重载OnEnable，并调用protected方法）
/// 
/// Warning：
/// -不要直接修改原XRDeviceSimulator的资源文件，而是克隆一份，方便Modder使用原XRDeviceSimulator进行调试（因为其带有完善的交互）
/// 
/// PS：
/// -XRInputModalityManager会检测每个Controller的isTracked，如果为false则隐藏物体。因此指定手柄不更新该值即可隐藏，或者将TrackingState下的XXControllerIsTracked禁用即可
/// -不是通过键盘来直接控制Rig，而是通过手柄的Action，从而调用DynamicMoveProvider
/// -deviceSimulatorActionAsset对应直接的HMD/Controller控制，controllerActionAsset对应VR键位
/// 
/// 主要功能：
/// -WSAD：默认是移动Rig；抓取物体时移动物体
/// -QE：【待确认】飞行模式下是升降
/// -左键抓取，（【待确认】部分复杂物体可以设置为左键抓取后一直在手上，然后使用特殊案件触发，然后右键（或键盘自定义按键，参考FPS游戏）放下）
/// -通过特殊按键切换DynamicMoveProvider的Gravity/Fly模式
/// 
/// 兼容【Gamepad模式】，实现方式：
/// -交互：将鼠标固定在屏幕中心(Cursor.lockState = CursorLockMode.Locked)，然后在中间增加一个半透明的光标（可以通过监听当前是否为键盘/鼠标输入，来开关该模式；或者通过control schemes；或通过CallbackContext.control.device）
/// -【V2】编辑功能：打开编辑模式后，使用Virtual Mouse来控制鼠标（用于UI，可以通过开关切换）（使用下方的四向键来控制鼠标，避免跟摇杆冲突。也可以将抓取交互点固定到中心，然后鼠标使用SoftwareCursor并额外处理）：使用Sample的GamepadMouseCursor。还需要找相关屏幕键盘插件
/// 
/// 
/// Todo：
/// 如果在UI上，以下按键无效：
///     -右键按下控制头显（【待确认】：抬起是否考虑）
/// 如果在UI输入中，以下按键无效：
///     -通过手柄移动XRRig
///     -其他向相关按键
/// </summary>
public class AD_XRDeviceSimulator : XRDeviceSimulator
    , IAD_RuntimeEditor_ModeActiveHandler
{
    #region Property & Fields
    static bool IsHoveringUI { get { return UITool.IsHoveringUI(); } }
    static bool IsFocusingInputfield { get { return UITool.IsFocusingInputfield(); } }
    public Quaternion CameraRotation { get { return m_HMDState.centerEyeRotation; } }//PS:因为程序退出时，RemoveDevices会使vrCamera提前恢复到默认旋转值，此时直接获取相机的旋转只能获取到默认值。（PS：因为编辑器模式无法阻止程序退出，所以改为返回当前的旋转值）

    readonly int InteractionLayerMask_TeleportID = 31;//Teleport的LayerMaskID

    Transform tfCameraRigParent { get { return AD_ManagerHolder.XRManager.TfCameraRigParent; } }
    Transform tfCameraEye { get { return AD_ManagerHolder.XRManager.TfCameraEye; } }
    Camera vrCamera { get { return AD_ManagerHolder.XRManager.VrCamera; } }
    ActionBasedController leftController { get { return AD_ManagerHolder.XRManager.LeftController; } }
    Transform tfLeftController { get { return AD_ManagerHolder.XRManager.TfLeftController; } }
    Transform tfRightController { get { return AD_ManagerHolder.XRManager.TfRightController; } }


    [Header("Custom Config")]
    public GameObject goGamepadUI;
    [SerializeField] InputActionReference m_SpeedUpAction;//加速
    [SerializeField] InputActionReference m_ManipulateSelectedObjectAction;//调整被选中物体（【PC模式】X键）

    [SerializeField] float _fovIncreaseSpeed = 1f;//Velocity of camera zooming in/out
    public bool hideControllerVisual = true;//是否隐藏Controller的模型、LineRenderer等，设置为false方便调试

    [Header("Runtime")]
    public XRRayInteractor leftRayInteractor;//使用手柄的Ray作为交互
    public Transform tfLeftRayProvider;//用于显示当前RayInteractor与物体交互的状态
    public ContinuousMoveProviderBase continuousMoveProviderBase;//提供持续移动的组件，当前版本为DynamicMoveProvider。可以由两个手柄进行控制
    Transform tfRayInteractor;
    #endregion

    #region Init
    //——PS：以下部分字段及方法与父类同名，等后续父类公开后可直接删掉——
    bool m_OnInputDeviceChangeSubscribed_Ex;
    protected override void OnEnable()
    {
        //base.OnEnable();

        //——复刻父类OnEnable的关键方法——
        if (removeOtherHMDDevices)
        {
            // Operate on a copy of the devices array since we are removing from it
            foreach (var device in InputSystem.devices.ToArray())
            {
                if (device is XRHMD && !(device is XRSimulatedHMD))
                {
                    InputSystem.RemoveDevice(device);
                }
            }

            InputSystem.onDeviceChange += OnInputDeviceChange;
            m_OnInputDeviceChangeSubscribed_Ex = true;
        }
        FindCameraTransform();//不需要，已经由Init实现
        AddDevices();

        //# 仅监听必要的按键
        SubscribeKeyboardXTranslateAction();
        SubscribeKeyboardYTranslateAction();
        SubscribeKeyboardZTranslateAction();
        SubscribeManipulateHeadAction();
        SubscribeMouseDeltaAction();
        SubscribeMouseScrollAction();

        SubscribeAxis2DAction();
        SubscribeGripAction();
        SubscribeTriggerAction();
        SubscribePrimaryButtonAction();//主按键（B）
        SubscribeSecondaryButtonAction();//次按键（N）
        SubscribeMenuAction();//菜单键（M）
        SubscribePrimary2DAxisClickAction();//主四向键按下（鼠标中键）（作用：抓取物体时，切换移动和缩放）

        SubscribeSpeedUpAction_Custom();
        SubscribeManipulateSelectedObjectAction_Custom();


        if (controllerActionAsset != null)
            controllerActionAsset.Enable();
        if (deviceSimulatorActionAsset != null)
            deviceSimulatorActionAsset.Enable();

        AD_ManagerHolderManager.PlayerInputControlChanged += OnPlayerInputControlChanged; ;
    }

    protected override void OnDisable()
    {
        //base.OnDisable();

        //——复刻父类的关键方法——
        if (m_OnInputDeviceChangeSubscribed_Ex)
        {
            InputSystem.onDeviceChange -= OnInputDeviceChange;
            m_OnInputDeviceChangeSubscribed_Ex = false;
        }

        RemoveDevices();

        //# 仅监听必要的按键
        UnsubscribeKeyboardXTranslateAction();
        UnsubscribeKeyboardYTranslateAction();
        UnsubscribeKeyboardZTranslateAction();
        UnsubscribeManipulateHeadAction();//Bug:没有调用到
        UnsubscribeMouseDeltaAction();
        UnsubscribeMouseScrollAction();

        UnsubscribeAxis2DAction();
        UnsubscribeGripAction();
        UnsubscribeTriggerAction();
        UnsubscribePrimaryButtonAction();//主按键（B）
        UnsubscribeSecondaryButtonAction();//次按键（N）
        UnsubscribeMenuAction();//菜单键（M）
        UnsubscribePrimary2DAxisClickAction();

        UnsubscribeSpeedUpAction_Custom();
        UnsubscribeManipulateSelectedObjectAction_Custom();

        if (controllerActionAsset != null)
            controllerActionAsset.Disable();

        if (deviceSimulatorActionAsset != null)
            deviceSimulatorActionAsset.Disable();

        AD_ManagerHolderManager.PlayerInputControlChanged -= OnPlayerInputControlChanged; ;
    }

 
    protected override void OnManipulateHeadPerformed(InputAction.CallbackContext context)
    {
        if (IsHoveringUI)//在UI上时按下无效（抬起不管）
            return;
        base.OnManipulateHeadPerformed(context);
    }
    protected override void OnAxis2DPerformed(InputAction.CallbackContext context)//用途：方向键控制角色移动
    {
        if (IsFocusingInputfield)//在输入时无效（仅悬在UI上有效）
            return;
        base.OnAxis2DPerformed(context);
    }
    protected override void OnGripPerformed(InputAction.CallbackContext context)
    {
        if (IsHoveringUI)//在UI上时按下无效（抬起不管）
            return;
        base.OnGripPerformed(context);
    }
    void SubscribeSpeedUpAction_Custom() => Subscribe(m_SpeedUpAction, OnSpeedUpPerformed, OnSpeedUpCanceled);
    void UnsubscribeSpeedUpAction_Custom() => Unsubscribe(m_SpeedUpAction, OnSpeedUpPerformed, OnSpeedUpCanceled);

    bool m_SpeedUpInput;
    bool m_SpeedUpRapidClick;//是否频繁点击Shift

    float lastClickSpeedUpTime;
    private void OnSpeedUpPerformed(InputAction.CallbackContext obj)
    {
        if (Time.unscaledTime - lastClickSpeedUpTime < 0.5f)//频繁点击
        {
            m_SpeedUpRapidClick = true;
        }

        lastClickSpeedUpTime = Time.unscaledTime;
        m_SpeedUpInput = true;
    }
    private void OnSpeedUpCanceled(InputAction.CallbackContext obj)
    {
        m_SpeedUpRapidClick = false;//Reset
        m_SpeedUpInput = false;
    }

    bool m_ManipulateSelectedObjectInput;
    private void SubscribeManipulateSelectedObjectAction_Custom() => Subscribe(m_ManipulateSelectedObjectAction, OnManipulateSelectedObjectPerformed, OnManipulateSelectedObjectCanceled);
    private void UnsubscribeManipulateSelectedObjectAction_Custom() => Unsubscribe(m_ManipulateSelectedObjectAction, OnManipulateSelectedObjectPerformed, OnManipulateSelectedObjectCanceled);
    void OnManipulateSelectedObjectPerformed(InputAction.CallbackContext obj) => m_ManipulateSelectedObjectInput = true;
    void OnManipulateSelectedObjectCanceled(InputAction.CallbackContext obj) => m_ManipulateSelectedObjectInput = false;


    private void Start()
    {
        //PS:不能放在OnEnable，因为有可能在AD_XRManager.SetInstanceFunc之前执行，原因是各组件的Awake的调用时机是不确定的：（https://forum.unity.com/threads/onenable-before-awake.361429/#post-2341710）
        Init();
    }

    /// <summary>
    /// 根据Simulator的设定，临时禁用部分不用的组件。
    /// 
    /// Warning：
    /// -为了方便运行时切换模式，需要确保能够复原
    /// </summary>
    public void Init()
    {
        //#1 初始化所有VR相关字段
        tfRayInteractor = tfLeftController.Find("Ray Interactor");
        leftRayInteractor = tfRayInteractor.GetComponent<XRRayInteractor>();
        tfLeftRayProvider = tfLeftController.Find("Simulator Ray Provider");//PS:因为Ray Interactor不在原点，所以需要放在tfLeftController下而不是RayInteractor下
        continuousMoveProviderBase = tfCameraRigParent.FindFirstComponentInChild<ContinuousMoveProviderBase>(false, true);

        //#2 显/隐不需要的物体:
        //-隐藏模拟器不需要的Interactor，仅保留Ray(PS:虽然Interactor由XRInteractionGroup管理，但是为了避免运行时切换到VR模式，还是直接隐藏了事，它们的代码会在Disable时取消注册)
        tfLeftController.FindFirstComponentInChild<XRPokeInteractor>()?.gameObject.SetActive(false);
        tfRightController.FindFirstComponentInChild<XRPokeInteractor>()?.gameObject.SetActive(false);
        tfLeftController.FindFirstComponentInChild<XRDirectInteractor>()?.gameObject.SetActive(false);
        tfRightController.FindFirstComponentInChild<XRDirectInteractor>()?.gameObject.SetActive(false);
        tfCameraEye.FindFirstComponentInChild<TunnelingVignetteController>()?.gameObject.SetActive(false);//隐藏移动时的黑屏遮罩
        tfLeftRayProvider.gameObject.SetActive(true);

        //#3 隐藏控制器模型
        if (hideControllerVisual)
        {
            leftController.modelPrefab = null;//控制器不生成模型（后期可以改为生成后再隐藏，因为可能默认是以VR模式启动，这时模型已经生成。实现：在LateUpdate中检测ActionBasedController.model 是否正在显示，如果是就隐藏【因为此时model的显隐由XRBaseControllerInteractor控制，所以要一直保证其被隐藏】）
            XRInteractorLineVisual xRInteractorLineVisual = leftRayInteractor.GetComponent<XRInteractorLineVisual>();
            if (xRInteractorLineVisual)
                xRInteractorLineVisual.enabled = false;
        }

        //#4 为Ray增加Teleport相关Layer，确保可直接点击Teleport进行传送
        leftRayInteractor.interactionLayers = leftRayInteractor.interactionLayers | 1 << InteractionLayerMask_TeleportID;

        //#5 初始化其他设置
        m_LeftControllerState.isTracked = true;//仅设置左手柄可见（其他手柄会被XRInputModalityManager隐藏）
        m_TargetedDeviceInput = TargetedDevices.LeftDevice;//默认控制手柄（Warning：因为枚举是私有的，因此后续尝试用int进行赋值）

        leftRayInteractor.selectEntered.AddListener(OnRaySelectEntered);
        leftRayInteractor.selectExited.AddListener(OnRaySelectExited);
    }
    bool isRaySelectEntered = false;
    private void OnRaySelectEntered(SelectEnterEventArgs args)
    {
        isRaySelectEntered = true;
    }
    private void OnRaySelectExited(SelectExitEventArgs args)
    {
        isRaySelectEntered = false;
    }


    void OnPlayerInputControlChanged(PlayerInput playerInput, AD_InputDeviceType inputDeviceType)
    {
        goGamepadUI.SetActive(false);
        switch (inputDeviceType)
        {
            case AD_InputDeviceType.Gamepad:
                Cursor.lockState = CursorLockMode.Locked;//将光标锁定到屏幕中央（PS：编辑器模式下，需要鼠标点击Game窗口，才能正常隐藏光标，否则会发现光标一直在其他窗口显示，不算bug）
                goGamepadUI.SetActive(true);//显示对应的UI
                break;
            default:
                Cursor.lockState = CursorLockMode.None;
                break;
        }
    }

    void OnInputDeviceChange(InputDevice device, InputDeviceChange change)
    {
#if ENABLE_VR || (UNITY_GAMECORE && INPUT_SYSTEM_1_4_OR_NEWER)
        if (!removeOtherHMDDevices)
            return;

        switch (change)
        {
            case InputDeviceChange.Added:
                if (device is XRHMD && !(device is XRSimulatedHMD))
                    InputSystem.RemoveDevice(device);
                break;
        }
#endif
    }

    #endregion

    #region Override

    /// <summary>
    /// 保留base.ProcessPoseInput()的以下操作：
    /// -对手柄的控制
    /// -对相机的右键旋转
    /// 
    /// 修改技巧:
    /// -部分字段后期可改用属性，或者针对局部变量定义一个类似的字段
    /// -重新实现关键按键的Action监听(使用额外方法，不覆盖旧方法)
    /// </summary>

    float increasedValue = 1;//加速度
    float lastLeftControllerEulerZ;//（按住Grip时）左控制器累计的旋转值

    Vector3 scaledMouseDeltaInput;
    [SerializeField] float deviceDistanceToCamera = 0.15f;
    /// <summary>
    /// 设备位置输入（根据模拟器的键位输入）
    /// 
    /// PS：
    /// -QE只能升降【PC模式】中的HMD，而且针对重力模式不适用，暂时忽略(ToUpdate：改为【Fly模式】时移动Rig)
    /// </summary>
    protected override void ProcessPoseInput()
    {
        //base.ProcessPoseInput();//注释后可以随意移动Controller
        //// #Set tracked states（如果需要通过m_LeftControllerState来控制手柄，则需要激活以下代码块）
        //InputTrackingState m_HMDTrackingState_PosAndRot = InputTrackingState.Position | InputTrackingState.Rotation;
        //m_LeftControllerState.isTracked = true;
        ////m_RightControllerState.isTracked = false;
        //m_HMDState.isTracked = true;
        //m_LeftControllerState.trackingState = (int)m_HMDTrackingState_PosAndRot;
        ////m_RightControllerState.trackingState = (int)m_RightControllerTrackingState;
        //m_HMDState.trackingState = (int)m_HMDTrackingState_PosAndRot;

        bool isControllingHMD = m_TargetedDeviceInput.HasDevice(TargetedDevices.HMD);//按住右键会为m_TargetedDeviceInput临时添加HMD，从而修改HMD的旋转（PS：如果接入Gamepad，那么移动摇杆时每个方向也会触发ManipulateHead）

        scaledMouseDeltaInput =
           new Vector3(
               m_MouseDeltaInput.x * mouseXRotateSensitivity,
               m_MouseDeltaInput.y * mouseYRotateSensitivity * (m_MouseYRotateInvert ? 1f : -1f),
               m_MouseScrollInput.y * mouseScrollRotateSensitivity);//Mouse movement+scroll

        //按住LShift：加速各种变换
        increasedValue = m_SpeedUpInput ? (m_SpeedUpRapidClick ? 10 : 5) : 1;
        continuousMoveProviderBase.moveSpeed = increasedValue;//设置移动速度
        leftRayInteractor.translateSpeed = increasedValue;//抓住物体时，按下方向键的移动速度

        if (!IsHoveringUI)//以下操作仅鼠标不在UI上有效
        {
            ///滚轮：
            ///- 【正在抓取物体（可以监听是否按下左键）】：旋转手柄（会带动被抓取的物体）
            /// - 【正在旋转HMD】：调整FOV
            if (isRaySelectEntered)//正在抓取物体
            {
                lastLeftControllerEulerZ += scaledMouseDeltaInput.z * increasedValue;//根据滚轮旋转手柄Z轴
            }
            else//没有抓取物体
            {
                lastLeftControllerEulerZ = 0;//重置左手柄旋转值

                if (isControllingHMD && m_MouseScrollInput.y != 0)//按下右键+滚轮：控制FOV
                {
                    vrCamera.fieldOfView -= Mathf.Sign(m_MouseScrollInput.y) * _fovIncreaseSpeed * increasedValue;
                }
            }
        }

        //——Rig——
        //【飞行模式】按QE：升降Rig
        if (AD_ManagerHolder.XRManager.EnableFly)
        {
            float deltaPositionY = m_KeyboardYTranslateInput * m_KeyboardYTranslateSpeed * increasedValue * Time.deltaTime;
            tfCameraRigParent.Translate(Vector3.up * deltaPositionY, UnityEngine.Space.Self);
        }

        //——手柄——
        bool isModifyingSelectedObjEx = isRaySelectEntered && m_ManipulateSelectedObjectInput;//是否选中物体且按下修改物体的按键（X键）
        if (isModifyingSelectedObjEx)//抓取物体并按住X：禁止移动手柄（Todo：同时禁止旋转相机），且将鼠标位移转为对被抓取物体的旋转
        {
            //更改附着点的旋转，从而旋转被抓取物体
            Vector3 objectRotateValue = scaledMouseDeltaInput * increasedValue;
            leftRayInteractor.attachTransform.Rotate(leftRayInteractor.anchorRotateReferenceFrame.up, -objectRotateValue.x, UnityEngine.Space.World);
            leftRayInteractor.attachTransform.Rotate(leftRayInteractor.anchorRotateReferenceFrame.right, -objectRotateValue.y, UnityEngine.Space.World);
        }
        else
        {
            ///ToUpdate:
            ///-以下部分操作仅抓取物体时有效

            ///【手柄】:朝向鼠标在屏幕前方指定深度的焦点，保证与相机视野一致
            Vector3 mousePos = Input.mousePosition;
            Vector3 devicePosition = vrCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, deviceDistanceToCamera));
            Quaternion deviceRotation = Quaternion.LookRotation((devicePosition - tfCameraEye.position), tfCameraEye.up);

            tfLeftController.position = devicePosition;// tfCameraEye.position;//PS：要远离Rig的CharactorController碰撞体
            tfLeftController.rotation = deviceRotation;// tfCameraEye.rotation;

            //#根据鼠标滚轮的值旋转手柄的Z轴，从而带动对应物体
            tfLeftController.Rotate(new Vector3(0, 0, lastLeftControllerEulerZ), UnityEngine.Space.Self);//原版实现：修改局部Z轴，从而带动被抓取物体
        }



        //m_LeftControllerState.devicePosition = devicePosition;//PS:devicePosition/deviceRotation是相对于相机的坐标轴。为了方便计算，改为直接使用上面的更改Controller的世界坐标
        //m_LeftControllerState.deviceRotation = deviceRotation;

        //——Camera——
        if (isControllingHMD && !isModifyingSelectedObjEx)
        {
            Vector3 anglesDelta_Camera = new Vector3(scaledMouseDeltaInput.y, scaledMouseDeltaInput.x, 0f);//基于鼠标位移值旋转相机

            //anglesDelta += new Vector3(0f, 0f, scaledMouseDeltaInput.z);  // Scroll contribution【滚轴控制Z轴】
            m_CenterEyeEuler += anglesDelta_Camera;
            m_HMDState.deviceRotation = m_HMDState.centerEyeRotation = Quaternion.Euler(m_CenterEyeEuler);
        }
    }

    //bool tempOverrideSetCamera = false;//需要临时设置相机信息（可以避免ProcessPoseInput中将SetCamera的修改重置）
    /// <summary>
    /// PS:需要在Game窗口聚焦时有效，通过EventPlayer调用会无效
    /// </summary>
    /// <param name="localPosition">为了避免传送延迟导致位置出错，使用基于父物体的局部坐标</param>
    /// <param name="rotation">全局的旋转值，避免转换</param>
    public void SetCamera(Vector3? localPosition = null, Quaternion? rotation = null)
    {
        //暂时忽略位置，因为没有影响其位置的因素
        if (rotation.HasValue)
        {
            m_CenterEyeEuler = rotation.Value.eulerAngles;//更新缓存值
            m_HMDState.deviceRotation = m_HMDState.centerEyeRotation = rotation.Value;
        }
    }
    #endregion

    #region IAD_RuntimeEditor_ModeActiveHandler
    public void OnRuntimeEditorActiveChanged(bool isRuntimeEditorActive)
    {
        try
        {
            //进入【编辑模式】时隐藏所有Interactor组件(会强制Interactor掉落)。Interactor的OnDisable会强制掉落当前抓取物（https://forum.unity.com/threads/force-player-to-drop-xrgrabinteractable.1061039/#post-6995438）
            if (leftRayInteractor)//避免未初始化就进入该方法
                leftRayInteractor.enabled = !isRuntimeEditorActive;
        }
        catch (System.Exception e)
        {
            Debug.LogError("OnRuntimeEditorActiveChanged error:\r\n" + e);
        }
    }
    #endregion
}
