using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MVC
{
    public static class CtrlCollector
    {
        private static List<CtrlBase> _allInstances = null;
        private static List<FieldInfo> _allFieldInfo = null;

        // 这是最简单的解决方案：不管静态实例叫什么名字，只要是 CtrlBase 类型就收集


        public static void Init()
        {
            if (_allInstances != null)
                return;

            _allInstances = new List<CtrlBase>();
            _allFieldInfo = new List<FieldInfo>();


            var assembly = Assembly.GetExecutingAssembly();

            // 获取所有类型
            var allTypes = assembly.GetTypes();

            foreach (var type in allTypes)
            {
                try
                {
                    // 查找这个类型的所有公共静态字段
                    var staticFields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

                    foreach (var field in staticFields)
                    {
                        // 如果字段类型是 CtrlBase 或其子类
                        if (typeof(CtrlBase).IsAssignableFrom(field.FieldType))
                        {
                            var instance = field.GetValue(null) as CtrlBase;
                            if (instance != null && !_allInstances.Contains(instance))
                            {
                                _allInstances.Add(instance);
                                _allFieldInfo.Add(field);

                                UnityEngine.Debug.Log($"找到静态字段: {type.Name}.{field.Name} -> {field.FieldType.Name}");
                            }
                        }
                    }

                    // 查找这个类型的所有公共静态属性
                    var staticProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Static);

                    foreach (var property in staticProperties)
                    {
                        // 如果属性类型是 CtrlBase 或其子类，并且可读
                        if (typeof(CtrlBase).IsAssignableFrom(property.PropertyType) && property.CanRead)
                        {
                            var instance = property.GetValue(null) as CtrlBase;
                            if (instance != null && !_allInstances.Contains(instance))
                            {
                                _allInstances.Add(instance);
                                UnityEngine.Debug.Log($"找到静态属性 : {type.Name}.{property.Name} -> {property.PropertyType.Name}");
                            }
                        }
                    }
                }
                catch
                {
                    // 忽略无法访问的类型
                }
            }
            Ctrls.Init();
        }



        public static List<CtrlBase> GetAllCtrlInstances()
        {
            if (_allInstances != null)
                return _allInstances;
            return _allInstances;
        }

        // 游戏退出时调用
        public static void Quit()
        {
            UnityEngine.Debug.Log("开始清理所有静态模型  Ctrl ...");

            // 步骤1：清理每个模型的内容
            Ctrls.Quit();

            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (typeof(CtrlBase).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    // 获取所有公共静态字段
                    var allStaticFields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

                    // 筛选自引用字段  
                    var selfReferenceFields = allStaticFields
                        .Where(f => f.FieldType == type)
                        .ToList();

                    foreach (var field in selfReferenceFields)
                    {
                        try
                        {
                            field.SetValue(null, null);
                            UnityEngine.Debug.Log($" 已置空 : {type.Name}.{field.Name}");
                        }
                        catch { /* 忽略错误 */ }
                    }
                }
            }

            foreach (var item in _allFieldInfo)
            {
                item.SetValue(null, null);
                UnityEngine.Debug.Log($"已置空 : {item.Name}.{item.Name}");
            }

            _allInstances.Clear();
            _allInstances = null; // 重要：列表本身也要置空
            _allFieldInfo.Clear();
            _allFieldInfo = null;

            // 步骤4：强制垃圾回收
            GC.Collect();
            GC.WaitForPendingFinalizers();

            UnityEngine.Debug.Log("所有  Ctrl   静态模型已清理完成");

        }

        // 根据类型获取实例
        public static T GetInstance<T>() where T : CtrlBase
        {
            var instances = GetAllCtrlInstances();
            return instances.OfType<T>().FirstOrDefault();
        }

        // 根据类名获取实例
        public static CtrlBase GetInstance(string className)
        {
            var instances = GetAllCtrlInstances();
            return instances.FirstOrDefault(i => i.GetType().Name == className);
        }
    }

}

