// PlayerState.cs
using UnityEngine;

/// <summary>
/// ゲーム中のプレイヤー状態を保持するコンポーネント（Runtime側）。
/// - Inspector で編集できる裏フィールドを持つ
/// - 整合性ルール（Clamp/Max等）は PlayerData に集約し、ここでは重複実装しない
/// </summary>
public class PlayerState : MonoBehaviour
{
    [Header("Player Status (Editable in Inspector)")]
    [SerializeField] private int level = 1;
    [SerializeField] private int exp = 0;
    [SerializeField] private int hp = 100;
    [SerializeField] private int maxHp = 100;
    [SerializeField] private int favorValue = 0;

    // 外部公開は読み取り専用
    public int Level => level;
    public int Exp => exp;
    public int Hp => hp;
    public int MaxHp => maxHp;
    public int FavorValue => favorValue;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Inspector編集時の事故防止。
        // ただし整合性ルールは PlayerData のみに置くため、ここでは PlayerData に委譲する。
        PlayerData.Normalize(ref level, ref exp, ref hp, ref maxHp, ref favorValue);
    }
#endif

    /// <summary>
    /// 保存データ（PlayerData）を適用して状態を更新する（ロード時など）。
    /// </summary>
    public void Apply(PlayerData data)
    {
        if (data == null) data = new PlayerData();
        data.Validate(); // 整合性は PlayerData が保証する

        level = data.Level;
        exp = data.Exp;
        hp = data.Hp;
        maxHp = data.MaxHp;
        favorValue = data.FavorValue;
    }

    /// <summary>
    /// 現在の状態を PlayerData として取り出す（セーブ時など）。
    /// </summary>
    public PlayerData ToData()
    {
        var data = new PlayerData
        {
            Level = level,
            Exp = exp,
            Hp = hp,
            MaxHp = maxHp,
            FavorValue = favorValue
        };

        data.Validate(); // 保存前に必ず正規化（セーブデータは常にクリーンに）
        return data;
    }

    /// <summary>
    /// Runtime上で値を更新したい場合の例（任意）。
    /// 直接フィールドを触るより、入口を作ると整合性の担保が簡単になります。
    /// </summary>
    public void SetHp(int newHp)
    {
        hp = newHp;
        PlayerData.Normalize(ref level, ref exp, ref hp, ref maxHp, ref favorValue);
    }
}
