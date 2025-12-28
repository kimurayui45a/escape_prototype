


//　現在作成中






//using System;
//using System.Collections.Generic;
//using UnityEngine;

///// <summary>
///// PlayGameData の保存（未セーブ領域・手動スロット）を担当するランタイム管理クラス。
///// - ファイル書き込みはしない（SaveData.SaveParam に反映するだけ）
///// - 各サブマネージャーから「セーブ用スナップショット」を生成して格納する
///// </summary>
//public class PlayGameDataManager
//{
//    private readonly SaveData saveData;

//    // 参照する各マネージャー
//    private readonly EventManager eventManager;
//    private readonly GameItemManager gameItemManager;
//    private readonly PlayerStateManager playerStateManager;
//    private readonly SceneDataManager sceneDataManager;

//    public PlayGameDataManager(
//        SaveData saveData,
//        EventManager eventManager,
//        GameItemManager gameItemManager,
//        PlayerStateManager playerStateManager,
//        SceneDataManager sceneDataManager)
//    {
//        this.saveData = saveData ?? throw new ArgumentNullException(nameof(saveData));

//        // managerはプロジェクト都合で null 許容でもOK（その場合は該当項目を空で保存）
//        this.eventManager = eventManager;
//        this.gameItemManager = gameItemManager;
//        this.playerStateManager = playerStateManager;
//        this.sceneDataManager = sceneDataManager;
//    }

//    /// <summary>
//    /// 未セーブ領域（UnSavedPlayGameData）からロードして、各Managerへ適用する。
//    /// - 未セーブが無い（フラグfalse / null）なら false
//    /// </summary>
//    public bool LoadFromUnSaved()
//    {
//        if (!saveData.SaveParam.UnSavedData) return false;

//        var src = saveData.SaveParam.UnSavedPlayGameData;
//        if (src == null) return false;

//        ApplyPlayGameDataToRuntime(src);
//        return true;
//    }

//    /// <summary>
//    /// 選択スロット（Manager.Instance.GameSession.SelectedSlot）からロードして各Managerへ適用する。
//    /// - スロットが無い/空なら false
//    /// </summary>
//    public bool LoadFromSelectedSlot()
//    {
//        int slot = GetSelectedSlotOrDefault();

//        var src = FindSlotByNumber(saveData.SaveParam.PlayGameDataList, slot);
//        if (src == null) return false;

//        ApplyPlayGameDataToRuntime(src);
//        return true;
//    }


//    /// <summary>
//    /// 未セーブデータの保存
//    /// 未セーブデータ（退避領域）を保存し、未セーブフラグを true にする。
//    /// - 想定：オートセーブ/中断復帰用の「一時スナップショット」
//    /// </summary>
//    public void SaveToUnSaved()
//    {
//        var slot = GetSelectedSlotOrDefault();

//        // 現在状態をスナップショット化
//        var snapshot = CreateSnapshot(playFileNumber: slot);

//        // SaveDataへ反映（未セーブ領域）
//        saveData.SaveParam.UnSavedPlayGameData = snapshot;
//        saveData.SaveParam.UnSavedData = true;
//    }

//    /// <summary>
//    /// 手動セーブスロットへ保存する。
//    /// - 保存先は Manager.Instance.GameSession.SelectedSlot（1..3想定）
//    /// - スロットが存在しなければ作成して追加する
//    /// </summary>
//    public void SaveToSelectedSlot()
//    {
//        // ファイル番号の取得（範囲外なら 1 を返す）
//        int slot = GetSelectedSlotOrDefault();

//        // 現在状態をスナップショット化（スロット番号も入れる）
//        var snapshot = CreateSnapshot(playFileNumber: slot);

//        // 対象スロットへ格納（PlayFileNumberで検索するので順番が崩れても安全）
//        var list = saveData.SaveParam.PlayGameDataList;
//        var target = FindSlotByNumber(list, slot);
//        if (target == null)
//        {
//            // スロットが無い場合は新規追加
//            list.Add(snapshot);
//        }
//        else
//        {
//            // 既存スロットを上書き（参照を差し替える）
//            int idx = list.IndexOf(target);
//            list[idx] = snapshot;
//        }

//        // 手動セーブしたなら、未セーブ領域は「不要」扱いに落とす運用が一般的
//        // 要件に明記はないですが、事故が減るので推奨。
//        saveData.SaveParam.UnSavedPlayGameData = null;
//        saveData.SaveParam.UnSavedData = false;
//    }

//    /// <summary>
//    /// 未セーブデータ（退避領域）を破棄する。
//    /// - UnSavedPlayGameData に null を入れる
//    /// - UnSavedData を false にする
//    /// </summary>
//    public void DiscardUnSaved()
//    {
//        saveData.SaveParam.UnSavedPlayGameData = null;
//        saveData.SaveParam.UnSavedData = false;
//    }

