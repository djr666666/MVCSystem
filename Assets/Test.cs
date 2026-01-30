using MVC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;


public class Test : MonoBehaviour
{
    public Button sub;
    public Button add;
    public Text value;

    void Start()
    {
        Ctrls.testCtrl.InitValue();

        CtrlAddAndSubNum_ev();
        TestCtrl.ev += CtrlAddAndSubNum_ev;
        sub.onClick.AddListener(Ctrls.testCtrl.Sub);
        add.onClick.AddListener(Ctrls.testCtrl.Add);     
    }

    private void CtrlAddAndSubNum_ev()
    {
        value.text = Models.testModel.GetValue().ToString();
    }

 
}
