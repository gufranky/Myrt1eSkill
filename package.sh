#!/bin/bash

# MyrtleSkill Plugin 打包脚本
# 用途：在本地测试打包流程，生成与 GitHub Actions 相同的 ZIP 包

set -e  # 遇到错误立即退出

# 配置
CONFIGURATION="Release"
VERSION="${1:-dev-build}"  # 默认版本号：dev-build
OUTPUT_ZIP="MyrtleSkill-${VERSION}.zip"

echo "========================================="
echo "MyrtleSkill Plugin 打包脚本"
echo "版本: ${VERSION}"
echo "========================================="

# 清理旧的构建
echo "🧹 清理旧文件..."
rm -rf release
rm -f "${OUTPUT_ZIP}"

# 编译项目
echo "🔨 编译项目..."
dotnet build MyrtleSkill.csproj --configuration "${CONFIGURATION}"

# 检查编译结果
if [ ! -f "bin/${CONFIGURATION}/net8.0/MyrtleSkill.dll" ]; then
    echo "❌ 编译失败！找不到 DLL 文件"
    exit 1
fi

# 创建目录结构
echo "📁 创建发布目录结构..."
mkdir -p release/addons/counterstrikesharp/addons/MyrtleSkill
mkdir -p release/gamedata

# 复制 DLL
echo "📦 复制插件 DLL..."
cp "bin/${CONFIGURATION}/net8.0/MyrtleSkill.dll" \
   release/addons/counterstrikesharp/addons/MyrtleSkill/

# 复制 gamedata
echo "📦 复制游戏数据..."
cp gamedata/MyrtleSkill.gamedata.json release/gamedata/

# 复制配置文件（如果存在）
if [ -f config.json ]; then
    echo "📦 复制配置文件..."
    cp config.json release/addons/counterstrikesharp/addons/MyrtleSkill/
else
    echo "⚠️  警告：config.json 不存在，将创建默认配置"
    echo '{}' > release/addons/counterstrikesharp/addons/MyrtleSkill/config.json
fi

# 复制许可证文件
if [ -f LICENSE ]; then
    echo "📦 复制许可证..."
    cp LICENSE release/
fi

# 复制 README
if [ -f README.md ]; then
    echo "📦 复制说明文档..."
    cp README.md release/
fi

# 创建版本信息
echo "📝 创建版本信息..."
cat > release/VERSION.txt << EOF
MyrtleSkill Plugin
Build Date: $(date -u +'%Y-%m-%d %H:%M:%S UTC')
Version: ${VERSION}
Branch: $(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo "unknown")
Commit: $(git rev-parse HEAD 2>/dev/null || echo "unknown")
EOF

# 创建安装说明
echo "📝 创建安装说明..."
cat > release/INSTALL.txt << 'EOF'
MyrtleSkill Plugin 安装说明
==========================

安装步骤：
1. 将本压缩包解压到服务器的以下目录：
   csgo/addons/counterstrikesharp/

2. 解压后的目录结构应该是：
   csgo/
   ├── addons/
   │   └── counterstrikesharp/
   │       ├── addons/
   │       │   └── MyrtleSkill/
   │       │       └── MyrtleSkill.dll
   │       └── gamedata/
   │           └── MyrtleSkill.gamedata.json

3. 重启 CS2 服务器或重载插件

4. 在服务器控制台输入以下命令启用功能：
   css_event_enable    # 启用娱乐事件系统
   css_skill_enable    # 启用玩家技能系统

配置文件位置：
- 插件配置: addons/counterstrikesharp/addons/MyrtleSkill/config.json
- 游戏数据: addons/counterstrikesharp/gamedata/MyrtleSkill.gamedata.json

更多信息请查看 README.md
EOF

# 创建 ZIP 包
echo "🗜️  压缩打包..."
cd release
zip -r "../${OUTPUT_ZIP}" .
cd ..

# 显示结果
echo ""
echo "========================================="
echo "✅ 打包完成！"
echo "📦 文件名: ${OUTPUT_ZIP}"
echo "📊 文件大小: $(du -h "${OUTPUT_ZIP}" | cut -f1)"
echo ""
echo "📂 内容预览："
unzip -l "${OUTPUT_ZIP}" | head -20
echo "========================================="
