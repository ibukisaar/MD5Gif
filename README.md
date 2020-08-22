# 示例GIF
![](https://github.com/ibukisaar/MD5Gif/raw/master/imgs/md5.gif)

# 使用说明
1. 需要用到fastcoll，可从[HashClash](https://www.win.tue.nl/hashclash/)下载。

2. 配置Program类里开头的常量，编译运行即可。
```csharp
class Program {
    /// <summary>
    /// fastcoll的可执行文件路径
    /// </summary>
    const string FastcollExe = @"Z:\fastcoll_v1.0.0.5.exe";
    /// <summary>
    /// 用于存放fastcoll碰撞结果的临时目录
    /// </summary>
    const string WorkspaceDir = @"Z:\"; // 建议用内存盘
    /// <summary>
    /// 最多同时运行的fastcoll进程个数
    /// </summary>
    const int FindCollisionConcurrency = 8;
    /// <summary>
    /// 制造开头为23333333的MD5值，通常这需要很长时间（除非欧皇
    /// </summary>
    const bool IsMake23333333MD5 = true;
    
    ...
```
