// -----------------------------------------------------------------------------
//  AppLogExample.cs
//  ---------------------------------------------------------------------------
//  AppLog 调用示例 —— 仅供参考,可直接挂在场景中任意 GameObject 上测试。
// -----------------------------------------------------------------------------

using HarvestAndHunger.Utils.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace HarvestAndHunger.Examples
{
    /// <summary>
    /// 演示如何在不同业务场景下使用 <see cref="AppLog"/>。
    /// </summary>
    public class AppLogExample : MonoBehaviour
    {
        [SerializeField] private Button _attackButton;

        private void Awake()
        {
            // ----- 1. 模块化开关 -----
            // 场景 1:调试战斗时只想看 Combat 与 UI
            AppLog.EnabledModules = LogModule.UI | LogModule.Combat;

            // 场景 2:排查资源加载问题,只开启 Resource
            // AppLog.EnabledModules = LogModule.Resource;

            // 场景 3:全部开启(默认行为)
            // AppLog.ResetModules();

            // 场景 4:动态屏蔽单个模块
            // AppLog.DisableModule(LogModule.AI);

            // 场景 5:完全静默(连 Error 都不打印)
            // AppLog.GlobalEnabled = false;
        }

        private void Start()
        {
            // ----- 2. 基础调用示例 -----
            AppLog.Log(LogModule.System, "游戏启动,玩家 ID = 1001");

            AppLog.Log(LogModule.Resource, "正在加载玩家贴图...");

            AppLog.Log(LogModule.UI, "主菜单已打开");

            // ----- 3. 警告示例 -----
            int hp = 15;
            if (hp < 20)
                AppLog.LogWarning(LogModule.Combat, $"玩家血量过低: {hp}");

            // ----- 4. 错误示例(传入 context,点击日志可高亮对象) -----
            if (_attackButton == null)
            {
                AppLog.LogError(LogModule.UI, "未指定攻击按钮,无法绑定事件!", this);
                return;
            }

            _attackButton.onClick.AddListener(OnAttackClicked);
        }

        private void OnAttackClicked()
        {
            // ----- 5. 异常示例 -----
            try
            {
                int divisor = 0;
                int result = 10 / divisor; // 故意制造异常
                Debug.Log(result);
            }
            catch (System.Exception ex)
            {
                AppLog.LogException(LogModule.Combat, ex, this);
            }
        }
    }
}
