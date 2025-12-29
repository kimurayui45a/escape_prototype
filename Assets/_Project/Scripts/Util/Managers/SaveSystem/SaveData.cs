using System;
using System.IO;
using UnityEngine;
using static SaveData;

/// <summary>
/// セーブデータ本体（ゲーム進行データ）。
/// 
/// 役割：
/// - 「何を保存するか」を定義する（Param）
/// -Param を JSON 化して payload（byte[]）として出力 / 復元する
/// 
/// 注意：
/// - ファイル先頭のヘッダ（Magic/FormatVersion/Flags/PayloadLength 等）は SaveFileFormat/SaveManager が担当する。
/// - このクラスは「payload の中身」に集中させる設計。
/// </summary>
public class SaveData
{
    /// <summary>
    /// 実際に保存したいパラメータ群。
    /// JsonUtility で扱えるように [Serializable] を付ける。
    /// 
    /// ここにフィールドを追加すると、保存される内容が増える。
    /// （追加/削除が互換性に影響するため、将来はマイグレーションも考慮する）
    /// </summary>
    [System.Serializable]
    public class Param
    {
        /// <summary>例：経過日数</summary>
        public int Day = 0;

        /// <summary>プレイヤー情報（別クラスのデータ表現）</summary>
        public PlayerData Player = new PlayerData();
    }

    /// <summary>
    /// 保存対象のパラメータ本体。
    /// private set にして外部からの差し替えを禁止（破壊的代入を抑止）。
    /// 
    /// ※ 値の更新（Day 等の変更）は SaveParam.Day = ... のように可能。
    /// </summary>
    public Param SaveParam { get; private set; } = new Param();

