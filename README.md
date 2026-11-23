# Monotask To Do

轻量待办工具，包含 Windows 桌面悬浮窗和手机端版本。数据本地优先，结构已为后续云端多端同步预留。

## 当前版本

**手机端测试版 1.0**

- Android APK 下载：https://github.com/ATiprime/Monotask-To-Do/releases/tag/android-v1.0
- 手机网页版入口：https://atiprime.github.io/Monotask-To-Do/
- 快捷清单 PWA：https://atiprime.github.io/Monotask-To-Do/quick/
- 除草器 PWA：https://atiprime.github.io/Monotask-To-Do/weeder/

## 功能

- 快捷清单：最多 5 项，勾选划线，拖动左侧把手排序
- 除草器：勾选划线，一键清除已勾选项，支持 6 秒撤回
- Windows 桌面端支持悬浮置顶、拖动排序和本地保存
- 手机端支持离线使用、添加到主屏幕、独立 App 安装

## 目录结构

- `WidgetCore.cs`、`QuickList.cs`、`WeederList.cs`：Windows C# WinForms 桌面端源码
- `docs/`：手机端 PWA 源码，包含两个独立应用和共享数据层
- `android/`：Android WebView 外壳与 APK 构建脚本
- `build-widgets.ps1`：Windows EXE 构建脚本
- `package-widgets.ps1`：Windows EXE 打包脚本
- `docs/make-mobile-icons.ps1`：手机端图标生成脚本

## 构建

Windows EXE：

```powershell
powershell -ExecutionPolicy Bypass -File .\build-widgets.ps1
powershell -ExecutionPolicy Bypass -File .\package-widgets.ps1
```

Android APK：

需要 JDK 17、Android SDK 34 和 build-tools 34.0.0。设置 `JAVA_HOME`、`ANDROID_HOME` 后运行：

```powershell
powershell -ExecutionPolicy Bypass -File .\android\build-apks.ps1
```

输出目录为 `android/dist/`。签名文件生成在 `android/keys/`，该目录不会提交到仓库，正式更新时需保留同一签名文件。

## 数据模型

手机端数据保存在本机，当前包含稳定的任务 ID、创建/更新时间、排序值、删除墓碑和修改设备标识，为后续接入云端同步做准备。桌面端与手机端目前尚未实现云端同步。
