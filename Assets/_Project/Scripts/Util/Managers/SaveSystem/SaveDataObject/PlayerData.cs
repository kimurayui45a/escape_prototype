// PlayerData.cs
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public int Level = 1;
    public int Exp = 0;
    public int Hp = 100;
    public int MaxHp = 100;
    public int FavorValue = 0;

    /// <summary>
    /// ロード後・セーブ前に呼んで、データを正規化する。
    /// 整合性ルールはこのクラスに集約する（Single Source of Truth）。
    /// </summary>
    public void Validate()
    {
        Normalize(ref Level, ref Exp, ref Hp, ref MaxHp, ref FavorValue);
    }

    /// <summary>
    /// 正規化ルール本体（唯一の場所）。
    /// PlayerState からも利用できるよう static にしている。
    /// </summary>
    public static void Normalize(ref int level, ref int exp, ref int hp, ref int maxHp, ref int favorValue)
    {
        level = Mathf.Max(1, level);
        exp = Mathf.Max(0, exp);
        maxHp = Mathf.Max(1, maxHp);
        hp = Mathf.Clamp(hp, 0, maxHp);

        // favorValue は仕様に合わせて Clamp したいならここで
        // favorValue = Mathf.Clamp(favorValue, 0, 999);
    }
}
