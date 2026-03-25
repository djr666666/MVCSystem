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

    [Inject] TestCtrl testCtrl;
    [Inject] TestModel testModel;

    void Start()
    {
        Ctrls.InjectTo(this);
        Models.InjectTo(this);

        testCtrl.InitValue();

        CtrlAddAndSubNum_ev();
        TestCtrl.ev += CtrlAddAndSubNum_ev;
        sub.onClick.AddListener(testCtrl.Sub);
        add.onClick.AddListener(testCtrl.Add);     
    }

    private void CtrlAddAndSubNum_ev()
    {
        value.text = testModel.GetValue().ToString();
    }

 
}
