using UnityEngine;
using HarvestAndHunger.Utils.Logging;

namespace HarvestAndHunger.Tests
{
    public class InputDemoLogger : MonoBehaviour
    {
        private void OnEnable()
        {
            if (InputManager.Instance == null)
            {
                AppLog.LogError(LogModule.System, "找不到 InputManager!请确认场景中已挂载。", this);
                return;
            }
            // ★ 防御性 + 日志输出
            AppLog.Log(LogModule.System, "InputDemoLogger 开始订阅输入事件。", this);
            InputManager.Instance.OnMove            += HandleMove;
            InputManager.Instance.OnAttackPerformed += HandleAttackPerformed;
            InputManager.Instance.OnAttackCanceled  += HandleAttackCanceled;
            InputManager.Instance.OnRunPerformed    += HandleRunPerformed;
            InputManager.Instance.OnMenuPerformed   += HandleMenuPerformed;
        }
        private void OnDisable()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnMove            -= HandleMove;
            InputManager.Instance.OnAttackPerformed -= HandleAttackPerformed;
            InputManager.Instance.OnAttackCanceled  -= HandleAttackCanceled;
            InputManager.Instance.OnRunPerformed    -= HandleRunPerformed;
            InputManager.Instance.OnMenuPerformed   -= HandleMenuPerformed;
            AppLog.Log(LogModule.System, "InputDemoLogger 已注销全部订阅。", this);
        }
        // ============================================================
        // 处理函数:每条都用 AppLog 替代 Debug.Log
        // ============================================================
        private void HandleMove(Vector2 v)
            => AppLog.Log(LogModule.System, $"[Input] Move = ({v.x:F2}, {v.y:F2})", this);
        private void HandleAttackPerformed()
            => AppLog.Log(LogModule.System, "[Input] Attack 按下", this);
        private void HandleAttackCanceled()
            => AppLog.Log(LogModule.System, "[Input] Attack 抬起", this);
        private void HandleRunPerformed()
            => AppLog.Log(LogModule.System, "[Input] RT 按下", this);
        private void HandleMenuPerformed()
            => AppLog.Log(LogModule.System, "[Input] Menu 按下", this);
    }
}
