@echo off
echo ==========================================
echo Windows硬件信息模拟Hook工具 - 编译脚本
echo ==========================================
echo.

REM 检查是否安装了.NET SDK或Visual Studio
where msbuild >nul 2>&1
if %errorlevel% neq 0 (
    echo 错误: 未找到 MSBuild，请安装 Visual Studio 或 .NET SDK
    echo.
    pause
    exit /b 1
)

echo 步骤 1: 还原 NuGet 包...
echo.
msbuild HardwareHook.sln -t:Restore -p:Configuration=Debug -p:Platform=x64
if %errorlevel% neq 0 (
    echo.
    echo 错误: NuGet 包还原失败
    pause
    exit /b 1
)

echo.
echo 步骤 2: 编译解决方案...
echo.
msbuild HardwareHook.sln -p:Configuration=Debug -p:Platform=x64
if %errorlevel% neq 0 (
    echo.
    echo 错误: 编译失败
    pause
    exit /b 1
)

echo.
echo ==========================================
echo 编译成功！
echo ==========================================
echo.
echo 可执行文件位置:
echo   - 主程序: HardwareHook.Main\bin\x64\Debug\HardwareHook.Main.exe
echo   - 测试程序: HardwareInfo.Test\bin\x64\Debug\HWInfoTest.exe
echo   - Hook DLL: HardwareHook.Core\bin\x64\Debug\HardwareHook.Core.dll
echo.
echo 提示: 请以管理员身份运行主程序
echo.
pause
