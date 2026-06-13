# UniDesk Release Notes

## v1.3.2

This release updates UniDesk into a more complete desktop sidebar tool.

### Changes

- Added module management with show/hide controls and module ordering.
- Added shortcut drag-to-add support and shortcut ordering.
- Added Quick Notes with multiple notes, auto save, pinning, copy, delete, backup, and restore support.
- Added Quick Text with clipboard history, text snippets, one-click copy, sensitive content filtering, backup, and restore support.
- Improved the main panel layout so hardware monitoring and network speed remain readable as modules grow.
- Improved GPU temperature reading with AMD ADL, NVIDIA NVML, and LibreHardwareMonitor fallback support.
- Improved personalization settings including panel height, font size, and custom display title.
- Updated README screenshots and project description for the current public release.

### 中文说明

- 新增模块管理，支持模块显示 / 隐藏和排序。
- 新增快捷方式拖拽添加和快捷方式排序。
- 新增快速便签，支持多条便签、自动保存、置顶、复制、删除、备份和还原。
- 新增快捷文本，支持剪贴板历史、常用短语、一键复制、敏感内容过滤、备份和还原。
- 优化主面板布局，模块增多时硬件监视和网速仍能完整显示。
- 优化 GPU 温度读取，支持 AMD ADL、NVIDIA NVML 和 LibreHardwareMonitor 兜底读取。
- 优化个性化设置，支持面板高度、字体大小和自定义显示标题。
- 更新 README 宣传图和项目介绍，使其匹配当前正式版本。

## v1.1.1

This release focuses on presentation and polish for the public GitHub release.

### Changes

- Updated the README main screenshot to show the integrated network speed monitor.
- Fixed the left-side module title icons for Hardware Monitor, Quick Shortcuts, and Todo List.
- The module title icons now follow the current theme text color instead of reverting to the old blue image assets.
- Removed unused legacy blue module icon assets from the package.
- Updated installer and documentation references to `UniDesk_Setup_1.1.1.exe`.

### 中文说明

- 更新 GitHub 首页主界面截图，现在能看到实时网速监测。
- 修复「硬件监视」「快捷方式」「待办事项」左侧小图标颜色。
- 三个模块图标现在会跟随当前主题文字颜色，不再固定为旧的蓝色图标。
- 移除了不再使用的旧蓝色图标资源。
- 安装包和文档版本更新为 `1.1.1`。

## v1.1.0

### Highlights

- Renamed the desktop widget project to UniDesk.
- Integrated the hardware monitor into the main panel.
- Added CPU, memory, GPU, temperature, and network speed display.
- Kept the hardware monitor under the same theme, transparency, and panel width settings as other modules.
- Preserved local user data by migrating compatible legacy data into the UniDesk data directory.
