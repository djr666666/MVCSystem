

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

        //接入鲁班导表工具就把这个注释打开
        // ublic static cfg.Tables tabs;
        // private static TextAsset txt;
        // private static bool _tablesInited = false;

    // public static void InitTables()
    // { 
    //     if (_tablesInited) return;
    //     _tablesInited = true;
    //     Models.tabs = new cfg.Tables(LoadBin);
    //     Debug.Log($"<color=#FFFB04>--->(鲁班导表初始化成功)Luaban Init SucessFull <---</color>");

    // }

    // private static ByteBuf LoadBin(string file)
    // {
    //     string path = $"Assets/Project/Config/{file}.bytes";
    //     var pakeage = YooAssets.GetPackage(AOTDefine.packageName);
    //     AssetHandle handle = pakeage.LoadAssetSync<TextAsset>(path);
    //     TextAsset textAsset = handle.AssetObject as TextAsset;
    //     return ByteBuf.Wrap(textAsset.bytes);
    // }
    // private static JSONNode LoadBin_1(string file)
    // {
    //     string path = $"Assets/Project/Config/{file}.json";
    //     var pakeage = YooAssets.GetPackage(AOTDefine.packageName);
    //     var handle = pakeage.LoadSubAssetsAsync<TextAsset>(path);
    //     var txtasset = handle.SubAssetObjects as TextAsset;
    //     return JSON.Parse(txtasset.text);
    // }
    }








}



