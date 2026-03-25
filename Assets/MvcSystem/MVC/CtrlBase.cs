

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
        /// <summary>
        /// 手动触发依赖注入（在 Init 中调用）
        /// </summary>
        protected void Inject()
        {
            Ctrls.InjectTo(this);
        }
    }
    public partial class Ctrls
    {
     
    }

}


