// -----------------------------------------------------------------------------
//  MonoSingleton.cs
//  ---------------------------------------------------------------------------
//  泛型单例基类 —— MonoBehaviour 版。
//  适用场景:InputManager / AudioManager / SceneLoader 等需要挂场景、
//  使用协程或 DontDestroyOnLoad 的全局基础设施。
//
//  用法:
//      public class InputManager : MonoSingleton<InputManager> { }
//      InputManager.Instance.SomeMethod();
//
//  特性:
//      · 自动查找或创建(无需手动挂载,首次访问 Instance 时自检)
//      · 防止重复实例(场景里手抖挂两个,后者自杀)
//      · 跨场景保留(DontDestroyOnLoad),子类可通过 IsPersistent 关闭
//
//  注意:若不需要 Unity 生命周期,请改用 Singleton<T>(纯 C# 版)。
// -----------------------------------------------------------------------------

using UnityEngine;

/// <summary>
/// 泛型单例基类(MonoBehaviour 版)。
/// 所有需要全局唯一的 MonoBehaviour Manager 都应继承此类。
/// </summary>
public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    // 静态实例,所有子类共享
    private static T s_Instance;

    // 锁对象,保证多线程下只创建一个实例
    private static readonly object s_Lock = new object();

    // 标记:应用退出时,不再创建新实例,避免报错
    private static bool s_ApplicationIsQuitting = false;

    /// <summary>
    /// 全局访问点。首次访问时如果场景里没有,会自动创建一个空 GameObject 挂上。
    /// </summary>
    public static T Instance
    {
        get
        {
            // 已退出运行时,直接返回 null,防止访问已销毁对象
            if (s_ApplicationIsQuitting)
            {
                return null;
            }

            // 加锁:防止多线程同时进入导致创建多个实例
            lock (s_Lock)
            {
                if (s_Instance != null) return s_Instance;

                // 场景里找一找(可能别人已经挂上了)
                s_Instance = FindObjectOfType<T>();
                if (s_Instance != null) return s_Instance;

                // 实在没有,就动态创建一个
                GameObject go = new GameObject($"[Singleton] {typeof(T).Name}");
                s_Instance = go.AddComponent<T>();
                return s_Instance;
            }
        }
    }

    /// <summary>
    /// 子类重写此属性可决定是否跨场景保留。
    /// 默认 true:典型 Manager(如 InputManager / AudioManager)都是跨场景的。
    /// </summary>
    protected virtual bool IsPersistent => true;

    /// <summary>
    /// 子类可以重写此方法,用于在 Awake 时执行初始化(替代原本的 Awake)。
    /// </summary>
    protected virtual void OnSingletonAwake() { }

    // -------------------------------------------------------------------------
    //  Unity 生命周期
    //  注意:子类如果也写 Awake(),必须显式调用 base.Awake()
    // -------------------------------------------------------------------------

    protected virtual void Awake()
    {
        // 防多实例:场景里手抖挂了俩,后者自杀
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_Instance = this as T;

        // 跨场景保留(若未挂父节点才生效)
        if (IsPersistent && transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }

        // 把"应用退出"事件钩上,用于释放静态引用
        OnSingletonAwake();
    }

    protected virtual void OnApplicationQuit()
    {
        s_ApplicationIsQuitting = true;
    }

    /// <summary>
    /// 子类可以重写此方法,用于在 OnDestroy 时执行清理(替代原本的 OnDestroy)。
    /// 基类会先调用本钩子,再清理静态引用,保证子类与基类的清理逻辑成对出现。
    /// </summary>
    protected virtual void OnSingletonDestroy() { }

    protected virtual void OnDestroy()
    {
        // 先让子类清理自己的资源(事件订阅 / 非托管资源 / 缓存等)
        OnSingletonDestroy();

        // 只有当前实例是单例本人时才清空静态引用
        if (s_Instance == this)
        {
            s_Instance = null;
        }
    }
}
