using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{
    
    void Start()
    {
        //收集所有 mvc 架构的静态类
        MVC.ModelCollector.Init();
        MVC.CtrlCollector.Init();
    }

    
    private void OnDestroy()
    {
        //释放所有 mvc 架构的静态类
        MVC.ModelCollector.Quit();
        MVC.CtrlCollector.Quit();
    }
}
