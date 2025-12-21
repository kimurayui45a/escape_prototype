using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public int Level = 1;
    public int Exp = 0;
    public int Hp = 100;
    public int MaxHp = 100;
    public int FavorValue = 0;

    public void Validate() // ロード後に壊れた値を補正
    {
        Level = Mathf.Max(1, Level);
        Exp = Mathf.Max(0, Exp);
        MaxHp = Mathf.Max(1, MaxHp);
        Hp = Mathf.Clamp(Hp, 0, MaxHp);
        // FavorValue は仕様に合わせて Clamp するならここで
    }
}