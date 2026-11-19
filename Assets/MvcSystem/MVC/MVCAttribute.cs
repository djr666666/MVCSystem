using System;

namespace MVC
{
    /// <summary>
    /// 标记这个类由 Ctrls 管理。
    /// [AutoCtrl] 或 [AutoCtrl(true)]：启动时创建并 Init，一直留在内存，Release 不会卸掉。
    /// [AutoCtrl(false)]：启动不创建。第一次 Get 才创建并 Init，Release 时 Quit 并移出字典。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoCtrlAttribute : Attribute
    {
        public bool Resident { get; }

        public AutoCtrlAttribute(bool resident = true)
        {
            Resident = resident;
        }
    }


    /// <summary>
    /// 标记这个类由 Models 管理。
    /// [AutoModel] 或 [AutoModel(true)]：启动时创建并 Init，一直留在内存，Release 不会卸掉。
    /// [AutoModel(false)]：启动不创建。第一次 Get 才创建并 Init，Release 时 Quit 并移出字典。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoModelAttribute : Attribute
    {
        public bool Resident { get; }

        public AutoModelAttribute(bool resident = true)
        {
            Resident = resident;
        }
    }


    /// <summary>
    /// 标记需要注入的字段或属性 [Inject]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class InjectAttribute : Attribute
    {
    }

 
    public static class C
    {
        public static T Create<T>() where T : class, new()
        {
            T instance = new T();
            Ctrls.InjectTo(instance);  // 传入 instance
            Models.InjectTo(instance);
            return instance;
        }
    }


    //monobehavior 类 和普通类 需要写这个
    //Ctrls.InjectTo(this);
    //Models.InjectTo(this);
}
