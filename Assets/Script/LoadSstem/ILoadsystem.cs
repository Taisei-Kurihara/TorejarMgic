using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// ロードシステムインターフェース
/// Strategy パターンで処理内容を分離
/// </summary>
public interface ILoadsystem
{
    /// <summary> ロード処理を実行（各リクエストのStrategyに委託） </summary>
    UniTask<LoadType> Load(LoadType loadType, Action callback, CancellationToken cancellationToken);

    /// <summary> リリース処理を実行（各リクエストのStrategyに委託） </summary>
    UniTask Release(LoadType loadType, Action callback, CancellationToken cancellationToken);
}