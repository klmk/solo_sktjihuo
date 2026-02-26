# Windows硬件信息模拟Hook工具

## 项目简介

Windows硬件信息模拟Hook工具是一个用于模拟Windows系统硬件信息的工具，主要功能包括：

- 模拟CPU信息（型号、核心数、CPU ID）
- 模拟硬盘信息（序列号）
- 模拟MAC地址
- 模拟主板信息（序列号）
- 支持DLL注入到目标进程
- 可视化操作界面
- 详细的日志系统
- 硬件信息测试工具

## 项目结构

```
SOLO_Code/
├── HardwareHook.sln              # 解决方案文件
├── HardwareHook.Core/            # 核心DLL项目
│   ├── Configuration/            # 配置管理
│   ├── HardwareInfo/             # 硬件信息读取
│   ├── Hooking/                  # API Hook实现
│   ├── Logging/                  # 日志系统
│   └── HardwareHook.Core.csproj  # 项目文件
├── HardwareHook.App/             # 主程序项目
│   ├── MainForm.cs               # 主窗体
│   ├── Program.cs                # 入口文件
│   └── HardwareHook.App.csproj   # 项目文件
├── HardwareHook.Tester/          # 测试程序项目
│   ├── Program.cs                # 入口文件
│   ├── TesterMainForm.cs         # 测试窗体
│   └── HardwareHook.Tester.csproj # 项目文件
└── README.md                     # 本说明文件
```

## 开发环境要求

- Windows 10/11 x64 系统
- Visual Studio 2019 或更高版本
- .NET Framework 4.8 开发工具包
- 管理员权限

## 依赖项

- EasyHook 2.6.6 - DLL注入和API Hook框架
- Newtonsoft.Json 13.0.1 - JSON配置文件处理

## 使用方法

### 1. 构建项目

1. 打开 `HardwareHook.sln` 解决方案文件
2. 设置平台目标为 `x64`
3. 构建解决方案

### 2. 运行主程序

1. 以管理员身份运行 `HardwareHook.App.exe`
2. 在「进程管理」选项卡中选择目标进程
3. 点击「注入」按钮将核心DLL注入到目标进程

### 3. 配置硬件信息

1. 在「硬件信息」选项卡中点击「读取硬件」按钮获取当前硬件信息
2. 点击「导出配置」按钮将当前硬件信息导出为配置文件
3. 手动编辑 `config.json` 文件修改硬件信息配置

### 4. 测试硬件信息

1. 运行 `HardwareHook.Tester.exe`
2. 点击相应的测试按钮查看硬件信息
3. 或者使用命令行模式：
   ```
   HardwareHook.Tester --cpu      # 测试CPU信息
   HardwareHook.Tester --disk     # 测试硬盘信息
   HardwareHook.Tester --mac      # 测试MAC地址
   HardwareHook.Tester --bios     # 测试主板信息
   HardwareHook.Tester --all      # 测试所有信息
   ```

### 5. 查看日志

1. 在「日志查看」选项卡中点击「读取日志」按钮查看日志
2. 日志文件位于 `logs/` 目录下

## 注意事项

1. 本工具需要以管理员身份运行
2. 避免注入系统关键进程，以防影响系统稳定性
3. 注入过程可能会被杀毒软件拦截，请暂时禁用杀毒软件或添加信任
4. 本工具仅用于测试和学习目的，请勿用于非法用途

## 故障排除

### 注入失败

- 确保目标进程存在且未退出
- 确保配置文件有效
- 确保 `HardwareHook.Core.dll` 在正确位置
- 确保以管理员身份运行主程序

### Hook功能未生效

- 检查日志，确认Hook安装是否成功
- 使用测试程序验证硬件信息是否已改变
- 检查目标程序使用的API是否被Hook覆盖

### 稳定性问题

- 所有Hook回调函数都有try-catch保护，避免目标进程崩溃
- 使用异步日志写入，避免影响目标进程性能
- 定期检查日志文件，及时发现问题

## 版本历史

### v1.0.0

- 初始版本
- 支持CPU、硬盘、MAC地址、主板信息模拟
- 支持DLL注入到目标进程
- 可视化操作界面
- 详细的日志系统
- 硬件信息测试工具

## 许可证

本项目仅供学习和测试使用，请勿用于商业用途。
