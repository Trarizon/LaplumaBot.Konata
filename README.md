# Tra.Lapluma

（原来看自己的旧代码真的会想骂人）
tx大清洗了一把，Konata也没了，暂时也没空写bot，先archive了

基于[Konata.Core](https://github.com/KonataDev/Konata.Core)的QQ bot，个人消遣的产物，大概也是用来练手的东西

## 用到的包
- [Konata.Core](https://github.com/KonataDev/Konata.Core) Bot基础.nuget
- [Newtonsoft.Json](https://www.newtonsoft.com/json) 大概是都知道的json parser，解析`JsonChain.Content`的东西，以后可能还有其他用途
- [System.Drawing.Common](https://www.nuget.org/packages/System.Drawing.Common) 画画用的，不知道有没有其他的绘图包

## Tasks
- `BaseTask` - 基类，没什么好说的
  - `UserTask` - 没有权限限制的功能，群里管理员以上可以开关
    - `AwaitTask<TimerPackage>` - 发送指令后，Bot会等待发送参数，比如一些json xml信息
    - `LoopTask<TimerPackage>` - 发送指令后，Bot会始终启用的功能
      - `MultiPlayerTask<MultiPlayerPackage>` - 多人游戏，也可以是双人

增加功能直接在`Tra.Lapluma.Core.Tasks.UserTasks.Models`里加就行了（怎么这么长

`Models.Utils`用于存放Task里用到的工具，
`Models.Utils.LocalAssets`封装本地资源

## Packages //因为被gitignore了改成了TaskPackage
- `BasePackage` - 基类
  - `TimerPackage` - 内置一个计时器的Package
    - `MultiPlayerPackage` - 内置多人游戏玩家基本信息的Package
      - `TwoPlayerPackage` - 特化后，专用于双人游戏的Package

## 已有的功能
主要将功能分为了两种。
`Tra.Lapluma.Tasks`下的是操作bot用的，`UserTasks`下的是大家使用的，

<details>
<summary>关于格式用到符号</summary>

符号|注释
---|---
`[]`|可省略
`<>`|参数
`\|`|或
`()`|通常用来限定`\|`的范围

</details>

TaskName|命令格式|备注
:-:|:--|:--
`BotManipulation`|[bot_manip] &lt;param&gt;|唯一一个可以忽略Regex的task，用于操控bot，可用参数help里写了
`SendMessage`|s[f\|g]msg &lt;uid&gt; &lt;msg&gt;|操控bot发送消息，方便用的
`BotHelp`|lpm [&lt;param&gt;]|就是普通的帮助页面，嗯。param省略就是普通的打招呼
`DeemoChartClipPainter`|decht [{&lt;speed&gt;}] <chart_code>|可以用来画de谱
`GetWyyResource`|(wyy\|网易云) [-c\|-m] [<url>]|从网易云拿歌和封面用的，封面会直接发图，歌发直链
`SolveHikariQf`|solve_qf [&lt;maxp&gt;]|用来和盐酸的光光友好互动（x，参数什么的和光光一样
`TicTacToe`|(井字棋\|tictactoe) [@&lt;somebody&gt;]|其实是用来测试的task基类的井字棋

