using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Addressablesをベースとしたロードシステム実装
/// 各リクエストのStrategyに処理を委託（Strategy パターン）
/// </summary>
public class Loadsystem_AddressablesManager : ILoadsystem
{
    public async UniTask<LoadType> Load(LoadType loadType, Action callback, CancellationToken ct)
    {
        // 各リクエストのStrategyに処理を委託
        var tasks = loadType.Requests.Select(async req =>
        {
            if (req.Strategy != null)
            {
                // Strategyが処理内容を決定（プール、通常ロード等）
                await req.Strategy.Load(req, ct);
            }
            else
            {
                // Strategy未設定時は Addressables でロード
                await LoadViaAddressables(req, ct);
            }
        });

        await UniTask.WhenAll(tasks);

        callback?.Invoke();

        return loadType;
    }

    public async UniTask Release(LoadType loadType, Action callback, CancellationToken ct)
    {
        // 各リクエストのStrategyに処理を委託
        var tasks = loadType.Requests.Select(async req =>
        {
            if (req.Strategy != null)
            {
                // Strategyがリリース処理を決定
                await req.Strategy.Release(req, ct);
            }
            else
            {
                // Strategy未設定時は Addressables でリリース
                ReleaseViaAddressables(req);
            }
        });

        await UniTask.WhenAll(tasks);

        callback?.Invoke();

        await UniTask.CompletedTask;
    }

    /// <summary> Addressablesを使用したロード </summary>
    private async UniTask LoadViaAddressables(LoadRequestItem req, CancellationToken ct)
    {
        var handle = Addressables.LoadAssetAsync<object>(req.Key);

        await handle.ToUniTask(cancellationToken: ct);

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            req.Result = handle.Result;
            req.Handle = handle;
        }
        else
        {
            Debug.LogError($"Load failed: {req.Key}");
        }
    }

    /// <summary> Addressablesを使用したリリース </summary>
    private void ReleaseViaAddressables(LoadRequestItem req)
    {
        if (req.Handle is AsyncOperationHandle handle)
        {
            Addressables.Release(handle);
        }
    }
}
