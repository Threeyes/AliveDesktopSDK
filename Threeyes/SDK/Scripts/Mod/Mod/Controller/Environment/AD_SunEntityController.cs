using System.Collections;
using System.Collections.Generic;
using Threeyes.Core;
using Threeyes.RuntimeEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
/// <summary>
/// 太阳实体（因为与相机相关，所以一个场景只能激活一个）
/// 
/// PS:
/// -【非必须，可以用普通静态物体代替，因为与相机无关】该组件只能有一个，但是可以提炼出通用的基类（如天体），方便用户添加月亮、恒星等物体
/// 
/// Todo：
/// -改名为AD_SunEntityController
/// 
/// 功能：
///     -【普通模式】：太阳会根据Config的设置，随着主相机的而同步移动，保证在不同位置观察天阳，其都与天空盒的太阳位置相同（监听Config设置的回调）
///     -【拖拽太阳时】：同步更新灯光位置及Config的值（原理就是一个无限远的球体）
/// 
/// Warning：
/// -如需使用太阳实体代替ProceduralSkybox材质中的假太阳，需要把skybox材质中SunSize设置为0来隐藏假太阳，否则可能会出现两个太阳（Todo：后续在EnvironmentController的Config中加上）
/// 
/// PS：
/// -用户可以抓取并缩放太阳
/// -尽量通用，方便后续有多个实例
/// </summary>
public class AD_SunEntity : MonoBehaviour
    , IRuntimeEditorSelectEnterHandler
    , IRuntimeEditorSelectExitHandler
{
    /// <summary>
    /// XR抓取中或RuntimeEditor编辑中
    /// </summary>
    public bool IsEditing { get { return isEditing; } }
    private bool isEditing = false;

    public XRBaseInteractable interactable;//[Optional] The real sun that user can drag, which will affect sunSourceLight. (PS: Place it very far away to avoid parallax issues during movement. Set Procedural Skybox Material's [SunSize] to zero to avoid two sun)

    //[Optional]
    public Renderer rendererMesh;
    public string shaderProperty_BaseColorName = "_BaseColor";
    public string shaderProperty_EmissionColorName = "_EmissionColor";

    public ColorEvent onBaseColorChanged = new ColorEvent();
    public ColorEvent onEmissionColorChanged = new ColorEvent();
    public FloatEvent onIntensityChanged = new FloatEvent();
    public UnityEvent onSelectEntered = new UnityEvent();
    public UnityEvent onSelectExited = new UnityEvent();

    //#Runtime
    public Material rendererMaterial;
    int baseColorID;
    int emissionColorID;
    private void Awake()
    {
        if (interactable)
        {
            interactable.selectEntered.AddListener(OnXRSelectEntered);
            interactable.selectExited.AddListener(OnXRSelectExited);
        }

        if (rendererMesh)
            rendererMaterial = rendererMesh.material;
        baseColorID = Shader.PropertyToID(shaderProperty_BaseColorName);
        emissionColorID = Shader.PropertyToID(shaderProperty_EmissionColorName);
    }

    #region Public
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

    #region XRI
    public virtual void SetInteractable(bool isEnable)
    {
        interactable.enabled = isEnable;
    }

    //被任意Interactor或Socket控制时，都代表在编辑中
    void OnXRSelectEntered(SelectEnterEventArgs args)
    {
        isEditing = true;
        onSelectEntered.Invoke();
    }
    void OnXRSelectExited(SelectExitEventArgs args)
    {
        isEditing = false;
        onSelectExited.Invoke();
    }
    #endregion

    #region RuntimeEditor
    public void OnRuntimeEditorSelectEntered(RESelectEnterEventArgs args)
    {
        isEditing = true;
        onSelectEntered.Invoke();
    }
    public void OnRuntimeEditorSelectExited(RESelectExitEventArgs args)
    {
        isEditing = false;
        onSelectExited.Invoke();
    }
    #endregion
}
