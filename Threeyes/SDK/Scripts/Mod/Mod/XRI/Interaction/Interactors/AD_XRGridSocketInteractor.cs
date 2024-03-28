using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
/// <summary>
/// Socket Interactor for holding a group of Interactables in a 2D grid.
/// 
/// Todo:
/// -【V2】被缩放后动态更新Grid子物体的相对位置以确保不变，以及针对Grid的配置新增Gid子物体
/// -新增设置gridParentTransform代表的锚点，方便Grid基于中心进行生成等
///
/// PS:
/// Grid生成参考UI，以gridParentTransform坐标系的XY轴为左下角：
///     -gridParentTransform作为Grid的父物体，其局部坐标系需要X为右，Y为上；
///     -通过gridRotationReference来控制Grid的朝向
/// 
/// Ref: UnityEngine.XR.Content.Interaction XRLockGridSocketInteractor+XRGridSocketInteractor
/// </summary>
public class AD_XRGridSocketInteractor : AD_XRSocketInteractor
{
    #region 修复Socket被占用后仍绘制无效Interactable的Mesh的问题
    protected override bool DrawCantHoverMaterialOnOccupied { get { return true; } }//因为 Grid数量/物体大小 不确定，而且为了提示用户将会放置的区域，所以允许一直绘制
    //protected override int socketSnappingLimit { get { return gridSize; } }//暂时不限制
    #endregion

    #region Ref: XRGridSocketInteractor
    /// <summary>
    /// The grid width. The grid width is along the Attach Transform's local X axis.
    /// </summary>
    public int gridWidth
    {
        get => m_GridWidth;
        set => m_GridWidth = Mathf.Max(1, value);
    }
    /// <summary>
    /// The grid height. The grid height is along the Attach Transform's local Y axis.
    /// </summary>
    public int gridHeight
    {
        get => m_GridHeight;
        set => m_GridHeight = Mathf.Max(1, value);
    }
    /// <summary>
    /// The distance (in local space) between cells in the grid.
    /// </summary>
    public Vector2 cellOffset
    {
        get => m_CellOffset;
        set => m_CellOffset = value;
    }
    /// <summary>
    /// (Read Only) The grid size. The maximum number of Interactables that this Interactor can hold.
    /// </summary>
    public int gridSize => m_GridWidth * m_GridHeight;
    bool hasEmptyAttachTransform => m_UnorderedUsedAttachedTransform.Count < gridSize;

    [Space]
    [Header("Grid Setting")]
    [Tooltip("The positioning of the grid reliative to gridParentTransform.")] [SerializeField] GridAnchor alignment = GridAnchor.MiddleCenter;
    [Tooltip("Parent of the grid.")] [SerializeField] Transform gridParentTransform;
    [Tooltip("[Optional] Rotation reference of the grid.")] [SerializeField] Transform gridRotationReference;

    [SerializeField]
    [Tooltip("The grid width. The grid width is along the Attach Transform's local X axis.")]
    [Min(1)] int m_GridWidth = 2;
    [SerializeField]
    [Tooltip("The grid height. The grid height is along the Attach Transform's local Y axis.")]
    [Min(1)] int m_GridHeight = 2;

    [SerializeField]
    [Tooltip("The distance (in local space) between cells in the grid.")]
    Vector2 m_CellOffset = new Vector2(0.1f, 0.1f);


    readonly HashSet<Transform> m_UnorderedUsedAttachedTransform = new HashSet<Transform>();
    readonly Dictionary<IXRInteractable, Transform> m_UsedAttachTransformByInteractable =
        new Dictionary<IXRInteractable, Transform>();

    Transform[,] m_Grid;


    protected override void InitFunc()
    {
        base.InitFunc();
        CreateGrid();

        // The same material is used on both situations
        interactableCantHoverMeshMaterial = interactableHoverMeshMaterial;
    }

    /// <inheritdoc />
    protected override void OnSelectEntering(SelectEnterEventArgs args)
    {
        if (m_UsedAttachTransformByInteractable.ContainsKey(args.interactableObject))//避免重复进入
            return;

        base.OnSelectEntering(args);

        var closestAttachTransform = GetAttachTransform(args.interactableObject);
        m_UnorderedUsedAttachedTransform.Add(closestAttachTransform);
        m_UsedAttachTransformByInteractable.Add(args.interactableObject, closestAttachTransform);
    }

