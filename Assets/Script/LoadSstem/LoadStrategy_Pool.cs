using Cysharp.Threading.Tasks;
using System.Threading;

/// <summary>
/// プールを使用したロード戦略
/// GameObjectをプールから取得・再利用
/// </summary>
public class LoadStrategy_Pool : ILoadStrategy
{
    private PoolManager _poolManager;

    public LoadStrategy_Pool(PoolManager poolManager)
    {
        _poolManager = poolManager;
    }

    public async UniTask Load(LoadRequestItem req, CancellationToken ct)
    {
        // プールからオブジェクトを取得
        req.Result = _poolManager.Get(req.Key);
        await UniTask.CompletedTask;
    }

    public UniTask Release(LoadRequestItem req, CancellationToken ct)
    {
        // プールに返却
        if (req.Result is UnityEngine.GameObject obj)
        {
            _poolManager.Release(req.Key, obj);
        }
        return UniTask.CompletedTask;
    }
}
