@echo off
chcp 65001 >nul
where node >nul 2>nul
if errorlevel 1 (
  echo [错误] 未找到 node.js，请安装 Node.js 或将其加入 PATH 后重试
  pause
  exit /b 1
)
echo ============================================
echo   Suzerain 修改器代码自检
echo   用法: 运行本脚本, 或手动执行:
echo     node selfcheck.js [--quick] [--skip=项] [--json] [--strict-warnings]
echo ============================================
node "%~dp0selfcheck.js" %*
echo.
if errorlevel 1 (
  echo [自检未通过] 请根据上方 ✗ 项修复后重新运行
) else (
  echo [自检通过] 无问题
)
pause
