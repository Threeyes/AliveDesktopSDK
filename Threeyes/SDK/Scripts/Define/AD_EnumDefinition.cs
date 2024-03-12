using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#region Shell
/// <summary>
/// 对应目标在Shell中的类型
/// 
/// 功能：
/// -决定打开方式
/// </summary>
[System.Flags]
public enum AD_ShellType
{
    None = 0,

    Link = 1 << 0,//快捷方式（.lnk）
    File = 1 << 1,
    Folder = 1 << 2,//（可以拆分为ShellContainer，其中包含ShellFolder、ShellSearchCollection、ShellLibrary）
    SpecialFolder = 1 << 3,//特殊文件夹（如此电脑、回收站），可能需要其他打开方式

    All = ~0
}

/// <summary>
/// 筛选出的常见特殊文件夹（包括Virtual Folder），后期可以添加
/// PS：
/// -筛选出常用的枚举（桌面图标、库）
/// 
/// ToDo:
/// -通过Attribute标记虚拟路径的文件夹
/// -兼容其他平台（如Library、Picture等）
/// </summary>
[System.Flags]
public enum AD_SpeicalFolder
{
    None = 0,//Not a SpeicalFolder

    //——Desktop Folders （桌面可显示的文件夹）——
    ControlPanel = 1 << 0,//[Virtual Folder]控制面板
    RecycleBin = 1 << 1,//[Virtual Folder]回收站
    UserProfile = 1 << 2,//{C:\Users\[username]} User's Files（PS：代码返回的路径文件夹只显示真实的名称二不是用户账号名称，不算Bug）
    MyComputer = 1 << 3,//[Virtual Folder]
    Network = 1 << 4,//[Virtual Folder]

    All = ~0
}

/// <summary>
/// 有真实物理路径的特殊文件夹，供用户设置
/// 
/// 用途：
/// -打开对应的路径
///PS:
/// -提供默认打开文件夹的枚举，
/// -通过Environment.GetFolderPath来获取
/// </summary>
//[System.Flags]
public enum AD_PhysicsSpeicalFolder
{
    Custom = 0,//自定义文件夹路径
    Desktop,//包含个人及公共桌面 （UID：-1840468049）
    MyDocuments,//（UID：292589685）
    MyMusic,//（UID：-772719632）
    MyVideos,//（UID：993157279）
    MyPictures,//（UID：1519376662）
}

///// <summary>
///// Ref: CSIDL (constant special item ID list), values provide a unique system-independent way to identify special folders used frequently by applications, but which may not have the same name or location on any given system. (https://learn.microsoft.com/en-us/windows/win32/shell/csidl)
///// 
///// PS：
///// -因为枚举值不是Flag所以弃用
///// -筛选出桌面常见的枚举
///// 
///// ToDo:
///// -通过Attribute标记虚拟路径的文件夹
///// </summary>
//public enum AD_SpeicalFolder : int
//{
//    None = -1,//Not SpeicalFolder

//    ControlPanel = 3,//[Virtual Folder]
//    RecycleBin = 10,//[Virtual Folder]回收站
//    UserProfile = 40,//{C:\Users\[username]} User's Files（PS：代码返回的路径文件夹只显示真实的名称二不是用户账号名称，不算Bug）
//    MyComputer = 17,//[Virtual Folder]
//    Network = 18,//[Virtual Folder]

//    ////——Wasted——
//    //Desktop = 0,//The [virtual] folder that represents the Windows desktop, the root of the namespace.
//    //MyDocuments= 12,
//    //MyMusic = 13,
//    //MyVideos = 14,
//    
//    //InternetExplorer = 1,
//    //Programs = 2,
//    //Printers = 4,
//    //Personal = 5,
//    //Favorites = 6,
//    //Startup = 7,
//    //Recent = 8,
//    //SendTo = 9,
//    //StartMenu = 11,
//    //DesktopDirectory = 0x10,//
//    //NetworkShortcuts = 19,
//    //Fonts = 20,
//    //Templates = 21,
//    //CommonStartMenu = 22,
//    //CommonPrograms = 23,
//    //CommonStartup = 24,
//    //CommonDesktopDirectory = 25,
//    //ApplicationData = 26,
//    //PrinterShortcuts = 27,
//    //LocalApplicationData = 28,
//    //AltStartup = 29,//The file system directory that corresponds to the user's nonlocalized Startup program group. This value is recognized in Windows Vista for backward compatibility, but the folder itself no longer exists.(该文件夹不存在)
//    //CommonAltStartup = 30,
//    //CommonFavorites = 0x1F,//The file system directory that serves as a common repository for favorite items common to [all users]
//    //InternetCache = 0x20,
//    //Cookies = 33,
//    //History = 34,
//    //CommonApplicationData = 35,
//    //Windows = 36,
//    //System = 37,
//    //ProgramFiles = 38,
//    //MyPictures = 39,
//    //SystemX86 = 41,
//    //ProgramFilesX86 = 42,
//    //CommonProgramFiles = 43,
//    //CommonProgramFilesX86 = 44,
//    //CommonTemplates = 45,
//    //CommonDocuments = 46,
//    //CommonAdminTools = 47,
//    //AdminTools = 48,
//    //Connections = 49,
//    //CommonMusic = 53,
//    //CommonPictures = 54,
//    //CommonVideos = 55,
//    //Resources = 56,
//    //LocalizedResources = 57,
//    //CommonOemLinks = 58,
//    //CDBurnArea = 59,
//    //ComputersNearMe = 61,
//    ////FLAG_CREATE = 0x8000,
//    ////FLAG_DONT_VERIFY = 0x4000,
//    ////FLAG_DONT_UNEXPAND = 0x2000,
//    ////FLAG_NO_ALIAS = 0x1000,
//    ////FLAG_PER_USER_INIT = 0x8000,
//    ////FLAG_MASK = 65280

