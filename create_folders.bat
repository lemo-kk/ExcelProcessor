@echo off
echo 正在创建Excel处理器的默认目录结构...

:: 创建主目录
if not exist "data" mkdir data
if not exist "config" mkdir config
if not exist "logs" mkdir logs

:: 创建data子目录
if not exist "data\input" mkdir data\input
if not exist "data\output" mkdir data\output
if not exist "data\templates" mkdir data\templates
if not exist "data\temp" mkdir data\temp

echo 目录结构创建完成！
echo.
echo 目录结构：
echo ├── data\
echo │   ├── input\      (Excel输入文件目录)
echo │   ├── output\     (导出文件目录)
echo │   ├── templates\  (Excel模板文件目录)
echo │   └── temp\       (临时文件目录)
echo ├── config\         (配置文件目录)
echo └── logs\           (日志目录)
echo.
echo 请将需要处理的Excel文件放入 data\input\ 目录中。
pause 