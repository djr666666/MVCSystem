using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MVC
{
    public partial class Ctrls
    {
        // 已经创建的控制器（类型 -> 实例）。常驻的启动就在这里，按需的 Get 之后才进来。
        private static Dictionary<Type, CtrlBase> _allCtrls = null;

        // [AutoCtrl(false)]。启动只登记类型，不 new。
        private static HashSet<Type> _lazyTypes = null;

        // Ctrls.Init 已经跑过。之后按需创建的控制器要当场 Init。
        private static bool _startupInitDone;

        // OnLoginSuccess 已经广播过。之后按需创建的控制器要补一次。
        private static bool _loginBroadcast;

        // ==================== 自动注册 ====================

        /// <summary>
        /// 扫描 [AutoCtrl]。resident 为 true 的立刻创建；false 的只登记，等 Get。
        /// </summary>
        private static void AutoCollect()
        {
            if (_allCtrls != null) return;

            _allCtrls = new Dictionary<Type, CtrlBase>();
            _lazyTypes = new HashSet<Type>();

            // 获取当前项目中的所有类型
            Assembly assembly = Assembly.GetExecutingAssembly();
            Type[] types = assembly.GetTypes();

            foreach (Type type in types)
            {
                AutoCtrlAttribute attr = type.GetCustomAttribute<AutoCtrlAttribute>();
                if (attr == null) continue;
                if (!typeof(CtrlBase).IsAssignableFrom(type) || type.IsAbstract) continue;

                if (!attr.Resident)
                {
                    _lazyTypes.Add(type);
                    Debug.Log($"[按需登记 Ctrl] {type.Name}");
                    continue;
                }

                try
                {
                    CtrlBase instance = (CtrlBase)Activator.CreateInstance(type);
                    _allCtrls[type] = instance;
                    Debug.Log($"[自动注册 Ctrl] {type.Name}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"创建 Ctrl {type.Name} 失败: {e.Message}");
                }
            }

            // 所有控制器创建完成后，进行依赖注入（此时 Models 可能还未初始化）
            foreach (var ctrl in _allCtrls.Values)
            {
                InjectTo(ctrl);
            }
        }

        // ==================== 依赖注入 ====================

        /// <summary>
        /// 注入依赖到指定对象（支持 Model 和 Ctrl 互相注入）
        /// </summary>
        public static void InjectTo(object target)
        {
            if (target == null) return;

            Type type = target.GetType();

            // 注入字段
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                if (Attribute.IsDefined(field, typeof(InjectAttribute)))
                {
                    Type fieldType = field.FieldType;
                    object instance = GetInstance(fieldType);

                    if (instance != null)
                    {
                        field.SetValue(target, instance);
                        Debug.Log($"[注入] {type.Name}.{field.Name} -> {instance.GetType().Name}");
                    }
                    else
                    {
                        Debug.LogWarning($"[注入失败] {type.Name}.{field.Name} 找不到类型 {fieldType.Name}");
                    }
                }
            }

            // 注入属性
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (PropertyInfo prop in properties)
            {
                if (Attribute.IsDefined(prop, typeof(InjectAttribute)) && prop.CanWrite)
                {
                    Type propType = prop.PropertyType;
                    object instance = GetInstance(propType);

                    if (instance != null)
                    {
                        prop.SetValue(target, instance);
                        Debug.Log($"[注入] {type.Name}.{prop.Name} -> {instance.GetType().Name}");
                    }
                }
            }
        }

        /// <summary>
        /// 获取实例（优先从 Ctrls 查找，其次从 Models 查找）
        /// </summary>
        private static object GetInstance(Type targetType)
        {
            // 先从 Ctrls 中查找
            if (_allCtrls != null && _allCtrls.TryGetValue(targetType, out CtrlBase ctrlInstance))
            {
                return ctrlInstance;
            }

            // 再从 Models 中查找
            if (Models.TryGetModel(targetType, out ModelBase modelInstance))
            {
                return modelInstance;
            }

            return null;
        }

        // ==================== 对外接口 ====================

        /// <summary>
        /// 获取所有控制器实例
        /// </summary>
        public static List<CtrlBase> GetAllCtrlInstances()
        {
            AutoCollect();
            return new List<CtrlBase>(_allCtrls.Values);
        }

        /// <summary>
        /// 获取控制器。已经在字典里就直接返回，不会再次 Init。
        /// [AutoCtrl(false)] 第一次 Get 时才创建、注入，并在启动 Init 已完成后调用 Init。
        /// </summary>
        public static T Get<T>() where T : CtrlBase
        {
            AutoCollect();
            Type type = typeof(T);

            if (_allCtrls.TryGetValue(type, out CtrlBase found))
                return (T)found;

            if (!_lazyTypes.Contains(type))
            {
                Debug.LogError($"找不到控制器: {type.Name}。常驻用 [AutoCtrl]，按需用 [AutoCtrl(false)]");
                return null;
            }

            try
            {
                T created = (T)CreateLazy(type);
                Debug.Log($"[按需创建 Ctrl] {type.Name}");
                return created;
            }
            catch (Exception e)
            {
                Exception inner = e.InnerException ?? e;
                Debug.LogError($"创建 Ctrl {type.Name} 失败: {inner.Message}");
                return null;
            }
        }

        /// <summary>
        /// 创建单个按需控制器。启动 Init 还没跑完时不在这里 Init，交给 Init 的循环。
        /// </summary>
        private static CtrlBase CreateLazy(Type type)
        {
            CtrlBase created = (CtrlBase)Activator.CreateInstance(type);
            _allCtrls[type] = created;
            try
            {
                InjectTo(created);
                if (_startupInitDone)
                {
                    created.Init();
                    if (_loginBroadcast)
                        created.OnLoginSuccess();
                }
                return created;
            }
            catch
            {
                _allCtrls.Remove(type);
                throw;
            }
        }

        /// <summary>
        /// 卸掉按需控制器：Quit，再从字典移除。
        /// 常驻控制器（[AutoCtrl] / [AutoCtrl(true)]）忽略，继续留在内存里。
        /// </summary>
        public static void Release<T>() where T : CtrlBase
        {
            AutoCollect();
            Type type = typeof(T);

            if (!_lazyTypes.Contains(type))
            {
                Debug.LogWarning($"[释放忽略] {type.Name} 不是 [AutoCtrl(false)]，不会卸掉");
                return;
            }

            if (!_allCtrls.TryGetValue(type, out CtrlBase ctrl))
                return;

            ctrl.Quit();
            _allCtrls.Remove(type);
            Debug.Log($"[释放 Ctrl] {type.Name}");
        }

        /// <summary>
        /// 尝试获取控制器（不打印错误日志）
        /// </summary>
        internal static bool TryGetCtrl(Type type, out CtrlBase ctrl)
        {
            AutoCollect();
            return _allCtrls.TryGetValue(type, out ctrl);
        }

        /// <summary>
        /// 初始化启动时已经创建的控制器。标了 false 的不在这里 Init。
        /// </summary>
        public static void Init()
        {
            AutoCollect();
            _startupInitDone = true;
            foreach (var item in new List<CtrlBase>(_allCtrls.Values))
            {
                item.Init();
            }
            Debug.Log($"[Ctrls] 初始化完成，常驻 {_allCtrls.Count} 个，按需 {_lazyTypes.Count} 个（未创建）");
        }

        /// <summary>
        /// 清理所有控制器
        /// </summary>
        public static void Quit()
        {
            if (_allCtrls == null) return;

            foreach (var item in _allCtrls.Values)
            {
                item.Quit();
            }
            Debug.Log($"[Ctrls] 清理完成");
        }

        /// <summary>
        /// 登录成功回调
        /// </summary>
        public static void OnLoginSuccess()
        {
            if (_allCtrls == null) return;

            _loginBroadcast = true;
            foreach (var item in new List<CtrlBase>(_allCtrls.Values))
            {
                item.OnLoginSuccess();
            }
        }
    }
}