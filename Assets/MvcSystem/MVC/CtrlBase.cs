

using System.Xml.Serialization;

namespace MVC
{
    public partial class CtrlBase
    {

        //处理一开始数据
        public virtual void Init()
        {
        }

        //清理数据根据不同 ctrl 清理自己的数据
        public virtual void Quit()
        {

        }

        public virtual void OnLoginSuccess()
        { 
        
        }
    }
    public partial class Ctrls
    {
        public static void Quit()
        {
            foreach (var item in CtrlCollector.GetAllCtrlInstances())
                item.Quit();
        }
        public static void Init()
        {
            foreach (var item in CtrlCollector.GetAllCtrlInstances())
                item.Init();
        }

        public static void OnLoginSuccess()
        {
            foreach (var item in CtrlCollector.GetAllCtrlInstances())
                item.OnLoginSuccess();
        }
    }









}


