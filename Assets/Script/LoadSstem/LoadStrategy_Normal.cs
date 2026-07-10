using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine;

/// <summary>
/// 通常のロード戦略
/// Addressablesを使用した標準的なロード処理
/// </summary>
public class LoadStrategy_Normal : ILoadStrategy
{
    public async UniTask Load(LoadRequestItem req, CancellationToken ct)
    {
        try
        {
            // Addressablesを使用してロード
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
        catch (System.Exception ex)
        {
            Debug.LogError($"Load exception for {req.Key}: {ex.Message}");
        }

        await UniTask.CompletedTask;
    }

    public UniTask Release(LoadRequestItem req, CancellationToken ct)
    {
        try
        {
            // Addressablesを使用してリリース
            if (req.Handle is AsyncOperationHandle handle)
            {
                Addressables.Release(handle);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Release exception for {req.Key}: {ex.Message}");
        }

        return UniTask.CompletedTask;
    }
}
