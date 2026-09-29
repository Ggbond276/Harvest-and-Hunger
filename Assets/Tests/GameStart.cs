using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// AppLog 位于命名空间 HarvestAndHunger.Utils.Logging 下,这里显式 using 后可直接使用。
// 注意:AppLog 的所有公共 API 都用 [Conditional("UNITY_EDITOR")] 标记,
// 因此本脚本在 Release 构建中不会有任何运行时开销。
using HarvestAndHunger.Utils.Logging;

/// <summary>
/// GameStart —— 挂载到场景后,运行时会在 Unity Console 中输出一整套 AppLog 演示日志。
/// 用法:把本脚本挂到场景中任意 GameObject 上,点击 Play 即可在 Console 里看到效果。
///
/// 测试覆盖:
///   1) 全部 6 个模块 (System / Resource / UI / Combat / Data / AI) × 3 个级别 (Log / LogWarning / LogError)
///   2) 运行时动态切换模块开关
///   3) 异常专用通道 LogException
///   4) GlobalEnabled 总开关静默验证
/// </summary>
public class GameStart : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        RunAppLogSmokeTest();
    }

    // Update is called once per frame
    void Update()
    {

    }

    // -------------------------------------------------------------------------
    //  日志冒烟测试 —— 在 Start 中调用一次,即可在 Console 看到完整覆盖。
    // -------------------------------------------------------------------------
    private static void RunAppLogSmokeTest()
    {
        // ---- 第 1 步:确认处于全模块开启状态 ----
        AppLog.ResetModules();
        AppLog.GlobalEnabled = true;

        Debug.Log("========== AppLog 冒烟测试开始 ==========");

        // ---- 第 2 步:6 个模块 × 3 个级别 = 18 条基础日志 ----
        AppLog.Log    (LogModule.System,   "System  模块 / 普通日志");
        AppLog.Log    (LogModule.Resource, "Resource 模块 / 普通日志");
        AppLog.Log    (LogModule.UI,       "UI 模块 / 普通日志");
        AppLog.Log    (LogModule.Combat,   "Combat 模块 / 普通日志");
        AppLog.Log    (LogModule.Data,     "Data 模块 / 普通日志");
        AppLog.Log    (LogModule.AI,       "AI 模块 / 普通日志");

        AppLog.LogWarning(LogModule.System,   "System  模块 / 警告");
        AppLog.LogWarning(LogModule.Resource, "Resource 模块 / 警告");
        AppLog.LogWarning(LogModule.UI,       "UI 模块 / 警告");
        AppLog.LogWarning(LogModule.Combat,   "Combat 模块 / 警告");
        AppLog.LogWarning(LogModule.Data,     "Data 模块 / 警告");
        AppLog.LogWarning(LogModule.AI,       "AI 模块 / 警告");

        AppLog.LogError (LogModule.System,   "System  模块 / 错误");
        AppLog.LogError (LogModule.Resource, "Resource 模块 / 错误");
        AppLog.LogError (LogModule.UI,       "UI 模块 / 错误");
        AppLog.LogError (LogModule.Combat,   "Combat 模块 / 错误");
        AppLog.LogError (LogModule.Data,     "Data 模块 / 错误");
        AppLog.LogError (LogModule.AI,       "AI 模块 / 错误");

        // ---- 第 3 步:演示运行时动态切换模块开关 ----
        // 只保留 Combat 与 Data,其余应被屏蔽(Log/Warning 不应再出现;Error 仍会输出)
        Debug.Log("---------- 切换 EnabledModules = Combat | Data ----------");
        AppLog.EnabledModules = LogModule.Combat | LogModule.Data;

        AppLog.Log    (LogModule.UI,     "这条 UI Log 不应出现");
        AppLog.Log    (LogModule.Combat, "这条 Combat Log 应出现");
        AppLog.Log    (LogModule.Data,   "这条 Data Log 应出现");
        AppLog.Log    (LogModule.AI,     "这条 AI Log 不应出现");
        AppLog.LogError(LogModule.UI,     "这条 UI Error 仍会出现(Error 绕过模块掩码)");

        // ---- 第 4 步:演示异常通道 ----
        Debug.Log("---------- 异常专用通道 ----------");
        try
        {
            // 故意制造一个除零异常
            int a = 1 / int.Parse("0");
        }
        catch (System.Exception ex)
        {
            AppLog.LogException(LogModule.Combat, ex);
        }

        // ---- 第 5 步:恢复全模块开启 + 全局静音验证 ----
        AppLog.ResetModules();
        AppLog.GlobalEnabled = false;
        Debug.Log("---------- GlobalEnabled = false 后,以下应完全无输出 ----------");
        AppLog.Log    (LogModule.System, "静音测试:不应出现");
        AppLog.LogError(LogModule.System, "静音测试:不应出现(连 Error 也被屏蔽)");

        AppLog.GlobalEnabled = true; // 恢复
        Debug.Log("========== AppLog 冒烟测试结束 ==========");
    }
}