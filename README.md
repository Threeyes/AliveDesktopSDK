<h1 align="center">AliveDesktopSDK</h1>

<p align="center">
    <a href="https://store.steampowered.com/app/2190810/Alive_Desktop/"><img src="SDK Icon.svg" alt="Logo" width="600px" height="300px"/>
    <br />
	<a href="https://unity.com/releases/editor/qa/lts-releases?version=2022.3"><img src="https://img.shields.io/badge/%20Unity-2022.3.10f1%20-blue" /></a>
	<a href="https://openupm.com/packages/com.threeyes.alivedesktop.sdk/"><img src="https://img.shields.io/npm/v/com.threeyes.alivedesktop.sdk?label=openupm&amp;registry_uri=https://package.openupm.com" /></a>
	<a href="https://github.com/Threeyes/AliveDesktopSDK/blob/main/LICENSE"><img src="https://img.shields.io/badge/License-MIT-brightgreen.svg" /></a>
    <br />
</p>

## Language
<p float="left">
  <a href="https://github.com/Threeyes/AliveDesktopSDK/blob/main/locale/README-zh-CN.md">中文</a> | 
  <a href="https://github.com/Threeyes/AliveDesktopSDK">English</a>
</p>

## Description
[AliveDesktop](https://store.steampowered.com/app/2190810/Alive_Desktop/) is a software released in Steam, it can replace desktop wallpaper with various 3dMods. It has the following characteristics:
+ Provide a complete visual function module, which can quickly implement common functions without understanding programming.
+ Support dozens of stable [third-party open source libraries](https://github.com/Threeyes/AliveDesktopSDK/wiki/Third-party) To facilitate achieving better results;
+ Support hot updates, you can write logic using C # or Visual Scripting;
+ Support for exposing various parameters (eg: base values, textures or material colors) to users to facilitate customization of the final effect.

## Installation via [OpenUPM](https://openupm.com/packages/com.threeyes.alivedesktop.sdk/)
1. Install [Git](https://git-scm.com/).
2. Create an empty Win project with [Unity2022.3.10f1](https://unity.com/releases/editor/qa/lts-releases?version=2022.3).
3. Download the latest [manifest.json](https://raw.githubusercontent.com/Threeyes/AliveDesktopSDK/main/ProjectConfig~/manifest.json) file and replace the file with the same name in the `[ProjectRootPath]/Packages` directory. 
4. Download [ProjectSetting](https://raw.githubusercontent.com/Threeyes/AliveDesktopSDK/main/ProjectConfig~/ProjectSettings.zip) zip file, extract it and replace the folder with the same name in the `[ProjectRootPath]` directory. 
5. Open the project, Make sure `Packages/manifest.json` contain one and only one External Script Editor that you are using in `Preferences/External Tools` (eg, VisualStudio):

![image](https://user-images.githubusercontent.com/13210990/180822147-5a917199-279f-4cbb-a073-32e5078e2709.png)

## Possible Errors
+ If some error appear or key components missing on first import, try close the project, delete `Library` folder then reopen.
+ If some subfolder in `Package/AliveDesktopSDK` become empty, try reimport the package on `Package Manager` window.

## Documentation
Check out [wiki](https://github.com/Threeyes/AliveDesktopSDK/wiki).

## When will the SDK be updated
![SDK更新规则](https://github.com/Threeyes/AliveDesktopSDK/assets/13210990/d83ef22f-bf28-4f0e-879e-a4c4276675e8)

## Contact
+ [QQ](https://im.qq.com/index/) Group: 159714089

## Samples
To find out the possibility of AliveDesktop, I also upload different kinds of mods to [workshop](https://steamcommunity.com/profiles/76561199378980403/myworkshopfiles/?appid=2190810), feel free to check out the [AliveDesktop_ModUploader](https://github.com/Threeyes/AliveDesktop_ModUploader) project.
