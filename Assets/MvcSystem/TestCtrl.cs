

using MVC;

public class TestCtrl : CtrlBase
{
    public override void Init()
    {
        base.Init();
        //初始化数据
    }

    public override void OnLoginSuccess()
    {
        base.OnLoginSuccess();
        //登陆成功
    }

    public override void Quit()
    {
        //退出处理
        base.Quit();
    }

    public void InitValue()
    {
        Models.testModel.SetValue(66);
    }

    public void Add()
    {
        int num = Models.testModel.GetValue();
        Models.testModel.SetValue(num + 1);
        ev?.Invoke();
    }
    public void Sub()
    {
        int num = Models.testModel.GetValue();
        Models.testModel.SetValue(num - 1);
        ev?.Invoke();
    }

    public delegate void event_1();
    public static event event_1 ev;

}
