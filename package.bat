@echo off
setlocal enabledelayedexpansion

REM MyrtleSkill Plugin 打包脚本 (Windows)
REM 用途：在本地测试打包流程，生成与 GitHub Actions 相同的 ZIP 包

set CONFIGURATION=Release
set VERSION=%1
if "%VERSION%"=="" set VERSION=dev-build
set OUTPUT_ZIP=MyrtleSkill-%VERSION%.zip

echo =========================================
echo MyrtleSkill Plugin 打包脚本
echo 版本: %VERSION%
echo =========================================

REM 清理旧的构建
echo 🧹 清理旧文件...
if exist release rmdir /s /q release
if exist "%OUTPUT_ZIP%" del "%OUTPUT_ZIP%"

REM 编译项目
echo 🔨 编译项目...
dotnet build MyrtleSkill.csproj --configuration %CONFIGURATION%

REM 检查编译结果
if not exist "bin\%CONFIGURATION%\net8.0\MyrtleSkill.dll" (
    echo ❌ 编译失败！找不到 DLL 文件
    exit /b 1
)

REM 创建目录结构
echo 📁 创建发布目录结构...
mkdir release\addons\counterstrikesharp\addons\MyrtleSkill
mkdir release\gamedata

REM 复制 DLL
echo 📦 复制插件 DLL...
copy "bin\%CONFIGURATION%\net8.0\MyrtleSkill.dll" release\addons\counterstrikesharp\addons\MyrtleSkill\ >nul

REM 复制 gamedata
echo 📦 复制游戏数据...
copy gamedata\MyrtleSkill.gamedata.json release\gamedata\ >nul

REM 复制配置文件（如果存在）
if exist config.json (
    echo 📦 复制配置文件...
    copy config.json release\addons\counterstrikesharp\addons\MyrtleSkill\ >nul
) else (
    echo ⚠️ 警告：config.json 不存在，将创建默认配置
    echo {} > release\addons\counterstrikesharp\addons\MyrtleSkill\config.json
)

REM 复制许可证文件
if exist LICENSE (
    echo 📦 复制许可证...
    copy LICENSE release\ >nul
)

REM 复制 README
if exist README.md (
    echo 📦 复制说明文档...
    copy README.md release\ >nul
)

REM 创建版本信息
echo 📝 创建版本信息...
(
    echo MyrtleSkill Plugin
    echo Build Date: %date% %time%
    echo Version: %VERSION%
) > release\VERSION.txt

REM 创建安装说明
echo 📝 创建安装说明...
(
    echo MyrtleSkill Plugin 安装说明
    echo ==========================
    echo.
    echo 安装步骤：
    echo 1. 将本压缩包解压到服务器的以下目录：
    echo    csgo/addons/counterstrikesharp/
    echo.
    echo 2. 解压后的目录结构应该是：
    echo    csgo/^/
    echo    ├── addons/^/
    echo    │   └── counterstrikesharp/^/
    echo    │       ├── addons/^/
    echo    │       │   └── MyrtleSkill/^/
    echo    │       │       └── MyrtleSkill.dll
    echo    │       └── gamedata/^/
    echo    │           └── MyrtleSkill.gamedata.json
    echo.
    echo 3. 重启 CS2 服务器或重载插件
    echo.
    echo 4. 在服务器控制台输入以下命令启用功能：
    echo    css_event_enable    # 启用娱乐事件系统
    echo    css_skill_enable    # 启用玩家技能系统
    echo.
    echo 配置文件位置：
    echo - 插件配置: addons/counterstrikesharp/addons/MyrtleSkill/config.json
    echo - 游戏数据: addons/counterstrikesharp/gamedata/MyrtleSkill.gamedata.json
    echo.
    echo 更多信息请查看 README.md
) > release\INSTALL.txt

REM 创建 ZIP 包（使用 PowerShell）
echo 🗜️ 压缩打包...
powershell -Command "Compress-Archive -Path release\* -DestinationPath '%OUTPUT_ZIP%' -Force"

REM 显示结果
echo.
echo =========================================
echo ✅ 打包完成！
echo 📦 文件名: %OUTPUT_ZIP%

REM 获取文件大小
for %%F in ("%OUTPUT_ZIP%") do (
    set size=%%~zF
    set /a sizeMB=!size! / 1048576
    echo 📊 文件大小: !sizeMB! MB
)

echo =========================================

endlocal
