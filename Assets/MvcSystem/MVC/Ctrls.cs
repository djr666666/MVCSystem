using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MVC
{
    public partial class Ctrls
    {
        // 存储所有控制器的字典（类型 -> 实例）
        private static Dictionary<Type, CtrlBase> _allCtrls = null;

        // ==================== 自动注册 ====================

        /// <summary>
        /// 自动收集所有带 [AutoCtrl] 标记的控制器
        /// </summary>
        private static void AutoCollect()
        {
            if (_allCtrls != null) return;

            _allCtrls = new Dictionary<Type, CtrlBase>();

            // 获取当前项目中的所有类型
            Assembly assembly = Assembly.GetExecutingAssembly();
            Type[] types = assembly.GetTypes();

            foreach (Type type in types)
            {
                // 检查是否有 [AutoCtrl] 标记
                if (type.GetCustomAttribute<AutoCtrlAttribute>() != null)
                {
                    // 检查是否继承自 CtrlBase
                    if (typeof(CtrlBase).IsAssignableFrom(type))
                    {
                        try
                        {
                            // 创建实例
                            CtrlBase instance = (CtrlBase)Activator.CreateInstance(type);
                            _allCtrls[type] = instance;
                            Debug.Log($"[自动注册 Ctrl] {type.Name}");
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"创建 Ctrl {type.Name} 失败: {e.Message}");
                        }
                    }
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
        /// 获取指定类型的控制器
        /// </summary>
        public static T Get<T>() where T : CtrlBase
        {
            AutoCollect();
            Type type = typeof(T);
            if (_allCtrls.TryGetValue(type, out CtrlBase ctrl))
            {
                return ctrl as T;
            }

            Debug.LogError($"找不到控制器: {type.Name}");
            return null;
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
        /// 初始化所有控制器
        /// </summary>
        public static void Init()
        {
            AutoCollect();
            foreach (var item in _allCtrls.Values)
            {
                item.Init();
            }
            Debug.Log($"[Ctrls] 初始化完成，共 {_allCtrls.Count} 个控制器");
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

            foreach (var item in _allCtrls.Values)
            {
                item.OnLoginSuccess();
            }
        }
    }
}