//public sealed class SettingsController
//{
//    // このコントローラが操作対象とする 本体データ（SaveData）
//    private readonly SaveData saveData;

//    // SettingsController は SaveData からしか作らせない（作る経路を制限する）
//    internal SettingsController(SaveData saveData) => this.saveData = saveData;

//    public void SetMute(bool value)
//    {
//        saveData.SettingData.MuteFlag = value;
//        saveData.SaveSystem();
//    }

//    public void SetBgm(int value)
//    {
//        saveData.SettingData.BGM = Mathf.Clamp(value, 1, 100);
//        saveData.SaveSystem();
//    }

//    public void SetSe(int value)
//    {
//        saveData.SettingData.SE = value;
//        saveData.SaveSystem();
//    }

//    public void SetTextSpeed(int value)
//    {
//        saveData.SettingData.TextSpeed = Mathf.Clamp(value, 1, 5);
//        saveData.SaveSystem();
//    }
//}


//// 使用例
//// playSaveData.SaveData.Settings.SetBgm(80);


