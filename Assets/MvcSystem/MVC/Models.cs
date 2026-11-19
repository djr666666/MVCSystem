using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MVC
{
    public partial class Models
    {
        // 已经创建的模型（类型 -> 实例）。常驻的启动就在这里，按需的 Get 之后才进来。
        private static Dictionary<Type, ModelBase> _allModels = null;

        // [AutoModel(false)]。启动只登记类型，不 new。
        private static HashSet<Type> _lazyTypes = null;

        // Models.Init 已经跑过。之后按需创建的模型要当场 Init。
        private static bool _startupInitDone;

        // OnLoginSuccess 已经广播过。之后按需创建的模型要补一次。
        private static bool _loginBroadcast;

        // ==================== 自动注册 ====================

        /// <summary>
        /// 扫描 [AutoModel]。resident 为 true 的立刻创建；false 的只登记，等 Get。
        /// </summary>
        private static void AutoCollect()
        {
            if (_allModels != null) return;
            _allModels = new Dictionary<Type, ModelBase>();
            _lazyTypes = new HashSet<Type>();

            // 获取当前项目中的所有类型
            Assembly assembly = Assembly.GetExecutingAssembly();
            Type[] types = assembly.GetTypes();

            foreach (Type type in types)
            {
                AutoModelAttribute attr = type.GetCustomAttribute<AutoModelAttribute>();
                if (attr == null) continue;
                if (!typeof(ModelBase).IsAssignableFrom(type) || type.IsAbstract) continue;

                if (!attr.Resident)
                {
                    _lazyTypes.Add(type);
                    Debug.Log($"[按需登记 Model] {type.Name}");
                    continue;
                }

                try
                {
                    ModelBase instance = (ModelBase)Activator.CreateInstance(type);
                    _allModels[type] = instance;
                    Debug.Log($"[自动注册 Model] {type.Name}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"创建 Model {type.Name} 失败: {e.Message}");
                }
            }

            // 所有模型创建完成后，进行依赖注入（此时 Ctrls 可能还未初始化）
            foreach (var model in _allModels.Values)
            {
                InjectTo(model);
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
        /// 获取实例（优先从 Models 查找，其次从 Ctrls 查找）
        /// </summary>
        private static object GetInstance(Type targetType)
        {
            // 先从 Models 中查找
            if (_allModels != null && _allModels.TryGetValue(targetType, out ModelBase modelInstance))
            {
                return modelInstance;
            }

            // 再从 Ctrls 中查找
            if (Ctrls.TryGetCtrl(targetType, out CtrlBase ctrlInstance))
            {
                return ctrlInstance;
            }

            return null;
        }

        // ==================== 对外接口 ====================

        /// <summary>
        /// 获取所有模型实例
        /// </summary>
        public static List<ModelBase> GetAllModelInstances()
        {
            AutoCollect();
            return new List<ModelBase>(_allModels.Values);
        }

        /// <summary>
        /// 获取模型。已经在字典里就直接返回，不会再次 Init。
        /// [AutoModel(false)] 第一次 Get 时才创建、注入，并在启动 Init 已完成后调用 Init。
        /// </summary>
        public static T Get<T>() where T : ModelBase
        {
            AutoCollect();
            Type type = typeof(T);

            if (_allModels.TryGetValue(type, out ModelBase found))
                return (T)found;

            if (!_lazyTypes.Contains(type))
            {
                Debug.LogError($"找不到模型: {type.Name}。常驻用 [AutoModel]，按需用 [AutoModel(false)]");
                return null;
            }

            try
            {
                T created = (T)CreateLazy(type);
                Debug.Log($"[按需创建 Model] {type.Name}");
                return created;
            }
            catch (Exception e)
            {
                Exception inner = e.InnerException ?? e;
                Debug.LogError($"创建 Model {type.Name} 失败: {inner.Message}");
                return null;
            }
        }

        /// <summary>
        /// 创建单个按需模型。先放进字典，Init 里再 Get 自己时拿到的是这一个。
        /// 启动 Init 还没跑完时不在这里 Init，交给 Init 的循环。
        /// </summary>
        private static ModelBase CreateLazy(Type type)
        {
            ModelBase created = (ModelBase)Activator.CreateInstance(type);
            _allModels[type] = created;
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
                _allModels.Remove(type);
                throw;
            }
        }

        /// <summary>
        /// 卸掉按需模型：Quit，再从字典移除，之后可以被 GC。
        /// 常驻模型（[AutoModel] / [AutoModel(true)]）忽略，继续留在内存里。
        /// 还没 Release 又 Get，拿到的仍是原来那个。
        /// </summary>
        public static void Release<T>() where T : ModelBase
        {
            AutoCollect();
            Type type = typeof(T);

            if (!_lazyTypes.Contains(type))
            {
                Debug.LogWarning($"[释放忽略] {type.Name} 不是 [AutoModel(false)]，不会卸掉");
                return;
            }

            if (!_allModels.TryGetValue(type, out ModelBase model))
                return;

            model.Quit();
            _allModels.Remove(type);
            Debug.Log($"[释放 Model] {type.Name}");
        }

        /// <summary>
        /// 尝试获取模型（不打印错误日志）
        /// </summary>
        internal static bool TryGetModel(Type type, out ModelBase model)
        {
            AutoCollect();
            return _allModels.TryGetValue(type, out model);
        }

        /// <summary>
        /// 初始化启动时已经创建的模型。标了 false 的不在这里 Init。
        /// </summary>
        public static void Init()
        {
            AutoCollect();
            // 先置位，这样某个常驻 Init 里去 Get 按需模型时，那个模型会当场 Init，且不会被下面的循环再 Init 一次
            _startupInitDone = true;
            foreach (var item in new List<ModelBase>(_allModels.Values))
            {
                item.Init();
            }
            Debug.Log($"[Models] 初始化完成，常驻 {_allModels.Count} 个，按需 {_lazyTypes.Count} 个（未创建）");
        }

        /// <summary>
        /// 清理所有模型
        /// </summary>
        public static void Quit()
        {
            if (_allModels == null) return;

            foreach (var item in _allModels.Values)
            {
                item.Quit();
            }
            Debug.Log($"[Models] 清理完成");
        }

        /// <summary>
        /// 登录成功回调
        /// </summary>
        public static void OnLoginSuccess()
        {
            if (_allModels == null) return;

            _loginBroadcast = true;
            foreach (var item in new List<ModelBase>(_allModels.Values))
            {
                item.OnLoginSuccess();
            }
        }
    }
}