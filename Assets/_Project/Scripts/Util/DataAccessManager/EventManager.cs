using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// イベント（既に達成したEventId群）を管理するクラス。
/// - 内部は HashSet<string> で保持（重複排除＆高速判定）
/// - セーブ/ロードは PlayerData.ClearEvent（string[]）でやり取り
/// - EventDataSO があれば、未登録IDを弾く/定義を引くことができる
/// </summary>
public class EventManager
{
    // 全イベント一覧
    private readonly EventDataSO eventMaster;

    // クリア済みEventId（重複なし）
    private readonly HashSet<string> clearedIds = new HashSet<string>();

    /// <summary>変更フラグ：未保存の変更があるか</summary>
    public bool IsDirty { get; private set; }

    /// <summary>
    /// 変更通知（eventId, isCleared）
    /// - 今回は「クリアになった」しか起きない設計なので bool は常に true になる想定
    /// </summary>
    public event Action<string, bool> OnChanged;

    public EventManager(EventDataSO master)
    {
        eventMaster = master;
    }

    /// <summary>
    /// クリア状況確認メソッド
    /// </summary>
    public bool IsCleared(string eventId)
    {
        if (string.IsNullOrEmpty(eventId)) return false;
        return clearedIds.Contains(eventId);
    }

    /// <summary>
    /// クリア済みイベント登録メソッド
    /// イベントを「クリア済み」として登録する。
    /// - 既にクリア済みなら false（変更なし）
    /// - 未登録IDは弾く（masterがある場合）
    /// </summary>
    public bool TryMarkCleared(string eventId)
    {
        if (string.IsNullOrEmpty(eventId)) return false;
        if (!IsValidEventId(eventId)) return false;

        // HashSet.Add は「追加できたら true / 既にあれば false」
        if (!clearedIds.Add(eventId)) return false;

        IsDirty = true;
        OnChanged?.Invoke(eventId, true);
        return true;
    }

    /// <summary>
    /// クリア状況ロードメソッド
    /// セーブからロードした ClearEvent（string[]）をランタイム状態へ反映する。
    /// - null/空/重複/未登録IDを吸収して正規化する
    /// - ロード直後は「未変更」とみなすので IsDirty は false
    /// </summary>
    public void LoadFromSaveClearEvents(string[] clearEventIds)
    {
        clearedIds.Clear();

        if (clearEventIds != null)
        {
            for (int i = 0; i < clearEventIds.Length; i++)
            {
                var id = clearEventIds[i];
                if (string.IsNullOrEmpty(id)) continue;
                if (!IsValidEventId(id)) continue;

                // 重複はHashSetが吸収
                clearedIds.Add(id);
            }
        }

        IsDirty = false;
        // UIを全更新したいなら、ここで clearedIds を走査して通知する設計もあり
    }

    /// <summary>
    /// クリア状況セーブメソッド
    /// 現在のクリア状態をセーブ用（string[]）へ変換する。
    /// - 配列はスナップショット。外側で保持せず都度作る想定。
    /// </summary>
    public string[] ToSaveClearEvents(bool sort = true)
    {
        if (clearedIds.Count == 0) return Array.Empty<string>();

        var arr = new string[clearedIds.Count];
        clearedIds.CopyTo(arr);

        // セーブの安定性（差分比較やデバッグ）を考えるとソート推奨
        if (sort) Array.Sort(arr, StringComparer.Ordinal);

        return arr;
    }

    /// <summary>
    /// マスターに存在するEventIdか検証する。
    /// - masterが未設定なら検証しない運用も可
    /// </summary>
    private bool IsValidEventId(string eventId)
    {
        if (eventMaster == null) return true;

        if (eventMaster.TryGetByEventId(eventId, out _)) return true;

        Debug.Log($"存在しない EventId: {eventId}");
        return false;
    }

    /// <summary>
    /// 任意：定義が欲しい場合（イベント名表示など）
    /// </summary>
    public bool TryGetDefinition(string eventId, out EventSO ev)
    {
        ev = null;
        if (eventMaster == null) return false;
        return eventMaster.TryGetByEventId(eventId, out ev);
    }
}


// ------呼び出し使用例------

// 起動時（Manager等）
//var eventManager = new EventManager(eventDataSO);

//// ロード後
//eventManager.LoadFromSaveClearEvents(save.PlayerData.ClearEvent);

//// クリア時
//eventManager.TryMarkCleared("Event001");

//// セーブ時
//save.PlayerData.ClearEvent = eventManager.ToSaveClearEvents();