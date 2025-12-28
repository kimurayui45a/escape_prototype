using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 所持アイテムの増減・参照・セーブ変換を担うランタイム管理クラス。
/// - 内部は Dictionary<string,int>（itemId -> count）で管理（高速）
/// - セーブ/ロードでは OwnedItemEntry[] へ変換してやり取りする（外部ファイル向け）
/// - GameItemDataSO を渡すと、存在しないIDを弾く/定義を引くことができる
/// </summary>
public class GameItemManager
{
    /// <summary>マスター（ID -> GameItemSO）</summary>
    private readonly GameItemDataSO gameItemMaster;

    /// <summary>所持数（ランタイム用）</summary>
    private readonly Dictionary<string, int> ownedItemCounts = new Dictionary<string, int>(64);

    /// <summary>変更フラグ：セーブすべき変更が入ったか（任意）</summary>
    public bool IsDirty { get; private set; }

    /// <summary>変更通知（UI更新等に使う場合）</summary>
    public event Action<string, int> OnChanged;

    public GameItemManager(GameItemDataSO master)
    {
        gameItemMaster = master;
    }

    /// <summary>
    /// 所持数取得メソッド（未所持なら0）
    /// </summary>
    public int GetGameItemCount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return 0;
        return ownedItemCounts.TryGetValue(itemId, out var c) ? c : 0;
    }

    /// <summary>
    /// 所持確認メソッド（count > 0）
    /// </summary>
    public bool HasGameItem(string itemId, int requiredCount = 1)
    {
        if (requiredCount <= 0) return true;
        return GetGameItemCount(itemId) >= requiredCount;
    }

    /// <summary>
    /// アイテム加算メソッド（amountは正数のみ）。成功したらtrue。
    /// - マスターが設定されている場合、未登録IDは弾く
    /// </summary>
    public bool TryAddGameItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        if (amount <= 0) return false;

        if (!IsValidItemId(itemId)) return false;

        var newCount = GetGameItemCount(itemId) + amount;
        SetInternal(itemId, newCount);
        return true;
    }

    /// <summary>
    /// アイテム減算メソッド（amountは正数のみ）。不足ならfalse（減らさない）。
    /// </summary>
    public bool TryConsumeGameItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        if (amount <= 0) return false;

        var current = GetGameItemCount(itemId);
        if (current < amount) return false;

        var newCount = current - amount;
        SetInternal(itemId, newCount);
        return true;
    }

    /// <summary>
    /// 所持数を強制セット。
    /// - 0 は「所持していないが既知（図鑑用に残す）」
    /// - デバッグやロード、報酬テーブル適用等で使う想定
    /// </summary>
    public bool SetCountGameItem(string itemId, int count)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        if (count < 0) return false;

        if (!IsValidItemId(itemId)) return false;

        SetInternal(itemId, count);
        return true;
    }

    /// <summary>
    /// アイテムデータ取得メソッド
    /// マスターからアイテム定義を取得（表示名などを取りたい場合）。
    /// </summary>
    public bool TryGetDefinitionGameItem(string itemId, out GameItemSO item)
    {
        item = null;
        if (gameItemMaster == null) return false;
        return gameItemMaster.TryGetByGameItemId(itemId, out item);
    }

    /// <summary>
    /// 所持数ロードメソッド
    /// セーブデータ（OwnedItemEntry配列）をランタイム辞書へ適用する。
    /// - ロードした内容を「正規化」しながら取り込む（null要素、空ID、0以下、未登録ID、重複IDなどを吸収）
    /// - 不正ID、0以下、重複IDなどを吸収して正規化する
    /// - 適用後は「今の状態はセーブ済みと同等」とみなして Dirty を false に戻す運用が一般的（ロード直後は未変更扱い）
    /// </summary>
    /// <param name="entries">
    /// セーブから読み出した所持アイテム配列。
    /// null の場合は「所持アイテム無し」として扱う。
    /// </param>
    public void LoadFromSaveEntries(OwnedItemEntry[] entries)
    {
        // まずランタイム辞書を空にして、セーブ内容で完全に上書きする。
        // （差分適用ではなくスナップショット適用）
        ownedItemCounts.Clear();

        if (entries != null)
        {
            // 配列を先頭から走査し、1件ずつ辞書へ取り込む。
            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];

                // 配列内に null 要素が混ざっていても落ちないようにスキップする。
                if (e == null) continue;

                // セーブデータ上の 所持アイテムの ID と個数を取り出す。
                var id = e.OwnedItemId;
                var count = e.OwnedItemCount;

                // ID が null / 空なら不正なので無視する。
                if (string.IsNullOrEmpty(id)) continue;

                // 0 は許容（図鑑用途で「保持する」ため）
                // （負数だけ弾く）
                if (count < 0) continue;

                // マスター未登録ID（タイポや古いセーブなど）なら無視する。
                // ここでログを出すかどうかは設計方針次第（移行期は警告止まり等もある）。
                if (!IsValidItemId(id)) continue;

                // 重複IDが来た場合は「合算」して取り込む。
                // 例： entries に ("item_potion", 2) と ("item_potion", 3) があれば、辞書上は 5 にする。
                // 合算にしておくと、データが重複していても破綻しにくい（事故耐性が高い）。
                // 既に辞書にある場合は合算（0同士もOK）
                ownedItemCounts[id] = GetGameItemCount(id) + count;
            }
        }
        // ロード直後は「ユーザーがまだ何も変更していない」状態なので、
        // この時点の状態は「セーブ済みの状態と同じ」とみなす。
        // そのため Dirty を false に戻す（＝未保存の変更なし）。
        IsDirty = false;

        // UIを全更新したいなら、ここで全件通知する設計もあり
    }

    /// <summary>
    /// 所持状態セーブメソッド
    /// 現在の所持状態をセーブ用の配列に変換する。
    /// - セーブ時に 0 も出す（0を永続化して「入手履歴」を残すため）
    /// - 出力は「スナップショット」なので外側で保持せず、都度作る想定
    /// </summary>
    public OwnedItemEntry[] ToSaveOwnedItem()
    {
        if (ownedItemCounts.Count == 0) return Array.Empty<OwnedItemEntry>();

        var list = new List<OwnedItemEntry>(ownedItemCounts.Count);
        foreach (var kv in ownedItemCounts)
        {
            if (kv.Value < 0) continue;

            list.Add(new OwnedItemEntry
            {
                OwnedItemId = kv.Key,
                OwnedItemCount = kv.Value
            });
        }

        return list.ToArray();
    }

    /// <summary>
    /// 「変更が入った」扱いを手動でクリアしたい場合（任意）。
    /// </summary>
    // public void ClearDirty()
    // {
    //     IsDirty = false;
    // }


    // -----------------------
    // 内部処理
    // -----------------------

    /// <summary>
    /// 所持アイテム更新メソッド（対象：OwnedItem）
    /// 0になっても削除しない（図鑑参照用にIDを保持する）
    /// </summary>
    private void SetInternal(string itemId, int newCount)
    {
        // 負数はここで 0 に丸める（仕様として「未所持」は0で表現）
        if (newCount < 0) newCount = 0;

        // 0でも辞書に保持（Removeしない）
        ownedItemCounts[itemId] = newCount;

        IsDirty = true;
        OnChanged?.Invoke(itemId, newCount);
    }

    /// <summary>
    /// 存在確認メソッド
    /// 存在しないアイテムのIDが来た場合の処理
    /// </summary>
    private bool IsValidItemId(string itemId)
    {
        // マスター未設定なら「検証しない」運用も可能（プロトタイプ等）
        if (gameItemMaster == null) return true;

        // 存在しないIDは弾く（タイポや未登録を早期に発見できる）
        if (gameItemMaster.TryGetByGameItemId(itemId, out _)) return true;

        Debug.Log($"存在しない GameItemId: {itemId}");
        return false;
    }
}




// ------呼び出し使用例------

//// 1) 起動時にItemManagerを作る（Manager/Singletonの中など）
//gameItemManager = new GameItemManager(gameItemDataSO);

//// 2) ロードしたセーブデータの OwnedItemList を適用
//gameItemManager.LoadFromSaveEntries(save.PlayerData.OwnedItemList?.ToArray());

//// 3) プレイ中の増減
//gameItemManager.TryAdd("item_potion_small", 1);
//gameItemManager.TryConsume("item_potion_small", 1);

//// 4) セーブ時に配列へ変換して保存データへ詰める
//save.PlayerData.OwnedItemList = new List<OwnedItemEntry>(gameItemManager.ToSaveOwnedItem());
