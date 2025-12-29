/// <summary>
/// セーブ関連定数
/// </summary>
public static class SaveDataConstants
{
    // スロット数
    public const int ManualSlotCount = 3;

    // システムセーブデータのファイル名
    public const string SystemFile = "system_data.dat";

    // 未セーブデータのファイル名
    public const string UnsavedFile = "unsaved.dat";

    // セーブデータのファイル名
    public static string SlotFile(int slotNumber1to3)
        => $"save_slot_{slotNumber1to3:00}.dat";
}
