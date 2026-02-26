# Windows硬件信息模拟Hook工具

## 项目概述

Windows硬件信息模拟Hook工具通过DLL注入和API Hook技术，实现对目标进程的硬件信息模拟，可用于软件测试、兼容性验证等场景。

## 项目结构

```
Buddy_Code/
├── HardwareHook.Core/          # Hook核心DLL
│   ├── Config/                 # 配置管理
│   ├── Logging/                # 日志系统
│   ├── Native/                 # 原生API定义
│   ├── Hook/                   # Hook管理器
│   └── EntryPoint.cs           # DLL入口点
├── HardwareInfo.Test/          # 硬件信息测试程序（控制台）
│   └── Program.cs              # 测试程序入口
└── HardwareHook.Main/          # 主程序（WinForms）
    ├── Forms/                  # 窗体
    ├── Controllers/            # 控制器
    └── Models/                 # 数据模型
```

## 开发环境要求

- **操作系统**: Windows 10/11 x64
- **运行时**: .NET Framework 4.8
- **开发工具**: Visual Studio 2019 或更高版本
- **权限**: 管理员权限（必须）

## 依赖项

- EasyHook 2.7.7097
- Newtonsoft.Json 13.0.3
- CommandLineParser 2.9.1（仅测试程序）

## 编译说明

### 1. 还原NuGet包

```bash
cd Buddy_Code
msbuild HardwareHook.sln -t:Restore
```

### 2. 编译解决方案

```bash
msbuild HardwareHook.sln -p:Configuration=Debug -p:Platform=x64
```

或在Visual Studio中打开解决方案后，选择 **x64** 平台和 **Debug** 配置，然后编译。

## 使用说明

### 1. 启动主程序

以**管理员身份**运行 `HardwareHook.Main.exe`：

```bash
右键 -> 以管理员身份运行
```

### 2. 基本操作流程

1. **刷新进程列表**: 点击"刷新进程列表"按钮获取系统进程
2. **选择配置文件**: 
   - 点击"选择配置文件"选择已有的JSON配置
   - 或点击"导出配置模板"生成默认配置
3. **选择目标进程**: 在进程列表中选择一个或多个进程
4. **注入Hook**: 点击"注入Hook"按钮执行注入
5. **验证效果**: 
   - 运行 `HWInfoTest.exe --all` 查看模拟后的硬件信息
   - 或在Hook管理标签页查看状态

### 3. 测试程序使用

```bash
# 显示所有硬件信息
HWInfoTest.exe --all

# 显示特定硬件信息
HWInfoTest.exe --cpu      # CPU信息
HWInfoTest.exe --disk     # 硬盘信息
HWInfoTest.exe --mac      # MAC地址
HWInfoTest.exe --bios     # 主板/BIOS信息
HWInfoTest.exe --memory   # 内存信息
HWInfoTest.exe --system   # 系统信息
```

### 4. 配置文件格式

```json
{
  "Version": "1.0",
  "Cpu": {
    "Model": "Intel(R) Core(TM) i9-12900K",
    "CoreCount": 16,
    "CpuId": "BFEBFBFF000A06E9"
  },
  "Disk": {
    "Serial": "1234567890ABCDEF"
  },
  "Mac": {
    "Address": "00:11:22:33:44:55"
  },
  "Motherboard": {
    "Serial": "MB-2025-12345678"
  }
}
```

## 功能特性

### 1.0版本已实现功能

- ✅ **硬件信息读取与配置导出**
- ✅ **进程列表加载与刷新**
- ✅ **DLL注入基础框架**
- ✅ **核心API Hook实现**
  - CPU信息（核心数）
  - 硬盘信息（序列号）
  - MAC地址
  - 主板/BIOS信息
- ✅ **可视化操作界面**
- ✅ **基础日志系统**
- ✅ **测试验证程序**
- ✅ **异常处理与边界情况**

### 支持的目标进程

- 标准用户进程（如 notepad.exe）
- 64位进程
- w3wp.exe（IIS应用池进程）

## 常见问题

### 1. 注入失败：权限不足

**原因**: 未以管理员身份运行
**解决**: 右键程序 -> 以管理员身份运行

### 2. Hook功能未生效

**原因**: 目标程序使用了不同的API获取硬件信息
**解决**: 
- 使用 `HWInfoTest.exe` 验证Hook效果
- 检查配置文件是否正确加载
- 查看日志了解Hook状态

### 3. 目标进程崩溃

**原因**: Hook回调函数异常
**解决**: 
- 查看日志中的错误信息
- 确保配置文件格式正确
- 更新到最新版本

### 4. w3wp.exe进程无法注入

**原因**: IIS应用池权限问题
**解决**:
- 确保以管理员身份运行
- 检查IIS应用池标识设置

## 性能指标

- **Hook回调函数执行时间**: < 1ms
- **注入过程耗时**: < 500ms
- **内存占用增加**: ~5MB
- **CPU使用率增加**: < 1%

## 安全声明

本工具仅用于软件测试、兼容性验证和开发调试。禁止用于绕过软件许可验证、侵犯知识产权等非法用途。使用者应遵守当地法律法规。

## 技术支持

如遇到问题，请查看Logs目录下的日志文件，或提交Issue反馈。

## 版本历史

### v1.0.0 (2026-02-25)
- 初始版本发布
- 实现基础硬件信息模拟功能
- 提供可视化操作界面
- 支持测试验证

## 开发团队

Windows硬件信息模拟Hook工具开发团队

---

**注意**: 使用本工具前请确保您有合法的使用权限，并遵守相关法律法规。
