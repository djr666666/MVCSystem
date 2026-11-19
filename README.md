# MVCSystem

轻量 MVC。Model / Ctrl 自动注册、依赖注入，并按功能做动静分离。上百个功能脚本不用全部留在内存里。

## 能力

- 带 `[AutoModel]` / `[AutoCtrl]` 的类自动收集
- 启动时 `Init`，退出时 `Quit`
- `[Inject]` 做 Model 和 Ctrl 互相注入
- 常驻和按需分开：没写 `false` 的一开始就初始化，写了 `false` 的进界面才初始化

## 初始化就看有没有 false

启动时会扫所有 `[AutoModel]`、`[AutoCtrl]`。这个类初始化不初始化，只看它自己有没有写 `false`。

| 写法 | 一开始 | 之后 |
| --- | --- | --- |
| `[AutoModel]`、`[AutoModel(true)]`，Ctrl 同理 | 创建并 `Init`，一直留在内存 | `Release` 不会卸掉 |
| `[AutoModel(false)]`、`[AutoCtrl(false)]` | 不创建，也不 `Init` | 自己的界面 `Get` 时才创建并 `Init`，离开时 `Release` |

没标 `false` 的，算常驻，游戏一启动就初始化。标了 `false` 的，启动时跳过，等进那个界面再单独初始化，离开再单独卸掉。这样上百个功能脚本不用一起留在内存里。

## 这次改了什么

`[AutoModel]`、`[AutoCtrl]` 可以带一个参数，决定要不要在启动时初始化。

- 不写参数，或者写 `true`：一开始就初始化。
- 写 `false`：一开始不初始化。进界面 `Get`，离开 `Release`。

按需对象的一次生命周期：

1. 打开界面，调用 `Get`。字典里没有就创建、注入，并调用 `Init`。
2. 还没 `Release` 又打开，`Get` 拿到原来的实例，不会再次 `Init`。
3. 离开界面，调用 `Release`。先 `Quit`，再从字典拿走，表、列表和资源引用就可以被回收。
4. 登录广播已经发生过之后才 `Get` 到的，会补一次 `OnLoginSuccess`。

下面这一组就是「个别功能自己加载、自己卸载」。玩家模型不写参数，启动就在，做菜界面进出不会动它。做菜的 Model 和 Ctrl 标 `false`，只有打开做菜界面才创建，关掉就各自 `Quit` 并卸掉。

```csharp
// 常驻：启动创建，一直留着。做菜界面 Release 不到它。
[AutoModel]
public class PlayerModel : ModelBase { }

// 按需：启动不创建。
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
    public override void Init() { base.Init(); }
    public override void Quit() { base.Quit(); }
}

public class Pnl_MakeFood : MonoBehaviour
{
    MakeFoodModel model;
    MakeFoodCtrl ctrl;

    void OnEnable()
    {
        // 字典里没有就创建，并调用 Init
        // 已经在字典里就直接拿回来，不会再 Init 一次
        model = Models.Get<MakeFoodModel>();
        ctrl = Ctrls.Get<MakeFoodCtrl>();
    }

    void OnDisable()
    {
        // 只卸做菜这一对。PlayerModel 仍留在内存里。
        Models.Release<MakeFoodModel>();
        Ctrls.Release<MakeFoodCtrl>();
        model = null;
        ctrl = null;
    }
}
```

再打开做菜，会新建一对并重新 `Init`。其它标了 `false` 的功能此刻还没自己 `Get`，仍然不在内存里。

## 使用约定

- 玩家、背包这种全程要用的，用 `[AutoModel]` / `[AutoCtrl]`，不写 `false`。
- 做菜、活动这种打开才用的，用 `[AutoModel(false)]` / `[AutoCtrl(false)]`。数据放在 `Init` 里建，在 `Quit` 里清。
- 两个界面共用同一个按需模型时，等最后一个关闭再 `Release`。先关的那个释放之后，另一个手里就是已经 `Quit` 过的对象。
- 常驻对象上的 `[Inject]` 不会把按需模型提前创建出来。按需模型在 `Get` 时，可以注入已经存在的常驻对象。
