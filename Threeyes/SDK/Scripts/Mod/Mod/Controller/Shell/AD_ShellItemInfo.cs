using Newtonsoft.Json;
using Threeyes.Core;
using Threeyes.RuntimeEditor;
using UnityEngine;

/// <summary>
/// 通用的FileSystemItem数据
/// 
/// ToUpdate:
/// +扫描后统一传入该数据类，给Controller初始化（调用其具体ItemInfo的构造函数方法，就能转为对应的数据）
/// +将关键属性弄成只读，避免其他人修改。Texture等运行时属性可以读写
/// +移动到通用地方
/// -尝试仅提供接口给Modder
/// </summary>
[System.Serializable]
[RuntimeEditorObject(MemberConsideration.OptIn)]
public class AD_ShellItemInfo : AD_SerializableItemInfo
    , IAD_ShellItemInfo
{
    public string Name { get { return name; } }
    public AD_ShellType DestShellType { get { return ItemShellType == AD_ShellType.Link ? targetShellType : ItemShellType; } }//最终目标的Shell类型
    public AD_ShellType ItemShellType { get { return itemShellType; } }
    public AD_ShellType TargetShellType { get { return targetShellType; } }
    public AD_SpeicalFolder SpecialFolder { get { return specialFolder; } }
    public string Path { get { return path; } }
    public string TargetPath { get { return targetPath; } }
    public Texture2D TexturePreview { get { return texturePreview; } }

    //#Runtime
    public string DisplayName { get { return Name; } }  //Todo:根据用户设置，决定返回的内容是否带后缀
    public string NameWithoutExtension { get { return Name.GetFileNameWithoutExtension(); } }

    [RuntimeEditorProperty] [RuntimeEditorReadOnly] [SerializeField] string name;//文件名
    [SerializeField] AD_ShellType itemShellType = AD_ShellType.File;//自身的类型
    [SerializeField] string path;//文件所在路径
    [SerializeField] AD_SpeicalFolder specialFolder = AD_SpeicalFolder.None;//特殊文件夹的类型（如果不是SpeicalFolder则设置为None）
    [SerializeField] AD_ShellType targetShellType = AD_ShellType.None;//(如果Item为Link有效）Link目标的类型，决定打开方式等。注意该值一般不为Link，因为嵌套的Link会直接指向最终目标
    [SerializeField] string targetPath;//(如果Item为Link有效）Link目标路径
    //public string arguments;//快捷方式的参数。ToDelete：因为直接打开Lnk，所以不需要再读其参数

    [JsonIgnore] public Texture2D texturePreview;//预览图标（实时读取）（Warning：不能标记为SerializeField，否则会被Json序列化导致报错！）

    public AD_ShellItemInfo()
    {
    }

    /// <summary>
    /// PS：一般调用这个构造函数的都是通过子类构造函数加上基类实例转换而成，所以将IsBaseType标记为基类
    /// </summary>
    /// <param name="otherInst"></param>
    public AD_ShellItemInfo(AD_ShellItemInfo otherInst)
    {
        CopyBaseMembersFromFunc(otherInst);
    }
    public AD_ShellItemInfo(string name, AD_ShellType itemShellType, string selfPath, AD_SpeicalFolder specialFolder = AD_SpeicalFolder.None, AD_ShellType targetShellType = AD_ShellType.None, string targetPath = "", Texture2D texturePreview = null)
    {
        this.name = name;
        this.itemShellType = itemShellType;
        this.path = selfPath;
        this.specialFolder = specialFolder;

        this.targetShellType = targetShellType;
        this.targetPath = targetPath;

        this.texturePreview = texturePreview;
    }

    public override void CopyBaseMembersFrom(object otherInst)
    {
        if (otherInst is AD_ShellItemInfo otherItemInfo)
            CopyBaseMembersFromFunc(otherItemInfo);
    }
    void CopyBaseMembersFromFunc(AD_ShellItemInfo otherInst)
    {
        name = otherInst.name;
        itemShellType = otherInst.itemShellType;
        targetShellType = otherInst.targetShellType;
        specialFolder = otherInst.specialFolder;
        path = otherInst.path;
        targetPath = otherInst.targetPath;
        texturePreview = otherInst.texturePreview;
    }


    public override void DestroyRuntimeAssets()
    {
        ///Todo：需要检查图像是否为项目资源，还是运行时加载资源（查看相关Runtime方法）
        if (texturePreview)
            Object.Destroy(texturePreview);
    }

    #region Override 【ToUpdate：部分放到父类】
    public override string ToString()
    {
        return Name;
    }

    /// <summary>
    /// PS：可以用该字符来代表任意文件夹的本地持久化名称
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        //#1 获取代表该数据的唯一字符
        string rawString = "";
        switch (ItemShellType)
        {
            case AD_ShellType.SpecialFolder:
                rawString = SpecialFolder.ToString(); break;
            default:
                rawString = Path; break;
        }

        //#2 使用32位Hash Code算法来获取string对应的UID（https://stacktuts.com/how-to-generate-unique-integer-from-string-in-c）
        //需要注意的是，返回值不能保证唯一，但该方法高效且碰撞几率低，可以先使用，影响不大（https://social.msdn.microsoft.com/Forums/vstudio/en-US/ae00f987-b12d-4866-9571-b82f583afafe/is-stringgethashcode-unique?forum=clr）
        return GetHashCode(rawString);
    }

    /// <summary>
    /// 基于文件夹的唯一路径，计算出唯一ID
    /// </summary>
    /// <param name="targetPath"></param>
    /// <returns></returns>
    public static int GetHashCode(string targetPath)
    {
        return targetPath.GetHashCode();
    }

    /// <summary>
    /// PS：优先使用该方法来判断而不是static==，因为该方法支持继承类比较
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public override bool Equals(object other)
    {
        //Check for null and compare run-time types.
        if ((other == null)/* || !GetType().Equals(other.GetType())*/)//-只判断关键字段，不直接判断引用类型（因为可能是不同实例）
            return false;
        else if (!(other is AD_ShellItemInfo))//非本类型
            return false;
        else
            return EqualsFunc((AD_ShellItemInfo)other);
    }
    /// <summary>
    /// 值是否相同
    /// 
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    protected virtual bool EqualsFunc(AD_ShellItemInfo other)
    {
        if (other == null)
            return false;

        //#1 判断类型是否一致
        if (itemShellType != other.itemShellType || targetShellType != other.targetShellType)//PS:名称不作为判断标准，因为是从路径转换而成，而且可能有本地化的区别
            return false;

        //#2 判断SpecialFolder：只比对枚举
        if (itemShellType == AD_ShellType.SpecialFolder)
            return specialFolder == other.specialFolder;


        //#3 判断普通类型的路径
        if (path.IsNullOrEmpty() || other.path.IsNullOrEmpty())
            return false;
        return path.Equals(other.path);
    }
    public static bool operator ==(AD_ShellItemInfo x, AD_ShellItemInfo y)
    {
        //可避免空引用：  https://stackoverflow.com/questions/4219261/overriding-operator-how-to-compare-to-null
        if (ReferenceEquals(x, null))
        {
            return ReferenceEquals(y, null);
        }

        return x.Equals(y);
    }
    public static bool operator !=(AD_ShellItemInfo x, AD_ShellItemInfo y)
    {
        return !x == y;
    }
    public static implicit operator bool(AD_ShellItemInfo exists)
    {
        return !(exists == null);
    }
    #endregion
}