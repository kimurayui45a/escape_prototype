using UnityEngine;


/// <summary>
/// システムセーブデータ
/// 
/// system_data.dat に入れる「システム系」
/// </summary>
[System.Serializable]
public class IndexData
{
    // 未セーブフラグ
    public bool UnSavedFlag = false;

    //  設定データ
    public SettingData SettingData = new SettingData();

    // 簡易版スロット情報
    public SlotSummary[] Slots = new SlotSummary[3]
    {
        new SlotSummary { SlotNumber = 1 },
        new SlotSummary { SlotNumber = 2 },
        new SlotSummary { SlotNumber = 3 },
    };
}