    /// <inheritdoc />
    protected override void OnSelectExiting(SelectExitEventArgs args)
    {
        var closestAttachTransform = m_UsedAttachTransformByInteractable[args.interactableObject];
        m_UnorderedUsedAttachedTransform.Remove(closestAttachTransform);
        m_UsedAttachTransformByInteractable.Remove(args.interactableObject);

        base.OnSelectExiting(args);
    }


    public override bool CanHover(IXRHoverInteractable interactable)
    {
        if (!base.CanHover(interactable))//包括 CanUnlock 方法
            return false;

        return !m_UnorderedUsedAttachedTransform.Contains(GetAttachTransform(interactable));
    }

    public override bool CanSelect(IXRSelectInteractable interactable)
    {
        //与父类不同的实现（因为父类仅判断interactorsSelecting的数量是否为1）
        bool canSelecct = IsSelecting(interactable) || (hasEmptyAttachTransform && !interactable.isSelected && !m_UnorderedUsedAttachedTransform.Contains(GetAttachTransform(interactable)));
        if (!canSelecct)
            return false;

        return CanUnlock(interactable);
    }

    public override Transform GetAttachTransform(IXRInteractable interactable)
    {
        if (m_UsedAttachTransformByInteractable.TryGetValue(interactable, out var interactableAttachTransform))
            return interactableAttachTransform;

        var interactableLocalPosition = gridParentTransform.InverseTransformPoint(interactable.GetAttachTransform(this).position);
        interactableLocalPosition -= GetPivotOffset();
        var i = Mathf.RoundToInt(interactableLocalPosition.x / m_CellOffset.x);
        var j = Mathf.RoundToInt(interactableLocalPosition.y / m_CellOffset.y);
        i = Mathf.Clamp(i, 0, m_GridWidth - 1);
        j = Mathf.Clamp(j, 0, m_GridHeight - 1);
        return m_Grid[i, j];
    }


    /// <summary>
    /// Creates the grid base on gridParentTransform
    /// </summary>
    void CreateGrid()
    {
        m_Grid = new Transform[m_GridWidth, m_GridHeight];

        //基于局部坐标生成网格
        for (var i = 0; i < m_GridWidth; i++)
        {
            for (var j = 0; j < m_GridHeight; j++)
            {
                var attachTransformInstance = new GameObject($"[{gameObject.name}] Attach ({i},{j})").transform;
                attachTransformInstance.SetParent(gridParentTransform, false);

                //var localOffset = new Vector3(i * m_CellOffset.x, j * m_CellOffset.y, 0f);

                attachTransformInstance.localPosition = GetGridLocaPos(i, j);// localOffset;

                attachTransformInstance.rotation = gridRotationReference ? gridRotationReference.rotation : gridParentTransform.rotation;//【新增】：设置附着点的旋转，用于控制附着物体的旋转

                m_Grid[i, j] = attachTransformInstance;
            }
        }
    }

    /// <summary>
    /// (i,j)=>LocalPoint
    /// </summary>
    /// <param name="i"></param>
    /// <param name="j"></param>
    /// <returns></returns>
    Vector3 GetGridLocaPos(int i, int j)
    {
        Vector3 localOffset = new Vector3(i * m_CellOffset.x, j * m_CellOffset.y, 0f);//与原点的位移
        return localOffset + GetPivotOffset();
    }

    /// <summary>
    /// 计算parent锚点基于alignment枚举的原点
    /// </summary>
    /// <returns></returns>
    Vector3 GetPivotOffset()
    {
        ///Todo:
        ///-通过Transform转换位置在parent的局部坐标，然后继续计算
        Vector2 originOffsetToPivot = Vector2.zero;//与原点（LowerLeft）的位移
        Vector2 areaSize = new Vector2((gridWidth - 1) * m_CellOffset.x, (gridHeight - 1) * m_CellOffset.y);
        switch (alignment)
        {
            case GridAnchor.UpperLeft: originOffsetToPivot = new Vector2(0, -areaSize.y); break;
            case GridAnchor.UpperCenter: originOffsetToPivot = new Vector2(-areaSize.x / 2, -areaSize.y); break;
            case GridAnchor.UpperRight: originOffsetToPivot = new Vector2(-areaSize.x, -areaSize.y); break;

            case GridAnchor.MiddleLeft: originOffsetToPivot = new Vector2(0, -areaSize.y / 2); break;
            case GridAnchor.MiddleCenter: originOffsetToPivot = new Vector2(-areaSize.x / 2, -areaSize.y / 2); break;
            case GridAnchor.MiddleRight: originOffsetToPivot = new Vector2(-areaSize.x, -areaSize.y / 2); break;

            case GridAnchor.LowerLeft: break;//对应Pivot
            case GridAnchor.LowerCenter: originOffsetToPivot = new Vector2(-areaSize.x / 2, 0); break;
            case GridAnchor.LowerRight: originOffsetToPivot = new Vector2(-areaSize.x, 0); break;
        }
        return originOffsetToPivot;
    }
    #region Editor

