

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
    }

    public partial class Models
    {
        public static void Init()
        {

            foreach (var item in ModelCollector.GetAllModelInstances())
                item.Init();
        }

        public static void Quit()
        {
            foreach (var item in ModelCollector.GetAllModelInstances())
                item.Quit();
        }
        public static void OnLoginSuccess()
        {
            foreach (ModelBase model in ModelCollector.GetAllModelInstances())
                model.OnLoginSuccess();
        }
    }








}



