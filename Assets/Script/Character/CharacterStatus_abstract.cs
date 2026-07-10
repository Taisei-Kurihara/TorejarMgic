using UniRx;
using UnityEngine;

/// <summary> キャラクターステータス基底クラス. </summary>
/// <remarks> MonoBehaviourなし. 純粋C#クラス. </remarks>
public abstract class CharacterStatus_abstract
{
    public string Name { get; protected set; }
    public int MaxHp { get; protected set; }
    public ReactiveProperty<int> Hp { get; protected set; } = new ReactiveProperty<int>();
    public int BaseAttack { get; protected set; }
    public int BaseDefense { get; protected set; }

    /// <summary> CharacterStatusManagerから読み込んだデータで自身を初期化. </summary>
    public virtual void Init(string characterName)
    {
        var data = CharacterStatusManager.Instance.GetStatusData(characterName);
        if (data == null)
        {
            Debug.LogWarning($"ステータス初期化失敗: {characterName}");
            return;
        }

        Name = data.name;
        MaxHp = data.maxHp;
        Hp.Value = data.maxHp;
        BaseAttack = data.baseAttack;
        BaseDefense = data.baseDefense;
    }

    /// <summary> HP が 0 以下かどうか. </summary>
    public bool IsDead => Hp.Value <= 0;
}
