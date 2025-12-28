using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 全プレイヤー状態（PlayerStateSO）を管理するマスターSO。
/// - PlayerStateId（string）をキーに PlayerStateSO を取得できるようにキャッシュ（Dictionary）を構築する。
/// - 取得側は PlayerStateId を渡すだけで、対応する PlayerStateSO 一式を受け取れる。
/// </summary>
[CreateAssetMenu(menuName = "Master/PlayerState DataSO")]
public class PlayerStateDataSO : ScriptableObject
{
    /// <summary>
    /// 登録されているプレイヤー状態定義一覧。
    /// </summary>
    public List<PlayerStateSO> PlayerStateList = new();

    /// <summary>
    /// PlayerStateId → PlayerStateSO の高速参照用キャッシュ。
    /// シリアライズ対象ではないため、実行時に構築する。
    /// </summary>
    Dictionary<string, PlayerStateSO> map;

    /// <summary>
    /// 指定した PlayerStateId に対応する PlayerStateSO を取得する。
    /// - 見つかったら true / 見つからなければ false。
    /// - キャッシュ未構築の場合は内部で構築する。
    /// </summary>
    /// <param name="playerStateId">取得したいプレイヤー状態ID</param>
    /// <param name="state">取得結果（成功時に PlayerStateSO が入る）</param>
    public bool TryGetByPlayerStateId(string playerStateId, out PlayerStateSO state)
    {
        // IDが空の場合は探索しない（呼び出し側のバグを早期に気づける）
        if (string.IsNullOrEmpty(playerStateId))
        {
            state = null;
            return false;
        }

        // キャッシュがまだ作られていなければ構築する
        if (map == null) BuildCache();

        return map.TryGetValue(playerStateId, out state);
    }

    /// <summary>
    /// PlayerStateList の内容からキャッシュ（Dictionary）を構築する。
    /// - null要素やID未設定はスキップする。
    /// </summary>
    private void BuildCache()
    {
        map = new Dictionary<string, PlayerStateSO>(PlayerStateList.Count);

        foreach (var s in PlayerStateList)
        {
            if (s == null || string.IsNullOrEmpty(s.PlayerStateId)) continue;

            // 重複を検知したいなら下記のようにチェックしてログを出す
            // if (map.ContainsKey(s.PlayerStateId))
            //     Debug.LogError($"Duplicate PlayerStateId: {s.PlayerStateId}", this);

            map[s.PlayerStateId] = s;
        }
    }

    /// <summary>
    /// 実行時にロードされたタイミングでキャッシュを作っておく
    /// </summary>
    private void OnEnable()
    {
        BuildCache();
    }


    // ----以下、必要なら...

    /// <summary>
    /// 指定した PlayerStateId に対応する PlayerStateSO を取得する（必須取得版）。
    /// - 見つからない場合はエラーログを出し、null を返す。
    /// - 「基本は存在する前提」の呼び出し箇所で使う想定。
    /// </summary>
    // public PlayerStateSO GetByIdOrNull(string playerStateId)
    // {
    //     if (TryGetByPlayerStateId(playerStateId, out var state)) return state;

    //     Debug.LogError($"Unknown PlayerStateId: {playerStateId}", this);
    //     return null;
    // }
}
