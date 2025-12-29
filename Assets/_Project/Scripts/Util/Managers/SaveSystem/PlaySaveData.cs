//using UnityEngine;

//public class PlaySaveData : MonoBehaviour
//{
//    private readonly SaveData saveData = new SaveData();

//    // 外部参照用
//    public SaveData SaveData => saveData;

//    public bool UnSavedFlag => saveData.UnSavedFlag;
//    public SettingData SettingData => saveData.SettingData;
//    public SlotSummary[] SlotSummary => saveData.SlotSummary;
//    public PlayGameData WorkingPlayGameData => saveData.WorkingPlayGameData;


//    public int slot = Manager.Instance.GameSession.SelectedSlot;

//    // ここの処理はセッションマネージャーとかの役目じゃないか？
//    // private void Awake()
//    // {
//    //     // SaveManager.Instance は初回アクセスで生成される前提
//    //     saveData.LoadSystem();
//    //     saveData.TryLoadUnsaved();
//    // }

//    // 操作API（外からはこちらを呼ぶ）

//    // =========================================================
//    // Load
//    // =========================================================
//    /// システムデータロードメソッド
//    public void LoadSystem() => saveData.LoadSystem();

//    /// 未セーブデータロードメソッド
//    public void TryLoadUnsaved() => saveData.TryLoadUnsaved();

//    /// セーブデータロードメソッド
//    public void TryLoadSlotToWorking(int slotNumber1to3) => saveData.TryLoadSlotToWorking(slot);

//    // =========================================================
//    // Save
//    // =========================================================
//    /// システムデータセーブメソッド
//    public void SaveSystem() => saveData.SaveSystem();

//    /// 未セーブデータセーブメソッド
//    public void SaveUnsaved() => saveData.SaveUnsaved();

//    /// セーブデータセーブメソッド
//    public void SaveWorkingToSlot(int slotNumber1to3) => saveData.SaveWorkingToSlot(slot);

//    // =========================================================
//    // その他
//    // =========================================================
//    /// 未セーブデータ削除メソッド
//    public void DiscardUnsaved() => saveData.DiscardUnsaved();

//    /// 設定データ差し替えセーブメソッド（必要？？）
//    public void SetSettingData(SettingData settingData) => saveData.SaveSystem(settingData);

//    /// 未セーブフラグ更新メソッド
//    public void MarkUnsavedDirty() => saveData.MarkUnsavedDirty();

//}
