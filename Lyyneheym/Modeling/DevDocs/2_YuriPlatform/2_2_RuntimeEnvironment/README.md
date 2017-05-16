﻿## 运行时环境

Yuri引擎运行时环境**YRE**（Yuri engine Runtime Environment）负责游戏逻辑的演绎、资源的托管、上下文服务以及为游戏提供各种基于当前虚拟机实现平台（如.NET）上的功能接口的更高程度的封装。<br/>
本质上来说，YRE并非**一个软件**，而是**一种接口**。即不同平台上的YRE都有这样的一个**保证**：在执行同一份Yuri-IL机器无关中间代码时，它们的演绎结果是**一致无差别的**。这种保证为Yuri的可跨平台性提供了基础。<br/>
YRE还提供了一定程度上的异常监视，当游戏逻辑里发生明显的异常时，YRE有能力抛出错误并选择应该执行的下一个业务流程。在未来的版本中，YRE还计划支持上下文监控和单步调试等功能。<br/>

### YRE体系结构
YRE的本质是一种变体**栈式**运行时环境，它拥有自己的指令集，内存组织方式和任务调度策略。在Yuri Engine项目中，这个指令集就是Yuri-IL机器无关的中间代码中的命令。YRE通过解析这些命令，来进行游戏后台的数据存储与操作，并调用当前平台的图形、音频和设备IO接口来实现游戏数据的呈现以及与用户的交互。<br/>

![YuriVM](./YuriVM.png)

YRE一共有以下几个核心部分组成：

- **核心堆栈（Engine Core Stack）**：处理主调用堆栈的消息循环和中断响应
- **堆（Heap）**：存放上下文、场景动作、索引、缓存、快照的容器
- **可并行堆栈（Parallelable Stack）**：处理场景的并行函数和信号系统的（反）激活函数的消息循环
- **依赖注入堆栈（IoC Stack）**：处理来自引擎**外部**Python脚本的函数调用

整个YRE体系结构的示例图如下：

![YRE](./RT.png)


注意到在**堆**区中，存在**Eden Space**和**Permanent Space**两个子区，Eden Space负责储存临时性上下文和资源缓存，它们非常驻内存，YRE会在必要的时候解除对它们的引用维持以触发CLR进行垃圾回收；而存放在Permanent Space区域的是可序列化上下文（场景上下文）、资源索引、脚本代码等高频度访问的内容，它们会在YRE环境主动坍塌之前常驻内存，不会被垃圾回收。<br/>
关于上述提及的消息循环、信号系统和并行处理等概念及其技术实现细节，将在本章节下的各小节中详述。本文档将以一个**Windows**平台下的YRE实现来分析其技术细节，它是一个基于.NET框架实现的程序，通过GDI+/WPF作为图形引擎，NAudio作为音频引擎，使用消息循环和中断机制来解析中间代码。

### YRE的启动
YRE按照以下的逻辑顺序来引导游戏的启动过程：

- 检查游戏文件的完整性
- 初始化WPF相关的各个管理器，如3D场景镜头管理器`SCamera3D`等
- IL解析器`ILConvertor`读取Yuri-IL文件，恢复为内存实体并整理代码块嵌套逻辑关系
- 配置管理器`ConfigParser`读取配置文件，恢复配置上下文
- 资源管理器`ResourceManager`扫描游戏资源索引PST文件，加载索引到内存，缓存持久性内容
- 初始化YRE主控制`Director`，并调用持久性上下文管理器`PersistContextDAO`读入持久性上下文
- 运行时信息管理器`RuntimeManager`将入口场景`Main`压入主调用堆栈
- 启动引擎的**消息循环**、**并行处理**、**信号系统**

### YRE的主动坍塌
在有必要结束游戏时，YRE按照以下逻辑顺序来引导游戏的结束过程：

- 主渲染器`UpdateRender`关闭前端视窗
- 持久性上下文管理器`PersistContextDAO`记录当前时间戳，更新游戏的持久性信息，并写到稳定储存器中
- 主控制器`Director`通知各部分模块调用自身的`Dispose`方法做收尾处理
- 退出程序，由CLR进行垃圾回收
