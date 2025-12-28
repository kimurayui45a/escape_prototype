using UnityEngine;


/// <summary>
/// ゲームプレイ中に全体で参照する値の管理
/// </summary>
public class GameSessionManager : MonoBehaviour
{
    // セーブファイルの番号（1～3）
    public int SelectedSlot { get; private set; } = 0;

    public void SelectSlot(int slot)
    {
        SelectedSlot = Mathf.Clamp(slot, 1, 3);
        Debug.Log("[GameSession] 現在のファイル番号" + SelectedSlot);
    }

    // スロットのリセットボタン
    public void ClearSlot()
    {
        SelectedSlot = 0;
        Debug.Log("[GameSession] 現在のファイル番号 => 0");
    }
}


// -----使用例-----
// int slot = Manager.Instance.GameSession.SelectedSlot;
// UI管理
// Manager.Instance.GameSession.SelectSlot(2);


