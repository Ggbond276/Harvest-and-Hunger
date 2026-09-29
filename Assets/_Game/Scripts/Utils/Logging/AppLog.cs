// -----------------------------------------------------------------------------
//  AppLog.cs
//  ---------------------------------------------------------------------------
//  Harvest & Hunger —— 2D ARPG 通用日志系统
//
//  设计理念:
//      * 模块化订阅 (LogModule)  : 调用方按业务模块打日志,模块互不干扰
//      * 双轴划分 (Module x Level): 横轴=模块,纵轴=级别,实现细粒度过滤
//      * 零开销 (Conditional)     : Release 编译期彻底剥离,无 GC、无调用
//      * 原生集成 (Unity Debug)   : Warning/Error 触发原生图标与堆栈
//
//  推荐放置位置:
//      Assets/Scripts/Logging/AppLog.cs
//
//  使用示例:
//      // 1. 仅开启 UI 与 Combat 模块,其余关闭
//      AppLog.EnabledModules = LogModule.UI | LogModule.Combat;
//
//      // 2. 输出不同级别日志
//      AppLog.Log(LogModule.Resource, "Texture loaded: " + tex.name);
//      AppLog.LogWarning(LogModule.Combat, "Player HP low: " + hp);
//      AppLog.LogError(LogModule.UI, "Button click handler missing!", buttonGo);
//
//  性能承诺:
//      在 Build(非 UNITY_EDITOR)产物中,所有 AppLog.* 调用会在编译期被
//      完全移除,不会产生任何字符串拼接、装箱或方法调用开销。
// -----------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;

namespace HarvestAndHunger.Logging
{
    /// <summary>
    /// 业务模块枚举 —— 横轴。
    /// 使用 [Flags] 位标记实现,可组合、可位运算屏蔽。
    /// 新增模块时请:
    ///   1. 在此追加枚举值(必须是 2 的幂)
    ///   2. 在 <see cref="ModuleMeta"/> 中追加颜色与名称映射
    ///   3. 在 <see cref="All"/> 中追加位或运算
    /// </summary>
    [Flags]
    public enum LogModule
    {
        /// <summary>无模块,占位用。</summary>
        None    = 0,

        /// <summary>系统底层 —— 启动/初始化/平台桥接。</summary>
        System  = 1 << 0,  // 0x0001

        /// <summary>资源加载 —— Addressable / AssetBundle / 异步加载。</summary>
        Resource = 1 << 1, // 0x0002

        /// <summary>用户界面 —— UI 事件、面板生命周期。</summary>
        UI      = 1 << 2,  // 0x0004

        /// <summary>战斗逻辑 —— 伤害、技能、受击、Buff。</summary>
        Combat  = 1 << 3,  // 0x0008

        /// <summary>数据/存档 —— 序列化、JSON、Save/Load。</summary>
        Data    = 1 << 4,  // 0x0010

        /// <summary>AI 行为 —— 敌人状态机、寻路、决策。</summary>
        AI      = 1 << 5,  // 0x0020
    }

    /// <summary>
    /// 应用日志门面 —— 静态类,全局唯一入口。
    /// 所有方法均使用 [Conditional("UNITY_EDITOR")] 包裹,
    /// 在非 Editor 构建中调用方代码会在编译期被完全删除。
    /// </summary>
    public static class AppLog
    {
        // ---------------------------------------------------------------------
        //  颜色与名称映射表 (内部)
        // ---------------------------------------------------------------------

        /// <summary>
        /// 单个模块的元信息:显示名称 + 16 进制颜色代码。
        /// 使用结构体避免装箱。
        /// </summary>
        private readonly struct ModuleMeta
        {
            /// <summary>输出时显示的中文/英文名。</summary>
            public readonly string Name;

            /// <summary>富文本 16 进制颜色,不含 # 号,例如 "00FFFF"。</summary>
            public readonly string ColorHex;

            public ModuleMeta(string name, string colorHex)
            {
                Name = name;
                ColorHex = colorHex;
            }
        }

        /// <summary>
        /// 全模块位掩码,用于“一键开启所有”场景。
        /// </summary>
        public const LogModule All =
            LogModule.System  | LogModule.Resource | LogModule.UI |
            LogModule.Combat  | LogModule.Data     | LogModule.AI;

        // 模块元数据表 —— 顺序与 LogModule 枚举值一一对应(不含 None)。
        // 颜色选取遵循: 高对比度 + 视觉差异最大化,避免相邻模块混淆。
        private static readonly ModuleMeta[] s_Metas =
        {
            new ModuleMeta("System",   "FFD700"), // 金色 —— 系统/底层,显眼但不刺眼
            new ModuleMeta("Resource", "00FF7F"), // 春绿 —— 资源/成功加载的隐喻
            new ModuleMeta("UI",       "00E5FF"), // 青色 —— UI 通用色,符合设计惯例
            new ModuleMeta("Combat",   "FF8C00"), // 橙色 —— 战斗/火光/紧张感
            new ModuleMeta("Data",     "C792EA"), // 紫色 —— 数据/神秘感,存档类
            new ModuleMeta("AI",       "FF4D6D"), // 玫红 —— AI/敌对/危险
        };

        // ---------------------------------------------------------------------
        //  全局开关 —— 模块掩码
        // ---------------------------------------------------------------------

        /// <summary>
        /// 当前启用的模块掩码。默认开启全部(<see cref="All"/>)。
        /// 调用方可在运行时随时修改,例如:
        /// <code>
        /// AppLog.EnabledModules = LogModule.UI | LogModule.Combat; // 只看这两个
        /// </code>
        /// </summary>
        public static LogModule EnabledModules = All;

        /// <summary>
        /// 是否启用全局日志总开关。true=启用,false=完全静默所有模块。
        /// 便于在 Console 上一键静音。
        /// </summary>
        public static bool GlobalEnabled = true;

