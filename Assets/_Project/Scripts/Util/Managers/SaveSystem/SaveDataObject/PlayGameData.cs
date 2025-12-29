using UnityEngine;


/// <summary>
/// プレイゲームデータクラス
/// </summary>
[System.Serializable]
public class PlayGameData
{
    // 最終セーブ日時
    public long LastPlaySaveTime = 000;

    // スロット番号（1～3、ファイル番号）
    public int PlaySlotNumber = 1;

    // 終了時にいたシーン
    public string LastSceneId = "scene_natural";

    // プレイヤーパラメータ（オブジェクト）
    public PlayerParameter PlayerParameter = new PlayerParameter();

}
