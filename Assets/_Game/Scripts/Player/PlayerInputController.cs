using UnityEngine;

/// <summary>
/// 连接现有 InputManager 输入 与 现有 SamuraiL7 Animator 参数。
///
/// 驱动 6 个可区分状态:
///   Idle        : Speed=0              (Animator: Walk/Run -> Idle by Speed<0.01)
///   Walk        : Speed=1              (Animator: Idle -> Walk by Speed>0.01)
///   Run         : Speed=2              (Animator: Walk -> Run by Speed>1.5)
///   Attack      : Attack trigger       (AnyState -> Attack)
///   WalkAttack  : WalkAttack trigger   (AnyState -> WalkAttack)
///   RunAttack   : RunAttack trigger    (AnyState -> RunAttack)
///
/// Hurt / IsDead 由受击系统触发,不在本类职责范围。
///
/// 关键约束:
/// - 不创建代码层状态机,只喂参数。
/// - 不动 .controller,所有 transition / blend tree 都用现有的。
/// - 松开按键后 DirX/DirY 不主动清零,Animator Idle BlendTree 用最后方向,
///   实现"左走 -> 松手 -> 左 idle"朝向保留效果。
/// </summary>
public class PlayerInputController : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private float _walkSpeed = 5f;
    [SerializeField] private float _runSpeedMultiplier = 2f;

    // Animator 参数名(集中常量,避免散落字符串)
    private const string P_Speed          = "Speed";
    private const string P_DirX           = "DirX";
    private const string P_DirY           = "DirY";
    private const string P_Attack         = "Attack";
    private const string P_WalkAttack     = "WalkAttack";
    private const string P_RunAttack      = "RunAttack";

    private void OnEnable()
    {
        if (InputManager.Instance == null) return;
        InputManager.Instance.OnMove             += HandleMove;
        InputManager.Instance.OnAttackPerformed  += HandleAttackPerformed;
    }

    private void OnDisable()
    {
        if (InputManager.Instance == null) return;
        InputManager.Instance.OnMove             -= HandleMove;
        InputManager.Instance.OnAttackPerformed  -= HandleAttackPerformed;
    }

    private void Update()
    {
        if (_animator == null) return;
        if (InputManager.Instance == null) return;

        // 读取输入状态
        Vector2 move = InputManager.Instance.Move;
        bool isRunning = InputManager.Instance.IsRunning;
        bool isMoving = move.sqrMagnitude > 0.0001f;

        // 决定 Animator Speed:
        //   移动 + RT      -> 2  (>1.5 触发 Walk->Run,Run 状态 BlendTree)
        //   移动           -> 1  (>0.01 触发 Idle->Walk)
        //   没动           -> 0  (<0.01 触发 Walk->Idle,Run->Idle)
        float animSpeed = !isMoving ? 0f : (isRunning ? 2f : 1f);
        _animator.SetFloat(P_Speed, animSpeed);

        // 朝向:只要有移动就更新方向;松开时保留最后值,不归零。
        // 这样 Animator Idle 状态的 BlendTree 会用最后一次方向,实现朝向保留。
        if (isMoving)
        {
            _animator.SetFloat(P_DirX, move.x);
            _animator.SetFloat(P_DirY, move.y);
        }

        // 物理移动:Rigidbody2D.velocity = 单位方向 * 速度
        // 没动时速度归零(Run->Idle/Walk->Idle 同时生效)。
        if (_rigidbody != null)
        {
            float speed = isMoving ? _walkSpeed * (isRunning ? _runSpeedMultiplier : 1f) : 0f;
            _rigidbody.velocity = move.normalized * speed;
        }
    }

    private void HandleMove(Vector2 move)
    {
        // Animator 参数的连续更新在 Update 中处理,这里保留事件订阅作为占位
        // (便于后续扩展为输入驱动的瞬时反应,例如落地硬直等)。
    }

    private void HandleAttackPerformed()
    {
        if (_animator == null) return;
        if (InputManager.Instance == null) return;

        Vector2 move = InputManager.Instance.Move;
        bool isMoving = move.sqrMagnitude > 0.0001f;
        bool isRunning = InputManager.Instance.IsRunning;

        // 决定攻击种类:同一时刻只发一个 trigger,AnyState transition 切到对应状态。
        if (isMoving && isRunning)       _animator.SetTrigger(P_RunAttack);
        else if (isMoving && !isRunning) _animator.SetTrigger(P_WalkAttack);
        else                             _animator.SetTrigger(P_Attack);
    }
}