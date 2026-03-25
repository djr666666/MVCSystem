using System;

namespace MVC
{
    /// <summary>
    /// 标记这个类需要自动注册到 Ctrls 中 [AutoCtrl]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoCtrlAttribute : Attribute
    {
    }


    /// <summary>
    /// 标记这个类需要自动注册到 Model 中 [AutoModel]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoModelAttribute : Attribute
    {
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
