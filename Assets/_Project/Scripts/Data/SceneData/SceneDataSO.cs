using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 全シーン（SceneSO）を管理するマスターSO。
/// - SceneId（string）をキーに SceneSO を取得できるようにキャッシュ（Dictionary）を構築する。
/// - 取得側は SceneId を渡すだけで、対応する SceneSO 一式を受け取れる。
/// </summary>
[CreateAssetMenu(menuName = "Master/Scene DataSO")]
public class SceneDataSO : ScriptableObject
{
    /// <summary>
    /// 登録されているシーン定義一覧。
    /// </summary>
    public List<SceneSO> SceneList = new();

    /// <summary>
    /// ※ 不要の可能性アリ（Sceneは名前からidを記録する方針のため）
    /// SceneId → SceneSO の高速参照用キャッシュ。
    /// シリアライズ対象ではないため、実行時に構築する。
    /// </summary>
    private Dictionary<string, SceneSO> idMap;

    // SceneName -> SceneSO
    private Dictionary<string, SceneSO> nameMap;

    /// <summary>
    /// 指定した SceneId に対応する SceneSO を取得する。
    /// - 見つかったら true / 見つからなければ false。
    /// - キャッシュ未構築の場合は内部で構築する。
    /// </summary>
    /// <param name="sceneId">取得したいシーンID</param>
    /// <param name="scene">取得結果（成功時に SceneSO が入る）</param>
    public bool TryGetBySceneId(string sceneId, out SceneSO scene)
    {
        scene = null;
        if (string.IsNullOrEmpty(sceneId)) return false;

        if (idMap == null) BuildCache();
        return idMap.TryGetValue(sceneId, out scene);
    }

    /// <summary>
    /// SceneName から SceneSO を取得する。
    /// - Unityのシーン名（Build Settingsの名前）と一致する想定。
    /// </summary>
    public bool TryGetBySceneName(string sceneName, out SceneSO scene)
    {
        scene = null;
        if (string.IsNullOrEmpty(sceneName)) return false;

        if (nameMap == null) BuildCache();
        return nameMap.TryGetValue(sceneName, out scene);
    }

    /// <summary>
    /// SceneList の内容からキャッシュ（Dictionary）を構築する。
    /// - null要素やID未設定はスキップする。
    /// </summary>
    private void BuildCache()
    {
        idMap = new Dictionary<string, SceneSO>(SceneList.Count);
        nameMap = new Dictionary<string, SceneSO>(SceneList.Count);

        foreach (var s in SceneList)
        {
            if (s == null) continue;

            // IDキャッシュ
            if (!string.IsNullOrEmpty(s.SceneId))
            {
                idMap[s.SceneId] = s;
            }

            // Nameキャッシュ
            if (!string.IsNullOrEmpty(s.SceneName))
            {
                nameMap[s.SceneName] = s;
            }
        }
    }

    /// <summary>
    /// 実行時にロードされたタイミングでキャッシュを作っておく
    /// </summary>
    private void OnEnable()
    {
        BuildCache();
    }

}
