using Cysharp.Threading.Tasks;
using System.Threading;

/// <summary>
/// ロード戦略インターフェース
/// 異なるロード方式（通常ロード、プールなど）を実装
/// </summary>
public interface ILoadStrategy
{
    /// <summary> ロード処理 </summary>
    UniTask Load(LoadRequestItem req, CancellationToken ct);

    /// <summary> リリース処理 </summary>
    UniTask Release(LoadRequestItem req, CancellationToken ct);
}
