// -----------------------------------------------------------------------------
//  Singleton.cs
//  ---------------------------------------------------------------------------
//  泛型单例基类 —— 纯 C# 版(不依赖 MonoBehaviour / Unity 生命周期)。
//  适用场景:配置服务、对象池、存档管理、缓存服务等"无 Unity 生命周期需求"
//  但仍需要全局唯一访问点的纯逻辑类。
//
//  用法:
//      public class ConfigService : Singleton<ConfigService> { }
//      ConfigService.Instance.LoadAll();
//
//  特性:
//      · 懒加载(首次访问 Instance 时才创建)
//      · 线程安全(lock + 双重检查锁定)
//      · 跨线程单次构造保证
//
//  注意:若需要 MonoBehaviour 单例(挂场景 / DontDestroyOnLoad / 协程),
//  请使用 MonoSingleton<T>(位于 Assets/_Game/Scripts/Systems/MonoSingleton.cs)。
// -----------------------------------------------------------------------------

/// <summary>
/// 泛型单例基类(纯 C#,不依赖 MonoBehaviour)。
/// 所有需要全局唯一的纯逻辑类都应继承此类。
/// </summary>
public abstract class Singleton<T> where T : class, new()
{
    // 静态实例,所有子类共享
    private static T s_Instance;

    // 锁对象,保证多线程下只创建一个实例
    private static readonly object s_Lock = new object();

    /// <summary>
    /// 全局访问点。首次访问时才会创建实例,线程安全(lock + 双重检查)。
    /// </summary>
    public static T Instance
    {
        get
        {
            // 第一重检查:无锁,已创建则直接返回(常见路径)
            if (s_Instance != null) return s_Instance;

            // 加锁:防止多线程同时进入导致创建多个实例
            lock (s_Lock)
            {
                // 第二重检查:持锁后再判一次,避免锁内重复构造
                if (s_Instance != null) return s_Instance;

                // 懒加载:首次访问时才 new
                s_Instance = new T();
                return s_Instance;
            }
        }
    }
}
