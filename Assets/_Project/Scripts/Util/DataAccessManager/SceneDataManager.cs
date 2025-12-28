using System;
using UnityEngine;


/// <summary>
/// シーンデータ管理クラス
/// 現在のシーン情報を管理するランタイムクラス。
/// - ゲーム中は「Unityのシーン名（SceneName）」を受け取り、マスターから SceneId を解決して保持する
/// - ロードは「SceneId」を受け取り、マスターから SceneSO を引いて復元する
/// - セーブは「SceneId」を返す
/// - シーン遷移自体（LoadScene等）は行わない
/// </summary>
public class SceneDataManager
{
    private readonly SceneDataSO sceneMaster;

    // セーブ対象（保存する値）
    private string lastSceneId;

    // 任意：デバッグや表示に使いたければ保持（セーブ対象外）
    private string lastSceneName;

    // 変更フラグ
    public bool IsDirty { get; private set; }

    /// <summary>変更通知（任意）</summary>
    public event Action<string> OnChangedSceneId;

    public SceneDataManager(SceneDataSO master)
    {
        sceneMaster = master;
    }

    // 読み取り専用
    /// <summary>最後に記録された SceneId（セーブ対象）</summary>
    public string LastSceneId => lastSceneId;
    /// <summary>最後に記録された SceneName（任意・セーブ対象外）</summary>
    public string LastSceneName => lastSceneName;

    /// <summary>
    /// ゲーム中：Unityの現在シーン名を渡して、SceneId を解決・保持する。
    /// 例：SceneManager.GetActiveScene().name を渡す想定。
    /// </summary>
    public bool TrySetCurrentSceneByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;

        // masterがあるなら SceneName -> SceneSO で存在チェック兼ねて解決
        if (sceneMaster != null)
        {
            if (!sceneMaster.TryGetBySceneName(sceneName, out var def))
            {
                Debug.LogError($"存在しない SceneName: {sceneName}");
                return false;
            }

            // 変更なしなら何もしない（Dirty/通知も不要）
            if (lastSceneId == def.SceneId) return false;

            lastSceneId = def.SceneId;
            lastSceneName = def.SceneName;
        }
        else
        {
            // masterが無い場合：SceneIdが解決できないので、
            // ここでは運用を決める必要がある（例：SceneNameを暫定的にIDとして扱う等）
            Debug.LogError("SceneDataSO(master) is null. Cannot resolve SceneId from SceneName.");
            return false;
        }

        IsDirty = true;
        OnChangedSceneId?.Invoke(lastSceneId);
        return true;
    }

    /// <summary>
    /// ロード：セーブに入っている SceneId を適用する（I/Oなし）。
    /// ロード直後は未変更扱いなので IsDirty を false に戻す。
    /// </summary>
    public void LoadFromSaveSceneId(string savedSceneId)
    {
        if (string.IsNullOrEmpty(savedSceneId))
        {
            lastSceneId = null;
            lastSceneName = null;
            IsDirty = false;
            OnChangedSceneId?.Invoke(lastSceneId);
            return;
        }

        // masterがあるなら検証して復元
        if (sceneMaster != null)
        {
            if (!sceneMaster.TryGetBySceneId(savedSceneId, out var def))
            {
                Debug.LogError($"Unknown SceneId: {savedSceneId}");
                lastSceneId = null;
                lastSceneName = null;
                IsDirty = false;
                OnChangedSceneId?.Invoke(lastSceneId);
                return;
            }

            lastSceneId = def.SceneId;
            lastSceneName = def.SceneName;
        }
        else
        {
            // master無しなら検証できないのでそのまま保持（運用次第）
            lastSceneId = savedSceneId;
            lastSceneName = null;
        }

        IsDirty = false;
        OnChangedSceneId?.Invoke(lastSceneId);
    }

    /// <summary>
    /// セーブ：保存すべき SceneId を返す。
    /// </summary>
    public string ToSaveSceneId()
    {
        return lastSceneId;
    }

    public void ClearDirty() => IsDirty = false;
}


// ------呼び出し使用例------
// 起動時
//sceneDataManager = new SceneDataManager(sceneDataSO);

//// ロード時：セーブからSceneIdを適用
//sceneDataManager.LoadFromSaveSceneId(save.Param.LastSceneId);

//// ゲーム中：現在シーン名から記録更新（SceneIdを内部に保持）
//var currentName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
//sceneDataManager.TrySetCurrentSceneByName(currentName);

//// セーブ時：SceneIdを書き戻す
//save.Param.LastSceneId = sceneDataManager.ToSaveSceneId();


//　シーン名称取得API
//using UnityEngine.SceneManagement;
//string sceneName = SceneManager.GetActiveScene().name;