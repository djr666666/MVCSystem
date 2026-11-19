# MVCSystem

Model 管数据，Ctrl 管操作，界面只负责显示和把点击交出去。带 `[AutoModel]` / `[AutoCtrl]` 的类会自动收集。没写 `false` 的，游戏一开始就创建并 `Init`。写了 `false` 的，一开始不创建，进自己的界面才初始化，离开再卸掉。

## 初始化

游戏入口先收集。`ModelCollector.Init()` 会创建所有没写 `false` 的 Model，并调用它们的 `Init`。`CtrlCollector.Init()` 对 Ctrl 做同样的事。退出时再 `Quit`，已经创建出来的 Model 和 Ctrl 都会走到自己的 `Quit`。

```csharp
public class Game : MonoBehaviour
{
    void Start()
    {
        // 收集所有 mvc 架构的静态类
        MVC.ModelCollector.Init();
        MVC.CtrlCollector.Init();
    }

    private void OnDestroy()
    {
        // 释放所有 mvc 架构的静态类
        MVC.ModelCollector.Quit();
        MVC.CtrlCollector.Quit();
    }
}
```

收集要发生在界面注入之前。`Game` 和界面如果都写在 `Start` 里，把 `Game` 的脚本执行顺序放到界面前面。

登录成功后调用一次下面两句。已经创建的 Model、Ctrl 会收到 `OnLoginSuccess`。写了 `false`、此时还没 `Get` 的不会收到；等它以后第一次 `Get` 时会补一次。

```csharp
MVC.Models.OnLoginSuccess();
MVC.Ctrls.OnLoginSuccess();
```

## 写 Model 和 Ctrl

Model 继承 `ModelBase`，类上加 `[AutoModel]`。数据放在这里。`Init` 里做启动时要准备的事，`Quit` 里清掉。

```csharp
using MVC;
using UnityEngine;

[AutoModel]
public class TestModel : ModelBase
{
    public int value = 100;

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
```

Ctrl 继承 `CtrlBase`，类上加 `[AutoCtrl]`。要改数据就注入对应的 Model，不要在界面里直接改。`Init`、`OnLoginSuccess`、`Quit` 按自己的功能去写。

```csharp
using MVC;

[AutoCtrl]
public class TestCtrl : CtrlBase
{
    [Inject] TestModel testModel;

    public override void Init()
    {
        base.Init();
        // 初始化数据
    }

    public override void OnLoginSuccess()
    {
        base.OnLoginSuccess();
        // 登陆成功
    }

    public override void Quit()
    {
        // 退出处理
        base.Quit();
    }

    public void InitValue()
    {
        testModel.SetValue(66);
    }

    public void Add()
    {
        int num = testModel.GetValue();
        testModel.SetValue(num + 1);
        ev?.Invoke();
    }

    public void Sub()
    {
        int num = testModel.GetValue();
        testModel.SetValue(num - 1);
        ev?.Invoke();
    }

    public delegate void event_1();
    public static event event_1 ev;
}
```

`[AutoModel]` 和 `[AutoCtrl]` 不写参数，启动时就会创建。写成 `[AutoModel(true)]`、`[AutoCtrl(true)]` 也一样。

## 注入依赖

字段或属性标 `[Inject]`，类型是某个 Model 或 Ctrl。收集完成时，Model 和 Ctrl 之间会自动注入。`TestCtrl` 上的 `testModel` 就是这样来的，不用自己 `new`。

界面是 `MonoBehaviour`，不会自动注入。在用到这些字段之前写上这两句：

```csharp
Ctrls.InjectTo(this);
Models.InjectTo(this);
```

普通类不是 `MonoBehaviour` 时，用 `C.Create` 创建，它会把两边都注入进去：

```csharp
var obj = C.Create<SomeClass>();
```

注入只会拿到已经创建出来的对象。没写 `false` 的，启动收集之后就能注入。写了 `false` 的，启动时还不存在，这时注入会是空的。要先 `Get` 把它创建出来，再 `InjectTo`。

## 界面里怎么用

界面持有 Ctrl 和 Model，自己不改数据。`Start` 里先注入，再通过 Ctrl 改值，用 Model 读值刷新显示。按钮只调用 Ctrl。

下面就是项目里的 `Test`。启动后 `TestModel.value` 先是 100，`InitValue` 把它改成 66 并显示出来。点加、点减都走 `TestCtrl`，Ctrl 改完 Model 再触发 `ev`，界面把文本更新成最新的数。

```csharp
using MVC;
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
```

`TestModel`、`TestCtrl` 没写 `false`，`Game` 里 `Init` 的时候就已经在了，所以界面这里只注入、不创建。

## 写了 false 的界面

全程都要用的，像上面的 `TestModel`，不写 `false`。只有打开某个界面才用的，写成 `false`。启动时扫到 `false` 会跳过，不创建，也不调用 `Init`。

| 写法 | 一开始 | 界面里 |
| --- | --- | --- |
| `[AutoModel]`、`[AutoCtrl]` | 创建并 `Init`，一直留着 | 像 `Test` 那样直接 `InjectTo` |
| `[AutoModel(false)]`、`[AutoCtrl(false)]` | 不创建 | `OnEnable` 里先 `Get` 再注入，`OnDisable` 里 `Release` |

`Get` 第一次会创建、注入它自己的 `[Inject]`，并调用 `Init`。还没 `Release` 又打开，拿到的是原来那个，不会再 `Init`。`Release` 会调用 `Quit`，再从字典里拿走。对没写 `false` 的调用 `Release` 不会卸掉。

界面写法仍和 `Test` 一样用 `[Inject]`，只是多了打开时创建、关闭时释放。`Get` 要写在 `InjectTo` 前面，不然字段还是空的。

```csharp
using MVC;
using UnityEngine;

[AutoModel(false)]
public class MakeFoodModel : ModelBase
{
    public override void Init()
    {
        base.Init();
        // 进做菜才走到这里：读这功能自己的表、建列表
    }

    public override void Quit()
    {
        base.Quit();
        // 离开时把表、列表、资源引用清掉
    }
}

[AutoCtrl(false)]
public class MakeFoodCtrl : CtrlBase
{
    [Inject] MakeFoodModel model;

    public override void Init()
    {
        base.Init();
    }

    public override void Quit()
    {
        base.Quit();
    }
}

public class Pnl_MakeFood : MonoBehaviour
{
    [Inject] MakeFoodCtrl ctrl;
    [Inject] MakeFoodModel model;

    void OnEnable()
    {
        // 先创建，再注入。字典里已经有就直接拿回来，不会再 Init
        Models.Get<MakeFoodModel>();
        Ctrls.Get<MakeFoodCtrl>();
        Ctrls.InjectTo(this);
        Models.InjectTo(this);
    }

    void OnDisable()
    {
        // 只卸做菜这一对。TestModel 这种没写 false 的仍留在内存里
        Models.Release<MakeFoodModel>();
        Ctrls.Release<MakeFoodCtrl>();
    }
}
```

两个界面共用同一个写了 `false` 的 Model 时，等最后一个关掉再 `Release`。先关的那个如果释放了，另一个手里就是已经 `Quit` 过的对象。
