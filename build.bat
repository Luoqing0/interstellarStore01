@echo off
chcp 65001 >nul
echo ========================================
echo  星际商店 - 编译脚本
echo ========================================

set "SOURCE_DIR=%~dp0Source"

echo 项目目录: %SOURCE_DIR%
echo 输出目录: %~dp0Assemblies
echo.

REM 清理旧的构建输出
if exist "%~dp0Assemblies\星际商店.dll" del "%~dp0Assemblies\星际商店.dll"
if exist "%~dp0Assemblies\星际商店.pdb" del "%~dp0Assemblies\星际商店.pdb"

echo 正在编译...
cd /d "%SOURCE_DIR%"
dotnet build -c Release --nologo -v minimal

if %ERRORLEVEL% EQU 0 (
    echo.
    if exist "%~dp0Assemblies\星际商店.dll" (
        echo ========================================
        echo  编译成功!
        echo  输出: Assemblies\星际商店.dll
        echo ========================================
    ) else (
        echo ========================================
        echo  编译警告: DLL未在预期位置生成，请检查.csproj
        echo ========================================
    )
) else (
    echo.
    echo ========================================
    echo  编译失败! 请检查上方错误信息。
    echo ========================================
)

pause
