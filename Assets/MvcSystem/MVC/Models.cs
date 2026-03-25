using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MVC
{
    public partial class Models
    {
        // 存储所有模型的字典（类型 -> 实例）
        private static Dictionary<Type, ModelBase> _allModels = null;

        // ==================== 自动注册 ====================

        /// <summary>
        /// 自动收集所有带 [AutoModel] 标记的模型
        /// </summary>
        private static void AutoCollect()
        {
            if (_allModels != null) return;
            _allModels = new Dictionary<Type, ModelBase>();

            // 获取当前项目中的所有类型
            Assembly assembly = Assembly.GetExecutingAssembly();
            Type[] types = assembly.GetTypes();

            foreach (Type type in types)
            {
                // 检查是否有 [AutoModel] 标记
                if (type.GetCustomAttribute<AutoModelAttribute>() != null)
                {
                    // 检查是否继承自 ModelBase
                    if (typeof(ModelBase).IsAssignableFrom(type))
                    {
                        try
                        {
                            // 创建实例
                            ModelBase instance = (ModelBase)Activator.CreateInstance(type);
                            _allModels[type] = instance;
                            Debug.Log($"[自动注册 Model] {type.Name}");
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"创建 Model {type.Name} 失败: {e.Message}");
                        }
                    }
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
        /// 获取指定类型的模型
        /// </summary>
        public static T Get<T>() where T : ModelBase
        {
            AutoCollect();
            Type type = typeof(T);
            if (_allModels.TryGetValue(type, out ModelBase model))
            {
                return model as T;
            }

            Debug.LogError($"找不到模型: {type.Name}");
            return null;
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
        /// 初始化所有模型
        /// </summary>
        public static void Init()
        {
            AutoCollect();
            foreach (var item in _allModels.Values)
            {
                item.Init();
            }
            Debug.Log($"[Models] 初始化完成，共 {_allModels.Count} 个模型");
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

            foreach (var item in _allModels.Values)
            {
                item.OnLoginSuccess();
            }
        }
    }
}