using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// プレイヤーパラメータクラス
/// </summary>
[System.Serializable]
public class PlayerParameter
{

    // イベントコレクション（配列）
    public string[] ClearEvent = System.Array.Empty<string>();

    // アイテム（List構造）
    public List<OwnedItemEntry> OwnedItemList = new();

    // 快値
    public int PleasureValue = 100;

    // 不快値
    public int UnPleasantValue = 100;

    // プレイヤーの状態
    public string PlayerStateId = "state_plant";
}