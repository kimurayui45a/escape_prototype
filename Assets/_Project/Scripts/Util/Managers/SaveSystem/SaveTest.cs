
using UnityEngine;

/// <summary>
/// セーブ、ロード処理の呼び出し用のコンポーネント。
/// Inspector値を持ち、Save/Loadを呼び出してデータが復元されるか試す想定。
/// </summary>
public class SaveTest : MonoBehaviour
{
    [SerializeField]
    int day = 0;

    [SerializeField] PlayerState playerState;

    // 実際に保存/ロード対象とするデータ
    SaveData saveData = new SaveData();

    // ここでSaveDataのPlayerStateにアクセスできるインサートを各
    public PlayerState PlayerState => playerState;

    /// <summary>
    /// 保存処理を呼ぶ（UIボタン等から呼び出す想定）
    /// 注意：このサンプルのままだと day を saveData に反映していないので、
    ///       いつも初期値（0）が保存される点に注意。
    /// </summary>
    public void Save()
    {
        if (playerState != null)
        {
            // PlayerState → SaveData
            saveData.CaptureFromRuntime(day, playerState);
        }
        else
        {
            // PlayerState が無い場合は SaveParam を埋める（テスト用）
            saveData.SaveParam.Day = day;
        }

        // セーブ実行（ファイル名は saveData.dat）
        SaveManager.Instance.Save(saveData, "saveData.dat");
    }

    /// <summary>
    /// ロード処理を呼ぶ（UIボタン等から呼び出す想定）
    /// ファイルが存在する場合のみ読み込む。
    /// </summary>
    public void Load()
    {
        // ロード実行
        var ret = SaveManager.Instance.Load("saveData.dat");

        // セーブデータがないならreturn
        if (ret == null) return;

        saveData = ret;

        if (playerState != null)
        {
            saveData.ApplyToRuntime(playerState, out day);
        }
        else
        {
            // ロード結果を保持
            saveData = ret;

            // SaveData → Inspector用変数へ反映
            day = saveData.SaveParam.Day;

            Debug.Log("Load:" + saveData.SaveParam.Day);
        }
    }
}
