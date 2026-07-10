/// <summary> 1ブロック分の効果データ. </summary>
[System.Serializable]
public class BlockData
{
    /// <summary> 効果種別. </summary>
    public BlockEffectType EffectType;

    /// <summary> 効果の識別子 (例: "fire", "boost_atk", "hp_regen"). </summary>
    public string EffectId;

    /// <summary> 効果量 (CSV編集で設定可能). </summary>
    public float EffectValue;

    public BlockData()
    {
        EffectType = BlockEffectType.None;
        EffectId = "";
        EffectValue = 0f;
    }

    public BlockData(BlockEffectType effectType, string effectId, float effectValue)
    {
        EffectType = effectType;
        EffectId = effectId;
        EffectValue = effectValue;
    }

    public BlockData Clone()
    {
        return new BlockData(EffectType, EffectId, EffectValue);
    }
}
