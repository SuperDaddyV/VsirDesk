# UniDesk

<p align="center">
  <img src="images/unidesk-hero.png" alt="UniDesk 介绍图" width="900">
</p>

<p align="center">
  <b>轻量的 Windows 桌面侧边栏，把时间天气、硬件监视、快捷方式和待办事项集中到一个面板里。</b>
</p>

<p align="center">
  <a href="README.md">English</a> | 简体中文
</p>

<p align="center">
  <a href="#功能亮点">功能亮点</a> ·
  <a href="#截图">截图</a> ·
  <a href="#硬件监视">硬件监视</a> ·
  <a href="#安装">安装</a> ·
  <a href="#构建">构建</a>
</p>

UniDesk 是一个紧凑的个人桌面中心，可以贴在桌面边缘，常驻显示日常信息、快捷入口和轻量系统状态。它支持主题颜色、透明度、宽度、置顶、锁定和收起展开，适合作为长期使用的桌面侧边工具。

## 功能亮点

- **时间天气**：显示时间、日期、农历、天气、湿度、空气质量和温度范围。
- **硬件监视**：显示 CPU、内存、GPU、温度，以及实时网络接收/发送速度。
- **快捷方式**：把常用应用、文件夹或文件固定到侧边栏，一键打开。
- **待办事项**：记录待办任务，支持优先级、到期时间、完成状态、备份和还原。
- **个性化面板**：支持主题颜色、透明度、宽度、置顶、锁定、收起和快捷方式数量设置。
- **本地优先**：设置、快捷方式和待办事项主要保存在本地。

## 截图

<p align="center">
  <img src="images/unidesk-main-panel.png" alt="UniDesk 主界面" width="360">
</p>

<p align="center">
  <img src="images/unidesk-modules.png" alt="UniDesk 模块介绍" width="900">
</p>

<p align="center">
  <img src="images/unidesk-settings.png" alt="UniDesk 设置介绍" width="900">
</p>

## 硬件监视

硬件监视模块直接集成在 UniDesk 主面板中，位于天气和快捷方式之间。它不是独立悬浮窗，因此会跟随主面板的主题、透明度、宽度和窗口设置统一变化。

不同电脑能读取到的数据会受到硬件、驱动和权限影响：

- CPU 使用率来自 Windows 系统计数。
- 内存使用率来自 Windows 系统内存状态。
- CPU 温度会尽量从可用的硬件监视来源读取。
- AMD GPU 使用率和温度会尽量从驱动或厂商数据读取。
- 网络速度会统计可用的有线网卡和 Wi-Fi 网卡，并显示实时接收/发送速度。

如果某些温度数据无法读取，界面会显示 `--`。

## 定位说明

自动定位使用当前网络出口 IP。普通家庭网络下通常能识别到正确城市，但代理、VPN、公司网络、运营商出口等情况可能导致定位到其他城市。需要固定城市时，可以在设置里手动选择。

## 安装

从 GitHub Releases 下载最新安装包并运行：

```powershell
UniDesk_Setup_1.1.0.exe
```

系统要求：

- Windows 10 1903 或更新版本
- Windows 11

## 构建

```powershell
dotnet restore
dotnet build --configuration Release
dotnet publish .\UniDesk\UniDesk.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish\win-x64
```

发布文件会输出到 `publish\win-x64` 目录。

## 数据兼容

UniDesk 的新数据目录为 `%LOCALAPPDATA%\UniDesk`。启动时会尝试复制兼容的旧版本地数据，尽量保留原有设置、待办事项、快捷方式、主题配置和缓存文件。
