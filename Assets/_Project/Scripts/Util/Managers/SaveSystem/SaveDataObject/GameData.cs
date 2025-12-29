using System;

/// <summary>
/// 進捗セーブデータ
/// 
/// save_slot_XX.dat、unsaved.dat に入れる「プレイ実績系」
/// </summary>
[Serializable]
public class GameData
{
    public PlayGameData PlayGameData = new PlayGameData();
}