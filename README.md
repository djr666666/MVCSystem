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

没写 `false` 的，框架会替你做完生命周期：启动时调用 `Init`，退出游戏时调用 `Quit`。界面不要去 `Release` 它。

写了 `false` 的要自己管。打开界面时 `Get`，框架才调用这个对象的 `Init`，服务器监听写在这里。关掉界面时调用 `Release`。`Release` 不是 `Quit`：它是卸字典的接口，里面会去调用你写的 `Quit()`，再把这个对象从字典拿走。反注册写在 `Quit()` 里。`Init` 注册了、`Quit` 没卸，`Release` 之后旧监听还在，下次 `Get` 又注册一个，同一条消息会进两次。

`Get` 第一次会创建、注入它自己的 `[Inject]`，并调用 `Init`。还没 `Release` 又打开，拿到的是原来那个，不会再 `Init`。对没写 `false` 的调用 `Release` 不会卸掉。

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
        // Release 不是 Quit。它会调用上面的 Quit()，再从字典拿走
        // TestModel 这种没写 false 的，Release 不会卸
        Models.Release<MakeFoodModel>();
        Ctrls.Release<MakeFoodCtrl>();
    }
}
```

两个界面共用同一个写了 `false` 的 Model 时，等最后一个关掉再 `Release`。先关的那个如果释放了，另一个手里就是已经走过 `Quit()` 的对象。

## 接口

Ctrl 和 Model 同名接口做的事一样，下面用 Model 说明。把 `Models` 换成 `Ctrls`、`ModelBase` 换成 `CtrlBase` 即可。

### 游戏入口

| 接口 | 干什么 |
| --- | --- |
| `ModelCollector.Init()` | 游戏启动时调用。创建没写 `false` 的 Model，并调用它们的 `Init()`。写了 `false` 的只登记，不创建。 |
| `CtrlCollector.Init()` | 同上，处理 Ctrl。 |
| `ModelCollector.Quit()` | 游戏退出时调用。给当前还在字典里的 Model 调用 `Quit()`。 |
| `CtrlCollector.Quit()` | 同上，处理 Ctrl。 |
| `Models.OnLoginSuccess()` | 登录成功时调用。通知当前已经创建出来的 Model。写了 `false`、还没 `Get` 的收不到，等第一次 `Get` 时补一次。 |
| `Ctrls.OnLoginSuccess()` | 同上，处理 Ctrl。 |

### 单个对象上要重写的

| 接口 | 干什么 |
| --- | --- |
| `Init()` | 准备数据和注册监听。没写 `false` 的，由启动时的 `ModelCollector.Init()` 调用。写了 `false` 的，由第一次 `Get` 调用。 |
| `Quit()` | 反注册监听、清表和列表。这不是 `Release`。没写 `false` 的，退出游戏时由 `ModelCollector.Quit()` 调用。写了 `false` 的，由 `Release` 在移出字典之前调用。 |
| `OnLoginSuccess()` | 登录成功后的处理。 |

### 界面上要调用的

| 接口 | 干什么 |
| --- | --- |
| `Models.Get<T>()` | 取 Model。字典里已有就直接返回，不会再 `Init`。写了 `false` 且字典里没有：创建、放进字典、注入、调用 `Init()`。 |
| `Ctrls.Get<T>()` | 同上，处理 Ctrl。 |
| `Models.Release<T>()` | 卸一个写了 `false` 的 Model。它不是 `Quit`。内部会调用该对象的 `Quit()`，再从字典移除。没写 `false` 的调用了也卸不掉。字典里本来没有则什么都不做。 |
| `Ctrls.Release<T>()` | 同上，处理 Ctrl。 |
| `Models.InjectTo(obj)` | 按 `[Inject]` 给 `obj` 填已经创建出来的 Model 或 Ctrl。写了 `false` 且还没 `Get` 的填不进去。 |
| `Ctrls.InjectTo(obj)` | 同上。`MonoBehaviour` 两句都要写。 |
| `C.Create<T>()` | 创建普通类并做上面两次注入。 |

`Models.Init()`、`Models.Quit()` 由收集器在启动和退出时调用，界面里不要直接调。界面卸载单个功能只用 `Release`。

## 主程和开发者注意

<font color="red">

**Init() 里注册之后，一定要在 Quit() 里反注册。调用 Release 才会帮你把监听卸掉。**

动态加载、动态卸载必须自己管好里头的注册。写了 false 的 Model / Ctrl，监听写在 Init()，反注册写在 Quit()。Release 会调用 Quit()，再从字典拿走这个对象。Quit() 里写了反注册，监听才会被卸掉。

Init 里注册了，Quit 里没写反注册：Release 之后旧对象还挂在收消息的地方。下次 Get 又注册一次，同一条消息进两次，旧对象也回收不掉。

没写 false 的，框架在启动时 Init、退出游戏时 Quit。写了 false 的，这两头都要自己写成对。

</font>