    /// <summary>
    /// SaveParam を payload（byte[]）へ変換する。
    /// 
    /// - SaveManager はこの payload を「ファイル本体」として書き込む。
    /// - ここでは JsonUtility を使い、JSON文字列を UTF-8 bytes に変換して返す。
    /// 
    /// メモ：
    /// - JsonUtility は型情報や Dictionary 等が弱いなど制約がある。
    /// - 将来、暗号化/圧縮を入れるなら SaveManager 側で payload に対して行うのが責務分離として自然。
    /// </summary>
    public byte[] ToPayloadBytes()
    {
        // SaveParam を JSON 文字列にする（prettyPrint=false：改行無しでサイズを抑える）
        var json = JsonUtility.ToJson(SaveParam, false);

        // JSON文字列を UTF-8 バイト列に変換して返す
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// payload（byte[]）から SaveParam を復元する。
    /// 
    /// - SaveManager がファイルから読み出した payload を渡してくる想定。
    /// - JSON文字列（UTF-8）として復元し、JsonUtility.FromJson で Param に戻す。
    /// </summary>
    /// <param name="payloadBytes">ファイルから取り出した payload のバイト列</param>
    public void FromPayloadBytes(byte[] payloadBytes)
    {
        // UTF-8 bytes → JSON文字列
        var json = System.Text.Encoding.UTF8.GetString(payloadBytes);

        // JSON文字列 → Param
        SaveParam = JsonUtility.FromJson<Param>(json);

        // 旧データ・破損対策（null ガード）
        // - 例えば旧バージョンに存在しないフィールドがある/ない等で想定外が起きた場合に備える
        if (SaveParam == null) SaveParam = new Param();
        if (SaveParam.Player == null) SaveParam.Player = new PlayerData();

        // PlayerData の整合性チェック/補正（あなたの PlayerData 実装に依存）
        // 例：範囲外値を丸める、必須値が未設定なら初期化する、など
        SaveParam.Player.Validate();
    }

    /// <summary>
    /// 実行時（Runtime）の状態から、保存用データ（SaveParam）へ取り込む。
    /// 
    /// - 「ゲーム内の生きた状態（PlayerStateなど）」を「保存用の純データ（PlayerData）」へ変換する橋渡し。
    /// - 呼び出し側（Saveボタン等）のコードを汚しにくくするため、SaveData 側にまとめる設計。
    /// </summary>
    /// <param name="day">現在の日数など（Runtime側の値）</param>
    /// <param name="playerState">Runtime側のプレイヤー状態</param>
    public void CaptureFromRuntime(int day, PlayerState playerState)
    {
        // 基本データをコピー
        SaveParam.Day = day;

        // Runtime表現 → 保存用表現へ変換
        // playerState が null の場合にも壊れないようにデフォルトを入れる
        SaveParam.Player = (playerState != null) ? playerState.ToData() : new PlayerData();
    }

    /// <summary>
    /// 保存データ（SaveParam）から、実行時（Runtime）の状態へ適用する。
    /// 
    /// - ロード直後に、Runtime側のオブジェクトへ復元する用途。
    /// - day を out にしているのは、呼び出し側でローカル変数へ取り込みやすくするため。
    /// </summary>
    /// <param name="playerState">Runtime側のプレイヤー状態（適用先）</param>
    /// <param name="day">復元した日数などを返す</param>
    public void ApplyToRuntime(PlayerState playerState, out int day)
    {
        // 保存値を返す
        day = SaveParam.Day;

        // 保存用表現 → Runtime表現へ反映
        if (playerState != null) playerState.Apply(SaveParam.Player);
    }
}




// 将来的に下記で実装する

//using System;
//using UnityEngine;

//public class SaveData
//{
//    // ---- 外部参照（システム） ----

//    // 未セーブフラグ
//    public bool UnSavedFlag { get; private set; } = false;

//    // 設定データ
//    public SettingData SettingData { get; private set; } = new SettingData();

//    // 簡易版スロット情報
//    public SlotSummary[] SlotSummary { get; private set; } = new SlotSummary[SaveDataConstants.ManualSlotCount];


//    // ---- 外部参照（ゲーム） ----
//    // プレイゲームデータ（スロットにセーブしているもの）
//    public PlayGameData[] ManualSlots { get; } = new PlayGameData[SaveDataConstants.ManualSlotCount]
//    {
//        new PlayGameData{ PlaySlotNumber = 1 },
//        new PlayGameData{ PlaySlotNumber = 2 },
//        new PlayGameData{ PlaySlotNumber = 3 },
//    };

//    // 作業中のデータ（セーブされていないもの）
//    public PlayGameData WorkingPlayGameData { get; private set; } = new PlayGameData();


//    // ---- 分野別の操作API（各フィールドの更新） ----
//    public SettingsController SettingsController { get; }
//    public PlayerController PlayerController { get; }


//    // 簡易版スロット情報配列に空のインスタンスをセット
//    public SaveData()
//    {
//        for (int i = 0; i < SlotSummary.Length; i++)
//        {
//            SlotSummary[i] ??= new SlotSummary { SlotNumber = i + 1 };
//        }

//        SettingsController = new SettingsController(this);
//        PlayerController = new PlayerController(this);
//    }

//    // たぶんいらない（WorkingPlayGameData が必ず安全に使える状態（nullが無い状態）に整えるための保険メソッド」）
//    // internal void EnsureWorking()
//    // {
//    //     WorkingPlayGameData ??= new PlayGameData();
//    //     WorkingPlayGameData.PlayerParameter ??= new PlayerParameter();
//    //     WorkingPlayGameData.LastSceneId ??= "scene_natural";
//    //     WorkingPlayGameData.PlayerParameter.ClearEvent ??= Array.Empty<string>();
//    //     WorkingPlayGameData.PlayerParameter.OwnedItemList ??= new List<OwnedItemEntry>();
//    //     WorkingPlayGameData.PlayerParameter.PlayerStateId ??= "state_plant";
//    // }

//    // =========================================================
//    // Load
//    // =========================================================

//    /// <summary>
//    /// システムデータロードメソッド
//    /// 
//    /// システムデータ（設定・スロット概要・未セーブフラグ等）を読み込み、
//    /// 現在の SaveData 状態へ反映する。
//    /// 
//    /// 期待する挙動:
//    /// - system_data.dat が存在し、正しく読めた場合：その内容で復元（ApplyIndex）
//    /// - 存在しない/破損などで読めない場合：初期値（new IndexData）で初期化し、
//    ///   その初期状態をファイルとして作成しておく（SaveSystem）
//    /// </summary>
//    public void LoadSystem()
//    {
//        // system_data.dat の読み込みを試みる
//        // - 成功すると index には復元された IndexData が入る
//        // - 失敗すると index は new IndexData() 相当（TryLoadの仕様）だが、
//        //   ここでは else 側で明示的に初期化して初回起動扱いにする
//        if (SaveManager.Instance.TryLoad<IndexData>(SaveDataConstants.SystemFile, out var index))
//        {
//            // システムデータの復元
//            // - UnSavedFlag / SettingData / SlotSummary などを SaveData に反映する
//            ApplyIndex(index);
//        }
//        else
//        {
//            // ファイルが無い・壊れている等の場合は初期値で開始する
//            // （ApplyIndex 内でも null ガード等をしているが、意図を明確にするため明示）
//            ApplyIndex(new IndexData());

//            // システムデータセーブメソッドも呼び出し
//            // 初回起動扱いとして初期状態の system_data.dat を作成しておく
//            // - 次回起動からは TryLoad が成功しやすくなる
//            // - 「初期値を書き込む」ことで、以降の処理で存在前提にしやすくなる
//            SaveSystem();
//        }
//    }

//    /// <summary>
//    /// 未セーブデータロードメソッド
//    /// 
//    /// 未セーブデータ（unsaved.dat）を読み込み、WorkingPlayGameData に復元する。
//    /// 
//    /// 返り値:
//    /// - true  : 復元に成功し、WorkingPlayGameData を更新した
//    /// - false : 未セーブフラグが立っていない、または読み込みに失敗した
//    /// 
//    /// 前提/意図:
//    /// - UnSavedFlag は「未セーブデータが存在するはず」という状態フラグとして扱う
//    /// - 実ファイルが無い/破損している場合は復元できないため false
//    /// </summary>
//    public bool TryLoadUnsaved()
//    {
//        // 未セーブフラグが立っていないなら、未セーブデータ復元は不要
//        // （ファイルI/Oをしないで早期returnする）
//        if (!UnSavedFlag) return false;

//        // unsaved.dat の読み込みを試す
//        if (SaveManager.Instance.TryLoad<GameData>(SaveDataConstants.UnsavedFile, out var game))
//        {
//            // ファイルから復元できた場合、WorkingPlayGameData に反映
//            // game.PlayGameData が null の可能性（旧データ/破損/初期状態等）に備えて new で補完
//            WorkingPlayGameData = game.PlayGameData ?? new PlayGameData();

//            return true;
//        }

//        // ファイルが存在しない/破損/復元失敗など
//        // ※必要ならここで「UnSavedFlag を false に落とす」「unsaved.dat を削除する」などの
//        //   回復処理を入れても良い（設計方針次第）
//        return false;
//    }

//    /// <summary>
//    /// セーブデータロードメソッド
//    /// 
//    /// 指定スロット（1～3）のセーブデータを読み込み、作業中データ（WorkingPlayGameData）へ反映する。
//    /// 
//    /// 返り値:
//    /// - true  : 指定スロットの読み込みに成功し、WorkingPlayGameData を更新した
//    /// - false : ファイルが無い/破損などで読み込みに失敗した
//    /// 
//    /// 挙動のポイント:
//    /// - 成功時は「未セーブ状態」を解除する（UnSavedFlagをfalse）
//    ///   「今の作業データはスロット由来で、未セーブ扱いではない」という意味付け
//    /// - その状態変更を system_data.dat に永続化するため SaveSystem() を呼ぶ
//    /// </summary>
//    /// <param name="slotNumber1to3">1～3 のスロット番号</param>
//    public bool TryLoadSlotToWorking(int slotNumber1to3)
//    {
//        // 想定外の番号（0 や 4 など）を早期に弾く
//        ValidateSlotRange(slotNumber1to3);

//        // 対象スロットのファイル名を組み立てる（例: save_slot_01.dat）
//        var file = SaveDataConstants.SlotFile(slotNumber1to3);

//        // スロットファイルの読み込みを試す
//        if (SaveManager.Instance.TryLoad<GameData>(file, out var game))
//        {
//            // 読み込み成功時：作業中データへ反映
//            // game.PlayGameData が null の可能性（旧データ/破損/初期状態など）に備えて new で補完
//            WorkingPlayGameData = game.PlayGameData ?? new PlayGameData();

//            // この作業データはスロットから復元された＝未セーブではない、としてフラグを下げる
//            UnSavedFlag = false;

//            // システムデータセーブメソッドも呼び出し
//            // UnSavedFlag などの「システム側状態」を永続化（system_data.dat を更新）
//            SaveSystem();

//            return true;
//        }

//        // ファイルが無い/破損/復元失敗など
//        return false;
//    }


//    // =========================================================
//    // Save
//    // =========================================================

//    /// <summary>
//    /// システムデータセーブメソッド
//    /// system_data.dat として保存する。
//    /// </summary>
//    public void SaveSystem()
//    {
//        // 現在のシステムデータを取得する
//        var index = BuildIndex();

//        // 共通セーブメソッドでシステムデータをファイルに書き出す
//        SaveManager.Instance.Save(SaveDataConstants.SystemFile, index);
//    }

//    /// <summary>
//    /// 未セーブデータセーブメソッド
//    /// 
//    /// 「未セーブデータ（作業中データ）」を unsaved.dat として保存する。
//    /// 
//    /// 目的:
//    /// - 手動スロット保存とは別に、現在の作業状態を一時退避しておく（続きから再開できるようにする）
//    /// - 保存後は UnSavedFlag を true にし、system_data.dat にも反映して永続化する
//    /// 
//    /// 注意:
//    /// - ここで保存されるのは「スロット1～3の正式セーブ」ではなく、
//    ///   ユーザーが明示的にスロット保存する前の作業状態（ワーキングデータ）
//    /// </summary>
//    public void SaveUnsaved()
//    {
//        // WorkingPlayGameData が null の可能性に備えて補完し、
//        // 保存用コンテナ（GameData）に詰める
//        var game = new GameData
//        {
//            PlayGameData = WorkingPlayGameData ?? new PlayGameData()
//        };

//        // 現在日時の取得と記録
//        var now = TimeUtil.NowUtcUnixSeconds();
//        WorkingPlayGameData.LastPlaySaveTime = now;

//        // 未セーブ用ファイル（unsaved.dat）へ保存
//        SaveManager.Instance.Save(SaveDataConstants.UnsavedFile, game);

//        // 「未セーブデータが存在する」状態にする
//        // 次回起動時などに TryLoadUnsaved() を走らせる判定に使う
//        UnSavedFlag = true;

//        // システムデータセーブメソッドも呼び出し
//        // UnSavedFlag の変更を system_data.dat に反映して永続化
//        SaveSystem();
//    }


//    /// <summary>
//    /// セーブデータセーブメソッド
//    /// 
//    /// 作業中データ（WorkingPlayGameData）を指定スロット（1～3）へ保存する。
//    /// 
//    /// 目的:
//    /// - ユーザーが「手動セーブ」を行ったときに、WorkingPlayGameData をスロットファイルへ確定保存する。
//    /// - 保存後はスロット一覧用のキャッシュ（ManualSlots / SlotSummary）も更新する。
//    /// - 未セーブデータ（unsaved.dat）は不要になるため削除し、UnSavedFlag も解除する。
//    /// - 変更されたシステム状態（UnSavedFlag / SlotSummary 等）を system_data.dat に反映する（SaveSystem）。
//    /// </summary>
//    /// <param name="slotNumber1to3">1始まりのスロット番号（1..ManualSlotCount）</param>
//    public void SaveWorkingToSlot(int slotNumber1to3)
//    {
//        // 1..ManualSlotCount の範囲か検証（範囲外なら例外でバグを早期検出）
//        ValidateSlotRange(slotNumber1to3);

//        // 配列アクセス用（0始まり）に変換
//        var slotIndex = slotNumber1to3 - 1;

//        // 作業中データが null の可能性に備えて補完
//        WorkingPlayGameData ??= new PlayGameData();

//        // 作業中データが「どのスロットへ保存されたか」を明示的に埋める
//        // （ロードや表示でスロット整合性を取りたい場合に有用）
//        WorkingPlayGameData.PlaySlotNumber = slotNumber1to3;

//        // 現在日時の取得と記録
//        var now = TimeUtil.NowUtcUnixSeconds();
//        WorkingPlayGameData.LastPlaySaveTime = now;

//        // 保存用コンテナ（GameData）に詰めてスロットファイルへ保存
//        // 例: save_slot_01.dat
//        var game = new GameData { PlayGameData = WorkingPlayGameData };
//        SaveManager.Instance.Save(SaveDataConstants.SlotFile(slotNumber1to3), game);

//        // スロット一覧表示やキャッシュ用途のため、
//        // 保存した時点のスナップショットを ManualSlots に保持する（参照共有を避けるためクローン）
//        ManualSlots[slotIndex] = CloneForCache(WorkingPlayGameData);

//        // スロット一覧用の簡易情報（最終セーブ日時・シーン等）を更新
//        SlotSummary[slotIndex] = BuildSlotSummary(WorkingPlayGameData);

//        // 手動スロットへ確定保存したので「未セーブ状態」は解除する
//        UnSavedFlag = false;

//        // 未セーブファイル（unsaved.dat）は不要になるため削除する
//        // ※存在しない場合でも Delete() 側が no-op なら安全
//        SaveManager.Instance.Delete(SaveDataConstants.UnsavedFile);

//        // システムデータセーブメソッドも呼び出し
//        // UnSavedFlag / SlotSummary などのシステム側状態を system_data.dat に反映して永続化
//        SaveSystem();
//    }


//    /// <summary>
//    /// 未セーブデータ削除メソッド
//    /// 
//    /// 未セーブデータを破棄する。
//    /// - 未セーブフラグを下げる
//    /// - unsaved.dat を削除する
//    /// - system_data.dat を更新する
//    /// </summary>
//    public void DiscardUnsaved()
//    {
//        UnSavedFlag = false;
//        SaveManager.Instance.Delete(SaveDataConstants.UnsavedFile);
//        SaveSystem();
//    }

//    /// <summary>
//    /// 設定データ差し替えセーブメソッド（必要？？）
//    /// 
//    /// 設定データを差し替えて保存する。
//    /// null が渡された場合はデフォルト設定を使う。
//    /// </summary>
//    public void SetSettingData(SettingData settingData)
//    {
//        SettingData = settingData ?? new SettingData();
//        SaveSystem();
//    }

//    /// <summary>
//    /// 未セーブフラグ更新メソッド
//    /// 
//    /// 「未セーブ状態」を立てて system_data.dat に反映する。
//    /// （作業中データがスロット保存されていないことを示す）
//    /// </summary>
//    public void MarkUnsavedDirty()
//    {
//        UnSavedFlag = true;
//        SaveSystem();
//    }


//    // =========================================================
//    // 内部
//    // =========================================================

//    /// <summary>
//    /// システムデータ現在状況取得メソッド
//    /// 
//    /// 現在の SaveData の「システム側状態」を IndexData にまとめて返す。
//    /// 
//    /// IndexData に入れる想定の内容:
//    /// - UnSavedFlag      : 未セーブデータが存在するか（unsaved.dat を読むべきか）
//    /// - SlotSummary      : 各スロットの簡易情報（最終セーブ日時・シーン等）
//    /// - SettingData      : 設定値（音量、テキスト速度など）
//    /// 
//    /// 目的:
//    /// - SaveSystem() で system_data.dat に書き込むための“保存用DTO”を構築する。
//    /// </summary>
//    private IndexData BuildIndex()
//    {
//        // SlotSummary 配列の各要素が null のままだと、
//        // JSON化/表示/参照時に NullReference の原因になるため、ここで必ず補完する。
//        // ※ここでは「中身の初期値」を埋めるだけで、日時などの値更新は別処理で行う想定。
//        for (int i = 0; i < SlotSummary.Length; i++)
//        {
//            SlotSummary[i] ??= new SlotSummary();
//        }

//        // 現在の状態を IndexData に詰めて返す
//        return new IndexData
//        {
//            // 未セーブデータがあるかどうか（ロード時の分岐に使用）
//            UnSavedFlag = UnSavedFlag,

//            // スロット概要配列（参照を渡す形。必要ならコピーにする設計も可）
//            SlotSummary = SlotSummary,

//            // 設定データ（null の可能性に備えて補完）
//            SettingData = SettingData ?? new SettingData()
//        };
//    }

//    /// <summary>
//    /// システムセーブデータ復元メソッド
//    /// 
//    /// IndexData（システム側のセーブ情報）を SaveData の現在状態へ反映する。
//    /// - UnSavedFlag / SettingData / SlotSummary を復元する
//    /// - SlotSummary 配列の長さを ManualSlotCount に揃える（旧データ互換）
//    /// - 各要素は null にしない 、 SlotNumber を必ず 1..N で保証する
//    /// </summary>
//    private void ApplyIndex(IndexData index)
//    {
//        // 呼び出し側が null であった場合は、空の IndexData に差し替える
//        index ??= new IndexData();

//        // 未セーブフラグを復元
//        UnSavedFlag = index.UnSavedFlag;

//        // 設定データを復元
//        // index.SettingData が null の場合もある（旧データ/破損/初回起動など）ので new で補完
//        SettingData = index.SettingData ?? new SettingData();

//        // スロット概要情報（SlotSummary配列）を復元
//        // index.SlotSummary が null の場合もあるので ManualSlotCount 分の配列を用意
//        SlotSummary = index.SlotSummary ?? new SlotSummary[SaveDataConstants.ManualSlotCount];

//        // 旧バージョンのセーブデータ等で配列長が違う可能性があるため、
//        // 現行仕様（ManualSlotCount）に合わせてリサイズする
//        // - 短い場合: 末尾に null が追加される
//        // - 長い場合: 末尾が切り捨てられる
//        if (SlotSummary.Length != SaveDataConstants.ManualSlotCount)
//            Array.Resize(ref SlotSummary, SaveDataConstants.ManualSlotCount);

//        // 各要素を必ず非nullにし、SlotNumber（1..N）を必ず設定する
//        // - null のままだと UI 表示や BuildIndex 等で NullReference の原因になる
//        // - SlotNumber は仕様上固定（スロット1〜N）なので毎回上書きして保証する
//        for (int i = 0; i < SlotSummary.Length; i++)
//        {
//            // 要素が null なら空のインスタンスを作る
//            SlotSummary[i] ??= new SlotSummary();

//            // スロット番号は配列順に一致させる（1始まり）
//            // ※ロードデータ側が壊れていても、ここで必ず整合させる
//            SlotSummary[i].SlotNumber = i + 1;
//        }
//    }


//    /// <summary>
//    /// 簡易版スロット情報同期メソッド
//    /// 
//    /// PlayGameData（実セーブデータ）から、スロット一覧表示などに使う「簡易情報（SlotSummary）」を生成する。
//    /// 最終セーブ時刻と最後にいたシーンIDの同期処理
//    /// 
//    /// 目的:
//    /// - スロット選択画面などで「最終セーブ日時」「最後にいたシーン」などを軽量に表示したい。
//    /// - そのため、PlayGameData 全体を参照せず、必要最低限の情報だけを SlotSummary に抜き出す。
//    /// </summary>
//    /// <param name="data">元となるプレイデータ。null の場合は空データとして扱う。</param>
//    /// <returns>スロット表示用の要約データ（SlotSummary）</returns>
//    private SlotSummary BuildSlotSummary(PlayGameData data)
//    {
//        // 呼び出し側が null を渡しても落ちないように補完
//        data ??= new PlayGameData();

//        // SlotSummary に必要な項目だけを転記して返す
//        return new SlotSummary
//        {
//            // 最終セーブ時刻（UnixTime 等の long を想定）
//            LastSaveTime = data.LastPlaySaveTime,

//            // 最後にいたシーンID（null の可能性に備え、空文字に寄せる）
//            LastSceneId = data.LastSceneId ?? ""
//        };
//    }


//    /// <summary>
//    /// スロット番号バリデーションメソッド
//    /// 
//    /// スロット番号が有効範囲（1～ManualSlotCount）に収まっているか検証する。
//    /// 
//    /// 目的:
//    /// - 0 や 4 などの不正な値が入った状態で配列アクセス（slotNumber-1）や
//    ///   ファイル名生成を行うと、例外や誤ファイル参照につながるため、入口で即座に防ぐ。
//    /// 
//    /// 仕様:
//    /// - スロット番号は「1始まり」（UI表示やファイル名の規約に合わせる）
//    /// - 内部配列アクセスでは slotNumber1to3 - 1 を使う前提
//    /// </summary>
//    /// <param name="slotNumber1to3">1始まりのスロット番号（例: 1..3）</param>
//    /// <exception cref="ArgumentOutOfRangeException">
//    /// 範囲外の場合にスローする（呼び出し側のバグを早期に顕在化させる目的）
//    /// </exception>
//    private static void ValidateSlotRange(int slotNumber1to3)
//    {
//        // ManualSlotCount はスロット数（現状は 3）だが、将来変更されてもここが追従する
//        if (slotNumber1to3 < 1 || slotNumber1to3 > SaveDataConstants.ManualSlotCount)
//            throw new ArgumentOutOfRangeException(nameof(slotNumber1to3), "slotNumber must be 1..3");
//    }

//    /// <summary>
//    /// プレイゲームデータキャッシュメソッド
//    /// 
//    /// PlayGameData を「キャッシュ用」に複製する（簡易ディープコピー）。
//    /// 
//    /// 目的:
//    /// - ManualSlots[slotIndex] に保存した時点のスナップショットを保持したい場合、
//    ///   参照をそのまま入れると WorkingPlayGameData の変更がキャッシュにも反映されてしまう。
//    /// - そのため、JSON シリアライズ → デシリアライズを使って複製を作る。
//    /// 
//    /// 注意点:
//    /// - JsonUtility が扱えない型（Dictionary / interface / 一部のプロパティのみ等）はコピーされない。
//    /// - パフォーマンスコストがあるため、頻繁に呼ぶ用途には向かない（保存時など限定的に使う想定）。
//    /// - 参照循環がある構造は扱えない（JsonUtilityの制約）。
//    /// </summary>
//    /// <param name="src">複製元。null の場合は空の PlayGameData を返す。</param>
//    /// <returns>複製した PlayGameData（復元失敗時も null を返さず new で補完）</returns>
//    private static PlayGameData CloneForCache(PlayGameData src)
//    {
//        // 呼び出し側が null を渡しても落ちないように、空インスタンスを返す
//        if (src == null) return new PlayGameData();

//        // src を JSON に変換（prettyPrint=false でサイズ/速度優先）
//        var json = JsonUtility.ToJson(src, false);

//        // JSON から新しいインスタンスを生成（簡易ディープコピー）
//        var clone = JsonUtility.FromJson<PlayGameData>(json);

//        // FromJson が null を返すケース（異常系）に備えて new で補完
//        return clone ?? new PlayGameData();
//    }

//}
