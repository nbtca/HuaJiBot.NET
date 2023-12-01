# huaji-bot-dotnet

## 介绍/开发计划

插件式，多适配器，包括但不限于\_\_\_\_协议

## 如何参与开发

- 安装 [Visual Studio 2022](https://visualstudio.microsoft.com/zh-hant/vs/community/)
- 安装 [微软 .NET 8 SDK](https://dotnet.microsoft.com/zh-cn/download)
- clone 本仓库源码
- 使用`Visual Studio 2022`打开 `HuaJiBot.NET.sln` 解决方案
- then just coding ...

## How to build?

- ensure `.NET 8 SDK` installed
- clone this repo
- run `dotnet build`

## How to deploy?

you guess it. (> ω <)

## Project Structure

- `src`
  - `HuaJiBot.NET`
    - Core project, include core functions such as `plugin manager`,`interface`,` event`,`utils ` and so on.
  - `HuaJiBot.NET.Adapter.Red`
    - `Red Protocol` adapter for connecting to `__NT`
  - `HuaJiBot.NET.CLI`
    - Command line interface (current only support `Red Protocol`)
  - `HuaJiBot.NET.Plugin.RepairTeam`
    - `Repair Team` plugin for `Red Protocol`
