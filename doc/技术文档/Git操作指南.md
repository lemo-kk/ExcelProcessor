# Git操作指南

## 代码提交标准流程

本文档记录了将代码提交到Git的标准操作步骤，请按照以下流程进行代码提交。

### 1. 检查当前状态
```bash
git status
```
- 查看当前工作目录状态
- 确认有哪些文件被修改、新增或删除

### 2. 添加文件到暂存区
```bash
git add .
```
- 将所有修改的文件添加到暂存区
- 或者使用 `git add <文件名>` 添加特定文件

### 3. 提交更改
```bash
git commit -m "提交信息描述"
```
- 提交暂存区的文件到本地仓库
- 提交信息应该简洁明了地描述本次更改内容

### 4. 推送到远程仓库
```bash
git push origin master
```
- 将本地提交推送到远程仓库
- 确保代码同步到GitHub

## 操作示例

### 2024年提交示例
```bash
# 1. 检查状态
PS E:\code\code demo\EXCEL_V1.0> git status
On branch master
Your branch is up to date with 'origin/master'.

Changes not staged for commit:
  (use "git add <file>..." to update what will be committed)
  (use "git restore <file>..." to discard changes in working directory)
        modified:   ExcelProcessor.Data/DependencyInjection/DataServiceCollectionExtensions.cs
        modified:   tatus

# 2. 添加文件
PS E:\code\code demo\EXCEL_V1.0> git add .
warning: in the working copy of 'ExcelProcessor.Data/DependencyInjection/DataServiceCollectionExtensions.cs', LF will be replaced by CRLF the next time Git touches it
warning: in the working copy of 'tatus', LF will be replaced by CRLF the next time Git touches it

# 3. 提交更改
PS E:\code\code demo\EXCEL_V1.0> git commit -m "Update DataServiceCollectionExtensions and status file"
[master 56ebaf8] Update DataServiceCollectionExtensions and status file
 1 file changed, 2 insertions(+)

# 4. 推送到远程
PS E:\code\code demo\EXCEL_V1.0> git push origin master
Enumerating objects: 5, done.
Counting objects: 100% (5/5), done.
Delta compression using up to 12 threads
Compressing objects: 100% (3/3), done.
Total 3 (delta 2), reused 0 (delta 0), pack-reused 0 (from 0)
remote: Resolving deltas: 100% (2/2), completed with 2 local objects.
To https://github.com/lemo-kk/ExcelProcessor.git
   ee08587..56ebaf8  master -> master
```

## 注意事项

1. **提交前检查**：每次提交前务必检查 `git status` 确认要提交的文件
2. **提交信息规范**：使用简洁明了的英文或中文描述提交内容
3. **分支管理**：当前使用 `master` 分支，如需创建新分支请先创建再切换
4. **远程仓库**：当前远程仓库地址为 `https://github.com/lemo-kk/ExcelProcessor.git`
5. **行尾符警告**：Windows系统下可能出现LF/CRLF转换警告，这是正常现象

## 常用Git命令

```bash
# 查看状态
git status

# 查看提交历史
git log --oneline

# 查看远程仓库
git remote -v

# 拉取最新代码
git pull origin master

# 查看分支
git branch

# 创建新分支
git checkout -b <分支名>

# 切换分支
git checkout <分支名>
```

## 更新记录

- 2024年：创建Git操作指南文档
- 记录标准提交流程和示例操作