using System.Collections;
using System.Collections.Generic;
using Threeyes.Core;
using Threeyes.RuntimeEditor;
using Threeyes.GameFramework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using NaughtyAttributes;
using Threeyes.Localization;
/// <summary>
/// 太阳实体（因为与相机相关，所以一个场景只能激活一个）
/// 
/// PS:
/// -【非必须，可以用普通静态物体代替，因为与相机无关】该组件只能有一个，但是可以提炼出通用的基类（如天体），方便用户添加月亮、恒星等物体
/// 
/// Todo：
/// +改名为AD_SunEntityController
/// +如果该实例已经由EnvironmentController控制，则禁止其自动注册（在注册时进行排除）
/// -如果添加了Custom，那么会把其他禁用（包括EnvironmentController链接的，在该组件里实现禁用代码，可以是隐藏物体）
/// -如果添加了多个Custom，那么会弹出警告（跟SkyboxController一同处理）
/// 
/// 功能：
///     -【普通模式】：太阳会根据Config的设置，随着主相机的而同步移动，保证在z不同位置观察天阳，其都与天空盒的太阳位置相同（监听Config设置的回调）
///     -【拖拽太阳时】：同步更新灯光位置及Config的值（原理就是一个无限远的球体）
/// 
/// Warning：
/// -如需使用太阳实体代替ProceduralSkybox材质中的假太阳，需要把skybox材质中SunSize设置为0来隐藏假太阳，否则可能会出现两个太阳（Todo：后续在EnvironmentController的Config中加上）
/// -Material：
///     -设置为（Particle/Lit）可以避免出现明暗边界
/// -MeshRenderer应该：
///     -CaseShadows设置为Off
/// -如果使用AD_XRGrabInteractable作为抓取，还需要（后期增加一个专用的顶部菜单项）：
///     -将Rigidbody设置为IsKinematic
///     -将AD_XRGrabInteractable.ThrowOnDetach设置为false，否则会因为Kinematic而弹出警告
/// 
/// PS：
/// -用户可以向普通XR物体一样抓取并缩放太阳
/// -如果需要实现三体等特殊效果，可以在子物体中加上多个模型
/// </summary>
public class AD_SunEntityController : MonoBehaviour
    , IRuntimeHierarchyItemProvider
    , IRuntimeEditorSelectEnterHandler
    , IRuntimeEditorSelectExitHandler
{
    /// <summary>
    /// XR抓取中或RuntimeEditor编辑中
    /// </summary>
    public bool IsEditing { get { return isEditing; } }
    private bool isEditing = false;

    [Required] public GameObject goRoot;//Root gameobject for renders

    //[Optional]
    public XRBaseInteractable interactable;//Use this to drag and affect sunSourceLight. (PS: Place it very far away to avoid parallax issues during movement. Set Procedural Skybox Material's [SunSize] to zero to avoiding the coexistence of two types of suns in a scene)
    public Renderer rendererMesh;
    public string shaderProperty_BaseColorName = "_BaseColor";
    public string shaderProperty_EmissionColorName = "_EmissionColor";

    public ColorEvent onBaseColorChanged = new ColorEvent();
    public ColorEvent onEmissionColorChanged = new ColorEvent();
    public FloatEvent onIntensityChanged = new FloatEvent();
    public UnityEvent onSelectEntered = new UnityEvent();
    public UnityEvent onSelectExited = new UnityEvent();

    //#Runtime
    Material rendererMaterial;
    int baseColorID;
    int emissionColorID;

    #region Init
    private void Awake()
    {
        baseColorID = Shader.PropertyToID(shaderProperty_BaseColorName);
        emissionColorID = Shader.PropertyToID(shaderProperty_EmissionColorName);
    }
    private void Start()//等待Ghost点击摆放后才生效，避免用户移动Gizmo看不见该物体，以为出Bug
    {
        //Todo：如果是模拟器，则等待初始化完成
        if (interactable)
        {
            interactable.selectEntered.AddListener(OnXRSelectEntered);
            interactable.selectExited.AddListener(OnXRSelectExited);
        }

        if (rendererMesh)
            rendererMaterial = rendererMesh.material;

        cacheEnum_Init = CoroutineManager.StartCoroutineEx(IEInit());
    }
    private void OnDestroy()
    {
        TryStopCoroutine_Init();
        if (!hasInit)
            return;
        if (AD_ManagerHolder.EnvironmentManager == null)
            return;
        AD_ManagerHolder.EnvironmentManager.ActiveController.UnRegisterSunEntityController(this);
    }

    bool hasInit = false;
    protected Coroutine cacheEnum_Init;
    IEnumerator IEInit()
    {
        if (ManagerHolder.SceneManager == null)
            yield break;
        while (ManagerHolder.SceneManager.IsChangingScene)//等待场景初始化完成（主要是ActiveController被初始化）
            yield return null;

        if (AD_ManagerHolder.EnvironmentManager == null)
            yield break;
        AD_ManagerHolder.EnvironmentManager.ActiveController.RegisterSunEntityController(this);
        hasInit = true;
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

    #region Public
    public void SetActive(bool isActive)
    {
        goRoot.SetActive(isActive);
    }

    public void SetHDRColor(Color hdrColor)
    {
        Color32 basicColor;
        float intensity;
        ColorTool.DecomposeHdrColor(hdrColor, out basicColor, out intensity);//分离出基础颜色和亮度

        SetColorAndIntensity(basicColor, hdrColor, intensity);
    }

    public void SetColorAndIntensity(Color32 baseColor, Color emissionColor, float intensity)
    {
        if (rendererMaterial.HasProperty(baseColorID))
            rendererMaterial.SetColor(baseColorID, baseColor);//需要同时设置基础颜色，否则会出现与Emission颜色不一致或黑色无法呈现的问题
        if (rendererMaterial.HasProperty(emissionColorID))
            rendererMaterial.SetColor(emissionColorID, emissionColor);

        //通过回调发送这些参数，方便通过装饰添加的AD_SunEntity通过RenderHelper更新自定义材质
        onBaseColorChanged.Invoke(baseColor);
        onEmissionColorChanged.Invoke(emissionColor);
        onIntensityChanged.Invoke(intensity);
    }
    #endregion

    #region XRI（抓取）
    public virtual void SetInteractable(bool isEnable)
    {
        interactable.enabled = isEnable;
    }

    //被任意Interactor或Socket控制时，都代表在编辑中
    void OnXRSelectEntered(SelectEnterEventArgs args)
    {
        isEditing = true;
        FireSelectEnterExitEvent(true);
    }

    void OnXRSelectExited(SelectExitEventArgs args)
    {
        isEditing = false;
        FireSelectEnterExitEvent(false);
    }
    #endregion

    #region RuntimeEditor（移动、缩放等变换）
    public void OnRuntimeEditorSelectEntered(RESelectEnterEventArgs args)
    {
        isEditing = true;
        FireSelectEnterExitEvent(true);
    }
    public void OnRuntimeEditorSelectExited(RESelectExitEventArgs args)
    {
        isEditing = false;
        FireSelectEnterExitEvent(false);
    }
    #endregion

    #region IRuntimeHierarchyItemProvider
    public RuntimeHierarchyItemInfo GetRuntimeHierarchyItemInfo()
    {
        RuntimeHierarchyItemInfo runtimeHierarchyItemInfo = new RuntimeHierarchyItemInfo();
        IAD_EnvironmentController environmentController = AD_ManagerHolder.EnvironmentManager.ActiveController;//因为只有Hub才会回调该方法，因此不用判断是否为空
        if (environmentController.SunEntityControllerCount > 1 && environmentController.ActiveSunEntityController != this)
        {
            runtimeHierarchyItemInfo.warningTips = LocalizationManagerHolder.LocalizationManager.GetTranslationText("RuntimeEditor/Hierarchy/OnlyOneCanExistInTheScene");//"Only one such object can exist in the scene at the same time, and this object will not take effect!";//Todo：多语言翻译：场景只能同时存在一个此类物体，该物体不会生效！
        }

        return runtimeHierarchyItemInfo;
    }
    #endregion

    void FireSelectEnterExitEvent(bool isEnter)
    {
        if (isEnter)
        {
            onSelectEntered.Invoke();
        }
        else
        {
            onSelectExited.Invoke();

            //编辑完成后，不管移动了多少距离，都强制刷新ReflectionProbe（ToUpdate：只有当前天空盒为Procedural才需要更新，因为主要刷新太阳的位置，SunEntity不是静态物体所以不会被烘焙到ReflectionProbe）
            if (ManagerHolder.EnvironmentManager != null)
                ManagerHolder.EnvironmentManager.BaseActiveController.RefreshReflectionProbe();
        }
    }
}