//    /// <summary>
//    /// PlayGameData の内容を、各ランタイムManagerへ適用する（I/Oなし）。
//    /// </summary>
//    private void ApplyPlayGameDataToRuntime(PlayGameData src)
//    {
//        if (src == null) return;

//        // ---- 日時系 ----
//        // ロード用メソッドが無いなら「直代入」でOK（必要なら保持先を作る）
//        // 例：最後にロードしたスナップショットの時刻を持ちたいなら、ここでフィールド保持する
//        // lastLoadedPlaySaveTime = src.LastPlaySaveTime;

//        // ---- シーン ----
//        if (sceneDataManager != null)
//        {
//            sceneDataManager.LoadFromSaveSceneId(src.LastSceneId);
//        }

//        // ---- プレイヤーパラメータ ----
//        var p = src.PlayerParameter ?? new PlayerParameter();

//        // イベント
//        if (eventManager != null)
//        {
//            eventManager.LoadFromSaveClearEvents(p.ClearEvent);
//        }

//        // アイテム（※ここは ToSaveOwnedItem ではなく Load）
//        if (gameItemManager != null)
//        {
//            gameItemManager.LoadFromSaveEntries(p.OwnedItemList?.ToArray());
//        }

//        // 快/不快/状態
//        if (playerStateManager != null)
//        {
//            playerStateManager.LoadFromPlayerParameter(p);
//        }

//        // 必要なら、ロード後に各マネージャの Dirty を落とす運用も可
//        // ただし「ファイルロード直後は未変更扱い」が前提の場合のみ。
//        // gameItemManager.ClearDirty();
//        // playerStateManager.ClearDirty();
//        // sceneDataManager.ClearDirty();
//    }

//    // -----------------------
//    // 内部：スナップショット生成
//    // -----------------------

//    /// <summary>
//    /// 現在のゲーム状態を PlayGameData にまとめて生成する。
//    /// </summary>
//    private PlayGameData CreateSnapshot(int playFileNumber)
//    {
//        var now = TimeUtil.NowUtcUnixSeconds();

//        var data = new PlayGameData
//        {
//            LastPlaySaveTime = now,
//            PlayFileNumber = playFileNumber,

//            // Sceneは「セーブはID」方針なので、SceneIdを保存する
//            LastSceneId = sceneDataManager != null
//                ? sceneDataManager.ToSaveSceneId()
//                : null,

//            PlayerParameter = new PlayerParameter()
//        };

//        // ---- イベント ----
//        // eventManager.ToSaveClearEvents() が string[] を返す想定
//        data.PlayerParameter.ClearEvent = eventManager != null
//            ? (eventManager.ToSaveClearEvents() ?? Array.Empty<string>())
//            : Array.Empty<string>();

//        // ---- アイテム ----
//        // gameItemManager.ToSaveOwnedItem() が OwnedItemEntry[] を返す想定
//        if (gameItemManager != null)
//        {
//            var owned = gameItemManager.ToSaveOwnedItem() ?? Array.Empty<OwnedItemEntry>();
//            data.PlayerParameter.OwnedItemList = new List<OwnedItemEntry>(owned);
//        }
//        else
//        {
//            data.PlayerParameter.OwnedItemList = new List<OwnedItemEntry>();
//        }

//        // ---- 快/不快/状態 ----
//        // playerStateManager.WriteToPlayerParameter(PlayerParameter) を呼ぶ想定
//        if (playerStateManager != null)
//        {
//            playerStateManager.WriteToPlayerParameter(data.PlayerParameter);
//        }

//        return data;
//    }

//    /// <summary>
//    /// 選択スロット（1..3想定）を取得する。
//    /// - 取得できない/範囲外なら 1 を返す（事故回避）
//    /// </summary>
//    private int GetSelectedSlotOrDefault()
//    {
//        // 要件：保存先確認に Manager.Instance.GameSession.SelectedSlot を使う
//        if (Manager.Instance == null || Manager.Instance.GameSession == null)
//        {
//            Debug.LogError("[PlayGameDataManager] Manager.Instance / GameSession is null. fallback to slot=1");
//            return 1;
//        }

//        int slot = Manager.Instance.GameSession.SelectedSlot;

//        // 1..3以外は事故なので丸める（またはエラーにしてreturn falseでもOK）
//        if (slot < 1 || slot > 3)
//        {
//            Debug.LogError($"[PlayGameDataManager] SelectedSlot out of range: {slot}. fallback to slot=1");
//            slot = 1;
//        }

//        return slot;
//    }

//    /// <summary>
//    /// PlayFileNumber が一致するスロットを探す。
//    /// </summary>
//    private PlayGameData FindSlotByNumber(List<PlayGameData> list, int playFileNumber)
//    {
//        if (list == null) return null;

//        for (int i = 0; i < list.Count; i++)
//        {
//            var d = list[i];
//            if (d == null) continue;
//            if (d.PlayFileNumber == playFileNumber) return d;
//        }

//        return null;
//    }
//}
