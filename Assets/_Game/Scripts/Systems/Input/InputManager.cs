using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // <- 新增

public class InputManager : MonoSingleton<InputManager>
{
    private PlayerControls _controls;

    public event System.Action<Vector2> OnMove;
    public event System.Action OnAttackPerformed; // 攻击按下瞬间
    public event System.Action OnAttackCanceled;  // 攻击抬起瞬间
    public event System.Action OnDashPerformed;   // Dash 按下瞬间
    public event System.Action OnMenuPerformed;   // 菜单按下瞬间

    // TODO (3): 评估事件与输入状态缓存的职责划分 —— 事件驱动 vs 状态查询,见 OnMovePerformed。
    // TODO (4): 后续根据实际业务需求再决定是否优化输入系统架构。

    protected override void OnSingletonAwake() 
    {
        _controls = new PlayerControls();

        _controls.GamePlay.Move.performed += OnMovePerformed;
        _controls.GamePlay.Move.canceled += OnMoveCanceled;
        _controls.GamePlay.Attack.performed += OnAttackPerformedHandler;
        _controls.GamePlay.Attack.canceled += OnAttackCanceledHandler;
        _controls.GamePlay.Dash.performed += OnDashPerformedHandler;
        _controls.GamePlay.Menu.performed += OnMenuPerformedHandler;
    }

    // 注意:MonoSingleton 已实现 Awake / OnApplicationQuit / OnDestroy 的 virtual 版本。
    //      子类若需扩展,必须 override 并调用 base。本类使用 OnSingletonDestroy 钩子代替。
    private void OnEnable() => _controls?.GamePlay.Enable();
    private void OnDisable() => _controls?.GamePlay.Disable();

    protected override void OnSingletonDestroy()
    {
        if (_controls == null) return;
        _controls.GamePlay.Move.performed -= OnMovePerformed;
        _controls.GamePlay.Move.canceled -= OnMoveCanceled;
        _controls.GamePlay.Attack.performed -= OnAttackPerformedHandler;
        _controls.GamePlay.Attack.canceled -= OnAttackCanceledHandler;
        _controls.GamePlay.Dash.performed -= OnDashPerformedHandler;
        _controls.GamePlay.Menu.performed -= OnMenuPerformedHandler;
        _controls.GamePlay.Disable();
        _controls = null;
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        // TODO (1): 在这里增加圆形 Deadzone,处理手柄摇杆漂移。
        // TODO (2): 增加每帧缓存清洗后的输入数据,让业务层可直接读取 InputManager.Instance.Move。
        Vector2 v = ctx.ReadValue<Vector2>();
        OnMove?.Invoke(v);
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        OnMove?.Invoke(Vector2.zero);
    }

    private void OnAttackPerformedHandler(InputAction.CallbackContext _) => OnAttackPerformed?.Invoke();
    private void OnAttackCanceledHandler(InputAction.CallbackContext _) => OnAttackCanceled?.Invoke();
    private void OnDashPerformedHandler(InputAction.CallbackContext _) => OnDashPerformed?.Invoke();
    private void OnMenuPerformedHandler(InputAction.CallbackContext _) => OnMenuPerformed?.Invoke();
}
