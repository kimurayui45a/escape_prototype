using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 全アイテム（GameItemSO）を管理するマスターSO。
/// - GameItemId（string）をキーに GameItemSO を取得できるようにキャッシュ（Dictionary）を構築する。
/// - 取得側は GameItemId を渡すだけで、対応する GameItemSO 一式を受け取れる。
/// </summary>
[CreateAssetMenu(menuName = "Master/GameItem DataSO")]
public class GameItemDataSO : ScriptableObject
{
    /// <summary>
    /// 登録されているアイテム定義一覧。
    /// </summary>
    public List<GameItemSO> GameItemList = new();

    /// <summary>
    /// GameItemId → GameItemSO の高速参照用キャッシュ。
    /// シリアライズ対象ではないため、実行時に構築する。
    /// </summary>
    Dictionary<string, GameItemSO> map;

    /// <summary>
    /// 指定した GameItemId に対応する GameItemSO を取得する。
    /// - 見つかったら true / 見つからなければ false。
    /// - キャッシュ未構築の場合は内部で構築する。
    /// </summary>
    /// <param name="gameItemId">取得したいアイテムID</param>
    /// <param name="item">取得結果（成功時に GameItemSO が入る）</param>
    public bool TryGetByGameItemId(string gameItemId, out GameItemSO item)
    {
        // IDが空の場合は探索しない（呼び出し側のバグを早期に気づける）
        if (string.IsNullOrEmpty(gameItemId))
        {
            item = null;
            return false;
        }

        // キャッシュがまだ作られていなければ構築する
        if (map == null) BuildCache();

        return map.TryGetValue(gameItemId, out item);
    }

    /// <summary>
    /// GameItemList の内容からキャッシュ（Dictionary）を構築する。
    /// - null要素やID未設定はスキップする。
    /// </summary>
    private void BuildCache()
    {
        map = new Dictionary<string, GameItemSO>(GameItemList.Count);

        foreach (var s in GameItemList)
        {
            if (s == null || string.IsNullOrEmpty(s.GameItemId)) continue;

            map[s.GameItemId] = s;
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