    /// <summary>
    /// See <see cref="MonoBehaviour"/>.
    /// </summary>
    protected override void OnValidate()
    {
        base.OnValidate();

        if (!gridParentTransform)
            gridParentTransform = attachTransform;
    }


    public bool drawGizmosOnChildSelected = true;
    public float gizmoOccupySphereSize = 0.05f;
    protected virtual void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (!drawGizmosOnChildSelected)//选择该物体或子物体时才绘制
            return;

        GameObject[] selectedGOs = UnityEditor.Selection.gameObjects;
        foreach (var go in selectedGOs)
        {
            if (go.transform.IsChildOf(transform))//任意选中物体是该物体，或者该物体的子物体
            {
                DrawGizmosFunc();
                break;
            }
        }
#endif
    }
    /// <summary>
    /// See <see cref="MonoBehaviour"/>.
    /// </summary>
    protected virtual void OnDrawGizmosSelected()
    {
        DrawGizmosFunc();
    }

    private void DrawGizmosFunc()
    {
        Gizmos.matrix = gridParentTransform != null ? gridParentTransform.localToWorldMatrix : transform.localToWorldMatrix;
        for (var i = 0; i < m_GridWidth; i++)
        {
            for (var j = 0; j < m_GridHeight; j++)
            {
                if (i == 0 && j == 0)//使用蓝色颜色绘制起始点
                    Gizmos.color = Color.blue;
                else//使用绿色绘制其他点
                    Gizmos.color = Color.green;

                //var currentPosition = new Vector3(i * m_CellOffset.x, j * m_CellOffset.y, 0f);
                var currentPosition = GetGridLocaPos(i, j);//new Vector3(i * m_CellOffset.x, j * m_CellOffset.y, 0f);
                //在点的两侧额外绘制线条，
                Gizmos.DrawLine(currentPosition + (Vector3.left * m_CellOffset.x * 0.5f), currentPosition + (Vector3.right * m_CellOffset.y * 0.5f));
                Gizmos.DrawLine(currentPosition + (Vector3.down * m_CellOffset.x * 0.5f), currentPosition + (Vector3.up * m_CellOffset.y * 0.5f));
            }
        }

        ///ToAdd:
        ///-运行时绘制被占用的点
        if (Application.isPlaying)
        {
            Gizmos.matrix = Matrix4x4.identity;//恢复世界坐标轴
            Gizmos.color = Color.red;
            foreach (var tfGrid in m_UnorderedUsedAttachedTransform)
            {
                Gizmos.DrawWireSphere(tfGrid.position, gizmoOccupySphereSize);
            }
        }
    }

    #endregion

    #endregion

    #region Define
    /// <summary>
    /// gridParentTransform 所对应的锚点
    /// </summary>
    public enum GridAnchor
    {
        //
        // 摘要:
        //     Text is anchored in upper left corner.
        UpperLeft,
        //
        // 摘要:
        //     Text is anchored in upper side, centered horizontally.
        UpperCenter,
        //
        // 摘要:
        //     Text is anchored in upper right corner.
        UpperRight,
        //
        // 摘要:
        //     Text is anchored in left side, centered vertically.
        MiddleLeft,
        //
        // 摘要:
        //     Text is centered both horizontally and vertically.
        MiddleCenter,
        //
        // 摘要:
        //     Text is anchored in right side, centered vertically.
        MiddleRight,
        //
        // 摘要:
        //     Text is anchored in lower left corner.
        LowerLeft,
        //
        // 摘要:
        //     Text is anchored in lower side, centered horizontally.
        LowerCenter,
        //
        // 摘要:
        //     Text is anchored in lower right corner.
        LowerRight
    }
    #endregion
}
