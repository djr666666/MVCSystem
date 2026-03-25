

namespace MVC
{
    public partial class ModelBase
    {
        //protected cfg.Tables tabs => Models.tabs;
        public virtual void Init()
        {
        }
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
            Models.InjectTo(this);
        }
    }

    public partial class Models
    {
     
    }








}



