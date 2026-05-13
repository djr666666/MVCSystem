
using UnityEngine;

public class Game : MonoBehaviour
{
    
    void Start()
    {
        //收集所有 mvc 架构的静态类
        MVC.ModelCollector.Init();
        MVC.CtrlCollector.Init();
        // 单独加载鲁班表
        //Models.InitTables();          
    }

    
    private void OnDestroy()
    {
        //释放所有 mvc 架构的静态类
        MVC.ModelCollector.Quit();
        MVC.CtrlCollector.Quit();
    }
}
