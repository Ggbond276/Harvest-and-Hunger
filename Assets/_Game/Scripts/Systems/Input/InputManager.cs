using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家输入集中管理器。
///
/// 设计原则(KISS):
/// - 每个动作提供两类信息:状态查询(IsRunning/IsAttacking) + 触发事件(performed/canceled)。
/// - 状态查询用于"当前按下了什么",事件用于"刚刚发生了什么"。
/// - 不做 Deadzone、不做平滑、不做输入缓存——只做"原样转译" + 事件分发。
///
/// 6 个可区分的输入组合:
///   Idle        : !IsRunning && !IsAttacking && Move==0
///   Walk        : !IsRunning && !IsAttacking && Move!=0
///   Run         :  IsRunning && !IsAttacking && Move!=0
///   Attack      : !IsRunning &&  IsAttacking && Move==0
///   WalkAttack  : !IsRunning &&  IsAttacking && Move!=0
///   RunAttack   :  IsRunning &&  IsAttacking && Move!=0
///
/// 注意:RT(Run)和 X(Attack)同时按下时,两个状态都为 true,
/// 由 PlayerInputController 根据 IsRunning && IsAttacking 自行选择 RunAttack 动画。
/// </summary>
public class InputManager : MonoSingleton<InputManager>
{
    private PlayerControls _controls;

    // ---- 公开状态查询(供业务层每帧读取)----
    /// <summary>最近一次 Move 值(WASD / 左摇杆)。松开按键时归零。</summary>
    public Vector2 Move { get; private set; }

    /// <summary>当前 RT 是否按住(Run 动作的 isPressed)。</summary>
    public bool IsRunning { get; private set; }

    /// <summary>当前 X 是否按住(Attack 动作的 isPressed)。</summary>
    public bool IsAttacking { get; private set; }

    // ---- 公开事件(供业务层订阅瞬时行为)----
    public event System.Action<Vector2> OnMove;          // Move 值变化(含 canceled)
    public event System.Action OnRunPerformed;           // RT 按下瞬间
    public event System.Action OnRunCanceled;            // RT 抬起瞬间
    public event System.Action OnAttackPerformed;        // X 按下瞬间
    public event System.Action OnAttackCanceled;      // X 抬起瞬间
    public event System.Action OnMenuPerformed;          // Menu 按下瞬间

    protected override void OnSingletonAwake()
    {
        _controls = new PlayerControls();

        _controls.GamePlay.Move.performed += OnMovePerformed;
        _controls.GamePlay.Move.canceled += OnMoveCanceled;
        _controls.GamePlay.Attack.performed += OnAttackPerformedHandler;
        _controls.GamePlay.Attack.canceled += OnAttackCanceledHandler;
        _controls.GamePlay.Run.performed += OnRunPerformedHandler;
        _controls.GamePlay.Run.canceled += OnRunCanceledHandler;
        _controls.GamePlay.Menu.performed += OnMenuPerformedHandler;
    }

    private void OnEnable() => _controls?.GamePlay.Enable();
    private void OnDisable() => _controls?.GamePlay.Disable();

    protected override void OnSingletonDestroy()
    {
        if (_controls == null) return;
        _controls.GamePlay.Move.performed -= OnMovePerformed;
        _controls.GamePlay.Move.canceled -= OnMoveCanceled;
        _controls.GamePlay.Attack.performed -= OnAttackPerformedHandler;
        _controls.GamePlay.Attack.canceled -= OnAttackCanceledHandler;
        _controls.GamePlay.Run.performed -= OnRunPerformedHandler;
        _controls.GamePlay.Run.canceled -= OnRunCanceledHandler;
        _controls.GamePlay.Menu.performed -= OnMenuPerformedHandler;
        _controls.GamePlay.Disable();
        _controls = null;
    }

    // ============================================================
    // 回调处理
    // ============================================================

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        Move = ctx.ReadValue<Vector2>();
        OnMove?.Invoke(Move);
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        Move = Vector2.zero;
        OnMove?.Invoke(Move);
    }

    private void OnAttackPerformedHandler(InputAction.CallbackContext ctx)
    {
        IsAttacking = true;
        OnAttackPerformed?.Invoke();
    }

    private void OnAttackCanceledHandler(InputAction.CallbackContext ctx)
    {
        IsAttacking = false;
        OnAttackCanceled?.Invoke();
    }

    private void OnRunPerformedHandler(InputAction.CallbackContext ctx)
    {
        IsRunning = true;
        OnRunPerformed?.Invoke();
    }

    private void OnRunCanceledHandler(InputAction.CallbackContext ctx)
    {
        IsRunning = false;
        OnRunCanceled?.Invoke();
    }

    private void OnMenuPerformedHandler(InputAction.CallbackContext _) => OnMenuPerformed?.Invoke();
}