        // ---------------------------------------------------------------------
        //  性能优化:复用 StringBuilder 避免每条日志产生 GC 分配
        // ---------------------------------------------------------------------

        // 64 是经验值,大多数日志 64 字符内足够;超出时 SetCapacity 自动扩容。
        private static readonly StringBuilder s_Sb = new StringBuilder(64);

        // ---------------------------------------------------------------------
        //  公共 API —— 三个基础级别
        //  Conditional 特性:非 UNITY_EDITOR 下,调用方的整行代码会被编译器删除,
        //  即不传入参数,也不执行方法体,真正零开销。
        // ---------------------------------------------------------------------

        /// <summary>
        /// 普通信息日志(蓝色)。
        /// </summary>
        /// <param name="module">所属业务模块。</param>
        /// <param name="message">日志内容。</param>
        /// <param name="context">可选的 Unity 对象上下文,点击日志可在 Hierarchy 中高亮。</param>
        [Conditional("UNITY_EDITOR")]
        public static void Log(LogModule module, string message, Object context = null)
        {
            if (!IsModuleEnabled(module)) return;
            Debug.Log(FormatMessage(module, message), context);
        }

        /// <summary>
        /// 警告日志 —— 转发到 <see cref="Debug.LogWarning(object,Object)"/>,
        /// 控制台显示黄色警告图标,可点击折叠堆栈。
        /// 仍保留模块颜色 Tag,便于在大量警告中快速定位模块。
        /// </summary>
        /// <param name="module">所属业务模块。</param>
        /// <param name="message">警告内容。</param>
        /// <param name="context">可选的 Unity 对象上下文。</param>
        [Conditional("UNITY_EDITOR")]
        public static void LogWarning(LogModule module, string message, Object context = null)
        {
            if (!IsModuleEnabled(module)) return;
            Debug.LogWarning(FormatMessage(module, message), context);
        }

        /// <summary>
        /// 错误日志 —— 转发到 <see cref="Debug.LogError(object,Object)"/>,
        /// 控制台显示红色错误图标。
        /// 同样保留模块颜色 Tag。
        /// 注意:Error 级别默认无视模块掩码,确保错误总能被发现;
        /// 如需完全屏蔽,请同时设置 <see cref="GlobalEnabled"/>=false。
        /// </summary>
        /// <param name="module">所属业务模块。</param>
        /// <param name="message">错误内容。</param>
        /// <param name="context">可选的 Unity 对象上下文,推荐传入出错的对象便于定位。</param>
        [Conditional("UNITY_EDITOR")]
        public static void LogError(LogModule module, string message, Object context = null)
        {
            if (!GlobalEnabled) return;
            Debug.LogError(FormatMessage(module, message), context);
        }

        // ---------------------------------------------------------------------
        //  便捷重载 —— 格式化异常 (Exception)
        // ---------------------------------------------------------------------

        /// <summary>
        /// 输出异常,自动追加堆栈。
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void LogException(LogModule module, Exception ex, Object context = null)
        {
            if (!GlobalEnabled || ex == null) return;
            Debug.LogError(FormatMessage(module, ex.Message) + "\n" + ex.StackTrace, context);
        }

        // ---------------------------------------------------------------------
        //  内部实现
        // ---------------------------------------------------------------------

        /// <summary>
        /// 检查模块是否启用。仅在 Editor 下有效(外层 Conditional 已剥离)。
        /// </summary>
        private static bool IsModuleEnabled(LogModule module)
        {
            return GlobalEnabled && (EnabledModules & module) == module && module != LogModule.None;
        }

        /// <summary>
        /// 拼接富文本前缀:`<color=#RRGGBB>[Name]</color> : message`。
        /// 复用 StringBuilder 实例以避免分配。
        /// </summary>
        private static string FormatMessage(LogModule module, string message)
        {
            // 枚举到索引的偏移:None=0,System=1,...,AI=6
            int index = LogToIndex(module);
            if (index < 0) index = 0; // 兜底,异常输入走 System 样式

            ref readonly var meta = ref s_Metas[index];

            s_Sb.Length = 0; // 重置但不释放容量,关键零 GC 技巧
            s_Sb.Append("<color=#").Append(meta.ColorHex).Append("><b>[")
                .Append(meta.Name).Append("]</b></color> : ");

            if (message != null) s_Sb.Append(message);
            return s_Sb.ToString();
        }

        /// <summary>
        /// LogModule -> s_Metas 数组下标的映射。
        /// 使用位运算提取最低有效位再查表,避免 switch。
        /// </summary>
        private static int LogToIndex(LogModule module)
        {
            // 抹掉 None(0),防止 BitOperations.Log2(0) 抛异常
            if (module == LogModule.None) return -1;
            // System=0x01 -> idx0, Resource=0x02 -> idx1 ... AI=0x20 -> idx5
            int v = (int)module;
            int idx = 0;
            while ((v >>= 1) != 0) idx++;
            return idx;
        }

        // ---------------------------------------------------------------------
        //  编辑器辅助(仅在 UNITY_EDITOR 下有意义,但 Conditional 保证 0 开销)
        // ---------------------------------------------------------------------

        /// <summary>
        /// 启用指定模块(位或)。
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void EnableModule(LogModule module) => EnabledModules |= module;

        /// <summary>
        /// 禁用指定模块(位与非)。
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void DisableModule(LogModule module) => EnabledModules &= ~module;

        /// <summary>
        /// 切换指定模块的开关状态。
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void ToggleModule(LogModule module) => EnabledModules ^= module;

        /// <summary>
        /// 重置为全模块开启(等同于 <see cref="All"/>)。
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void ResetModules() => EnabledModules = All;
    }
}
