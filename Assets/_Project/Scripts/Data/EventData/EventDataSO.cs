using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 全イベント（EventSO）を管理するマスターSO。
/// - EventId（string）をキーに EventSO を取得できるようにキャッシュ（Dictionary）を構築する。
/// - 取得側は EventId を渡すだけで、対応する EventSO 一式を受け取れる。
/// </summary>
[CreateAssetMenu(menuName = "Master/Event DataSO")]
public class EventDataSO : ScriptableObject
{
    /// <summary>
    /// 登録されているイベント定義一覧。
    /// </summary>
    public List<EventSO> EventList = new();

    /// <summary>
    /// EventId → EventSO の高速参照用キャッシュ。
    /// シリアライズ対象ではないため、実行時に構築する。
    /// </summary>
    Dictionary<string, EventSO> map;

    /// <summary>
    /// 指定した EventId に対応する EventSO を取得する。
    /// - 見つかったら true / 見つからなければ false。
    /// - キャッシュ未構築の場合は内部で構築する。
    /// </summary>
    /// <param name="sceneId">取得したいイベントID</param>
    /// <param name="eventdata">取得結果（成功時に EventSO が入る）</param>
    public bool TryGetByEventId(string sceneId, out EventSO eventdata)
    {
        // IDが空の場合は探索しない（呼び出し側のバグを早期に気づける）
        if (string.IsNullOrEmpty(sceneId))
        {
            eventdata = null;
            return false;
        }

        // キャッシュがまだ作られていなければ構築する
        if (map == null) BuildCache();

        return map.TryGetValue(sceneId, out eventdata);
    }

    /// <summary>
    /// EventList の内容からキャッシュ（Dictionary）を構築する。
    /// - null要素やID未設定はスキップする。
    /// </summary>
    private void BuildCache()
    {
        map = new Dictionary<string, EventSO>(EventList.Count);

        foreach (var s in EventList)
        {
            if (s == null || string.IsNullOrEmpty(s.EventId)) continue;

            map[s.EventId] = s;
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
