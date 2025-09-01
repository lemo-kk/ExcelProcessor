@echo off
echo 正在启动Excel处理器...
echo.

cd /d "%~dp0"
cd ExcelProcessor.WPF\bin\Debug\net6.0-windows

if exist ExcelProcessor.WPF.exe (
    echo 找到可执行文件，正在启动...
    start ExcelProcessor.WPF.exe
    echo 程序已启动！
) else (
    echo 错误：找不到可执行文件
    echo 请先构建项目：dotnet build
)

pause 