using UnityEngine;


/// <summary>
/// プレイゲームデータクラス
/// </summary>
[System.Serializable]
public class PlayGameData
{
    // 最終セーブ日時
    public long LastPlaySaveTime = 000;

    // ファイル番号（1～3）
    public int PlayFileNumber = 1;

    // 終了時にいたシーン
    public string LastSceneId = "scene_natural";

    // プレイヤーパラメータ（オブジェクト）
    public PlayerParameter PlayerParameter = new PlayerParameter();

}
