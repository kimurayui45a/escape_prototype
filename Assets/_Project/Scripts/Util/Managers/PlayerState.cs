
using UnityEngine;

/// <summary>
/// ゲーム中のプレイヤー状態を保持するコンポーネント。
/// 簡易的に Inspector から値を編集できるように、SerializeField の裏フィールドを用意している。
/// </summary>
public class PlayerState : MonoBehaviour
{
    // ★ Inspector で編集する実体（裏フィールド）
    [Header("Player Status (Editable in Inspector)")]
    [SerializeField] int level = 1;      // ★
    [SerializeField] int exp = 0;        // ★
    [SerializeField] int hp = 100;       // ★
    [SerializeField] int maxHp = 100;    // ★
    [SerializeField] int favorValue = 0; // ★

    // 外部公開は読み取り専用（ゲーム内ロジックはプロパティを参照）
    public int Level => level;           // ★ 変更（private set をやめて裏フィールド参照に）
    public int Exp => exp;               // ★
    public int Hp => hp;                 // ★
    public int MaxHp => maxHp;           // ★
    public int FavorValue => favorValue; // ★

    void OnValidate() // ★ 追加：Inspector 変更時に自動で整合性補正
    {
        ValidateSelf();
    }

    /// <summary>
    /// PlayerData を適用して状態を更新する（ロード時など）
    /// </summary>
    public void Apply(PlayerData data)
    {
        if (data == null) data = new PlayerData(); // nullガード
        data.Validate();                           // 整合性補正

        // ★ 変更：プロパティではなく裏フィールドに反映
        level = data.Level;      // ★
        exp = data.Exp;          // ★
        hp = data.Hp;            // ★
        maxHp = data.MaxHp;      // ★
        favorValue = data.FavorValue; // ★

        ValidateSelf(); // ★ 追加：反映後も念のため補正
    }

    /// <summary>
    /// 現在の状態を PlayerData として取り出す（セーブ時など）
    /// </summary>
    public PlayerData ToData()
    {
        ValidateSelf(); // ★ 追加：取り出す直前に補正

        var data = new PlayerData
        {
            Level = level,          // ★
            Exp = exp,              // ★
            Hp = hp,                // ★
            MaxHp = maxHp,          // ★
            FavorValue = favorValue // ★
        };

        data.Validate(); // 念のためセーブ直前も整合性確認
        return data;
    }

    // ★ 追加：このコンポーネント内の値を整合性補正する
    void ValidateSelf()
    {
        level = Mathf.Max(1, level);
        exp = Mathf.Max(0, exp);          
        maxHp = Mathf.Max(1, maxHp);
        hp = Mathf.Clamp(hp, 0, maxHp);
        // favorValue は仕様に合わせて Clamp したいならここで
    }
}
















//using UnityEngine;

//public class PlayerState : MonoBehaviour
//{
//    public int Level { get; private set; } = 1;
//    public int Exp { get; private set; } = 0;
//    public int Hp { get; private set; } = 100;
//    public int MaxHp { get; private set; } = 100;
//    public int FavorValue { get; private set; } = 0;

//    public void Apply(PlayerData data)
//    {
//        if (data == null) data = new PlayerData(); // nullガード
//        data.Validate();                             // 整合性補正

//        Level = data.Level;
//        Exp = data.Exp;
//        Hp = data.Hp;
//        MaxHp = data.MaxHp;
//        FavorValue = data.FavorValue;
//    }

//    public PlayerData ToData()
//    {
//        var data = new PlayerData
//        {
//            Level = Level,
//            Exp = Exp,
//            Hp = Hp,
//            MaxHp = MaxHp,
//            FavorValue = FavorValue
//        };

//        data.Validate(); // 念のためセーブ直前も整合性確認
//        return data;
//    }
//}
