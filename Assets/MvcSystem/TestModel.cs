using MVC;
using UnityEngine;

public class TestModel : ModelBase
{
    public int value = 100;
    public static float adfasfsa;

    public override void Init()
    {
        base.Init();
        Debug.Log(" AddAndSubModel Init()   ");
    }


    public void SetValue(int value)
    {
        this.value = value;
    }

    public int GetValue()
    {
        return value;
    }

}