//    Any = 65535//Any SpeicalFolder, Mainly used for PrefabInfo
//}


#endregion

#region Workshop Item
//——Custom Tags——

/// <summary>
///Item种类：
///     Scene
///     Model（如FileSystem或Decoration等Item，不拆分，那通过Group来统一加载，作为一个系列）
/// </summary>
public enum AD_WSItemType
{
    Scene,
    Model
}

/// <summary>
/// 年龄等级【必选】【在Manager中为单选，但可以进行组合】（Ref： https://rating-system.fandom.com/wiki/Australian_Classification_Board）
/// </summary>
[System.Flags]
public enum AD_WSItemAgeRating
{
    //None = 0,

    General = 1 << 0,//Suitable for everyone
    ParentalGuidance = 1 << 1,// Suggested for younger viewers
    Restricted = 1 << 2,//Children Under XX Require Accompanying Parent or Adult Guardian.

    //All = ~0//-1
}

/// <summary>
/// Item风格/主题
/// </summary>
[System.Flags]
public enum AD_WSItemStyle
{
    None = 0,

    Animal = 1 << 0,//动物（如猫、爬虫）
    Human = 1 << 1,//人物（如武士）
    Botany = 1 << 2,//植物（如花草）
    Food = 1 << 3,//食物（如香蕉）
    Gear = 1 << 4,//机械（如齿轮）
    Vehicle = 1 << 5,//交通工具（如车、船）
    Weapon = 1 << 6,//武器（如枪械、弹弓）
    Sport = 1 << 7,//体育（如篮球/足球）

    //——【ToUpdate】以下为新增，需要在Steam中更新多语言——
    Nature = 1 << 8,//自然（如野外）
    //ToAdd：风景/风景、室内等

    All = ~0//-1
}

/// <summary>
/// Item类型
/// Ref：https://www.imdb.com/feature/genre
/// </summary>
[System.Flags]
public enum AD_WSItemGenre
{
    None = 0,

    Comedy = 1 << 0,//喜剧
    Scifi = 1 << 1,//科幻（Science fiction often takes place in a dystopian society sometime in the future and contains elements of advanced technology.）【反乌托邦（如银翼杀手）】
    Horror = 1 << 2,//恐怖（如爬虫、猎头蟹 ）
    Romance = 1 << 3,//浪漫
    Action = 1 << 4,//动作
    Mystery = 1 << 5,//神秘
    Crime = 1 << 6,//犯罪
    Animation = 1 << 7,//动画片
    Adventure = 1 << 8,//冒险
    Fantasy = 1 << 9,//幻想。异兽及超自然能力（如指环王、哈利波特）（usually set in the fantasy realm and includes mythical creatures and supernatural powers.）
    SuperHero = 1 << 10,//超级英雄

    All = ~0
}

/// <summary>
/// Item引用（致敬）
/// </summary>
[System.Flags]
public enum AD_WSItemReference
{
    None = 0,

    Game = 1 << 0,//游戏（如Halo）
    Movie = 1 << 1,//电影（如Wall-E）
    Cartoon = 1 << 2,//卡通（如Futurama）
    Book = 1 << 3,//书籍（如西游记）
    Celebrity = 1 << 4,//名人（如鲁迅）
    Software = 1 << 5,//软件（如Mac）
    Festival = 1 << 6,//节日（如新年）
    All = ~0
}

/// <summary>
/// 物体功能特性（多选）
/// </summary>
[System.Flags]
public enum AD_WSItemFeature
{
    None = 0,
    Interactable = 1 << 0,//可交互（碰撞）（如气球、弹球、扫雷、液体）
    AudioVisualizer = 1 << 1,//音频可视化
    Clock = 1 << 2,//时钟
    Exhibition = 1 << 3,//展示用途（如条幅、官方或第三方制作的用于展示及循环播放的广告）
    Experimental = 1 << 10,//实验性
    All = ~0
}

//ToUse:
/// <summary>
/// Item的高级（功能），涉及安全
/// 
/// PS:仅开发者可设置的内部Tag枚举。
/// 如：Item使用了代码，那就设置该枚举， 并且每次在Item上有UI提示
/// </summary>
[System.Flags]
public enum AD_WSItemAdvance
{
    None = 0,

    IncludeScripts = 1 << 0,//包含第三方脚本（Include custom script）
    KeyListening = 1 << 1,//监听按键（Listen to KeyBoard event）
    Networking = 1 << 2,//联网（eg:Multi Player）

    All = ~0
}
#endregion

#region XR

/// <summary>
/// Which part of the XR Rig does the destination represented
/// </summary>
public enum AD_XRDestinationRigPart
{
    Foot,//Root (Default Value)
    Head,
}
#endregion

#region Input
/// <summary>
/// 根据输入类型，自动切换到对应交互
/// </summary>
public enum AD_InputDeviceType
{
    Keyboard_Mouse,
    Gamepad,
    XRController//ToUse
}
#endregion

#region Command Line
/// <summary>
/// 运行平台模式
/// </summary>
public enum AD_PlatformMode
{
    PC,//【默认值】使用Simulator
    PCVR
}

public enum AD_WindowMode
{
    Default,//【默认值】全屏，在壁纸上方，在其他窗口下方
    Window,//窗口模式，方便录屏等
    FullScreen//全屏模式
}
#endregion