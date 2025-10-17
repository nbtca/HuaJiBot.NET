# huaji-bot-dotnet

## 💡 介绍/开发计划

插件式，多适配器，多通信协议

## 💻 如何参与开发

- 安装 [Visual Studio 2022](https://visualstudio.microsoft.com/zh-hant/vs/community/)
- 安装 [微软 .NET 10 SDK](https://dotnet.microsoft.com/zh-cn/download)
- clone 本仓库源码
- 使用`Visual Studio 2022`打开 `HuaJiBot.NET.sln` 解决方案
- then just coding ...

## 🛠️ How to build?

- ensure `.NET 10 SDK` installed
- clone this repo
- run `dotnet build`

## 🚀 How to deploy?

you guess it. (> ω <)

## 🧩 Project Structure
<!-- 
- `src`
  - `HuaJiBot.NET.CLI`
    - Command line interface for `HuaJiBot.NET`, entry point for running `HuaJiBot.NET`
  - `HuaJiBot.NET`
    - Core project, include core functions such as `plugin manager`,`interface`,` event`,`utils ` and so on.
  - `HuaJiBot.NET.Adapter.OneBot`
    - [`OneBot Protocol`](https://onebot.dev/) adapter for connecting to IM platforms
  - `HuaJiBot.NET.Adapter.Satori`
    - [`Satori Protocol`](https://satori.js.org/en-US/protocol/) adapter for connecting to IM platforms
  - `HuaJiBot.NET.Plugin.RepairTeam`
    - `Repair Team` plugin, for pushing messages from [Repair Service](https://github.com/nbtca/Saturday) to IM platforms
  - `HuaJiBot.NET.Plugin.GitHubBridge`
    - `GitHub Bridge` plugin, push GitHub events to IM platforms
  - `HuaJiBot.NET.Plugin.Calendar`
    - `Calendar` plugin, for pushing calendar events to IM platforms -->
```
📁 src/
├── 🗂️ HuaJiBot.NET.CLI         (Command-Line Interface for running the bot)
├── 🗂️ HuaJiBot.NET             (Core project with core functionalities)
│   ├── Events                (Event handling components)
│   ├── PluginManager        (Plugin management system)
│   ├── Utils                (Utility functions)
│   └── ...                  (Other core files)
├── 🗂️ HuaJiBot.NET.Adapter.*   (Adapters for connecting to IM platforms)
│   ├── OneBot               (Adapter for the OneBot protocol)
│   └── Satori               (Adapter for the Satori protocol)
│       └── ...              (Other potential adapters)
├── 🗂️ HuaJiBot.NET.Plugin.*     (Plugins for specific functionalities)
│   ├── Calendar             (Plugin for pushing calendar events)
│   ├── GitHubBridge         (Plugin for pushing GitHub events)
│   ├── RepairTeam           (Plugin for pushing messages from Repair Service)
│   └── ...                  (Other potential plugins)
└── ...                      (Other project files)
```