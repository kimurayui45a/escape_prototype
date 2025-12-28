using System;
using UnityEngine;


/// <summary>
/// プレイヤー状態管理クラス
/// PlayerStateIdと、快/不快などのパラメータ計算を管理する。
/// - PlayerStateDataSO を使って PlayerStateId -> PlayerStateSO を解決する
/// - PleasureValue / UnPleasantValue の更新・計算（合算など）を提供する
/// - セーブ対象は PlayerParameter 側に保持し、マネージャーは「操作/計算の窓口」になる
/// </summary>
public class PlayerStateManager
{
    // プレイヤー状態一覧
    private readonly PlayerStateDataSO stateMaster;

    // パラメータ範囲
    // 仕様：快値 0..999、不快値 0..999（合算時に不快はマイナスとして扱う）
    private const int PleasureMin = 0;
    private const int PleasureMax = 999;

    private const int UnPleasantMin = 0;
    private const int UnPleasantMax = 999;

    // セーブ対象のパラメータ（保存する値）
    private int pleasureValue;
    private int unPleasantValue;
    private string playerStateId;

    // ランタイム専用の補正（保存しない）
    // 例：バフ/デバフ、装備効果、イベント中の一時補正など
    private int pleasureDeltaRuntime;
    private int unPleasantDeltaRuntime;

    /// <summary>変更フラグ：未保存の変更が入ったか（セーブ対象が変わったらtrue）</summary>
    public bool IsDirty { get; private set; }

    /// <summary>状態変更通知：状態IDが変わった等の通知が欲しい場合に使う（任意）</summary>
    public event Action<string> OnStateChanged;

    /// <summary>パラメータ変更通知：パラメータが変わった通知（任意）</summary>
    public event Action OnParamChanged;

    public PlayerStateManager(PlayerStateDataSO master)
    {
        stateMaster = master;
    }


    // -------------------------
    // セーブデータ適用/取り出し
    // -------------------------

    /// <summary>
    /// プレイヤー状況ロードメソッド
    /// ロードした PlayerParameter を適用する（I/Oはしない）。
    /// ロード直後は「未変更扱い」なので IsDirty は false に戻す。
    /// </summary>
    public void LoadFromPlayerParameter(PlayerParameter data)
    {
        if (data == null) data = new PlayerParameter();

        // 快・不快、状態の反映
        pleasureValue = data.PleasureValue;
        unPleasantValue = data.UnPleasantValue;
        playerStateId = data.PlayerStateId;

        // ランタイム補正はセーブしないので、ロード時はリセット（必要なら残す設計も可）
        pleasureDeltaRuntime = 0;
        unPleasantDeltaRuntime = 0;

        IsDirty = false;
        OnParamChanged?.Invoke();
    }

    /// <summary>
    /// プレイヤー状況セーブメソッド
    /// 現在のセーブ対象値を PlayerParameter に書き戻す（スナップショット）。
    /// ※セーブ処理側がこの結果をファイルに書く想定。
    /// </summary>
    public void WriteToPlayerParameter(PlayerParameter data)
    {
        // 渡された値がnullであったら例外でスロー
        if (data == null) throw new ArgumentNullException(nameof(data));

        // 現在の快・不快、状態をデータに保存
        data.PleasureValue = pleasureValue;
        data.UnPleasantValue = unPleasantValue;
        data.PlayerStateId = playerStateId;

        // 書き戻した＝セーブした、という運用なら外側で IsDirty を落とす/またはここで落とす
        // ただし「実際にファイル保存できたか」はこのクラスでは分からないため、
        // Dirty解除はセーブ成功後に呼ぶ方が堅い。
        // 現在はセーブしてもIsDirtyは変動しない設計
    }

    public void ClearDirty() => IsDirty = false;


    // -------------------------
    // 状態解決（SO参照）
    // -------------------------

    /// <summary>
    /// プレイヤー状態取得メソッド
    /// 現在の PlayerStateId から PlayerStateSO を取得する（安全取得版）。
    /// - 取得できたら true / できなければ false
    /// - stateMaster（マスターSO）が未設定、または playerStateId が空の場合は取得できない
    /// </summary>
    /// <param name="state">取得結果（成功時に PlayerStateSO が入る）</param>
    public bool TryGetCurrentState(out PlayerStateSO state)
    {
        // out引数は必ず初期化して返す（失敗時に呼び出し側が安全に扱える）
        state = null;

        // 現在の状態IDが空なら取得不能（呼び出し側の設定漏れ/未初期化の可能性）
        if (string.IsNullOrEmpty(playerStateId)) return false;

        // マスターが無いと ID -> SO の解決ができない
        if (stateMaster == null) return false;

        // マスター辞書から引く（見つかれば true、見つからなければ false）
        return stateMaster.TryGetByPlayerStateId(playerStateId, out state);
    }

    /// <summary>
    /// プレイヤー状態変更メソッド
    /// プレイヤー状態IDを変更する（安全設定版）。
    /// - 変更できたら true / 変更できなければ false
    /// - マスターがある場合は「未登録ID」を弾く（タイポや定義漏れの早期発見）
    /// - 実際に変更が起きたときだけ Dirty を立て、通知（OnStateChanged）を発火する
    /// </summary>
    /// <param name="newStateId">設定したいプレイヤー状態ID（例："state_plant"）</param>
    public bool TrySetPlayerStateId(string newStateId)
    {
        // 入力が空なら不正
        if (string.IsNullOrEmpty(newStateId)) return false;

        // masterがある場合は存在チェックを行う（未登録IDを弾く）
        // out _ は「取得結果は使わない（存在確認だけしたい）」という意味
        if (stateMaster != null && !stateMaster.TryGetByPlayerStateId(newStateId, out _))
        {
            Debug.LogError($"Unknown PlayerStateId: {newStateId}");
            return false;
        }

        // 既に同じIDなら変更不要（Dirtyも立てない・通知もしない）
        if (playerStateId == newStateId) return false;

        // 状態IDを更新
        playerStateId = newStateId;

        // セーブ対象の値が変わったので「未保存の変更あり」にする
        IsDirty = true;

        // 状態変更を外部へ通知（UI更新、演出、能力値再計算などのトリガーに使う）
        OnStateChanged?.Invoke(newStateId);

        return true;
    }



    // -------------------------
    // パラメータ操作（元値＝セーブ対象）
    // -------------------------

    // プレイヤー状態を外部に公開する（読み取り専用）
    public int PleasureValue => pleasureValue;
    public int UnPleasantValue => unPleasantValue;
    public string PlayerStateId => playerStateId;

    /// <summary>
    /// 快値加減メソッド
    /// </summary>
    public void AddPleasure(int delta)
    {
        if (delta == 0) return;

        // 0..999 に丸める（マイナスにしない）
        pleasureValue = Mathf.Clamp(pleasureValue + delta, PleasureMin, PleasureMax);

        IsDirty = true;
        OnParamChanged?.Invoke();
    }

    /// <summary>
    /// 不快値加減メソッド
    /// </summary>
    public void AddUnPleasant(int delta)
    {
        if (delta == 0) return;

        // 0..999 に丸める（マイナスにしない）
        unPleasantValue = Mathf.Clamp(unPleasantValue + delta, UnPleasantMin, UnPleasantMax);

        IsDirty = true;
        OnParamChanged?.Invoke();
    }


    // -------------------------
    // ランタイム専用補正（セーブしない）
    // -------------------------

    /// <summary>
    /// ランタイム専用の「快値補正（デルタ）」を設定する。
    /// - この補正値はセーブしない（ゲーム内一時効果：バフ/デバフ/装備補正/演出中の係数など想定）
    /// - 値が変わったときだけ通知を出す（無駄な更新を避ける）
    /// </summary>
    /// <param name="delta">快値に加算する補正値（正/負どちらも可）</param>
    public void SetRuntimePleasureDelta(int delta)
    {
        // 同じ値なら変更なし（Dirtyも通知も不要）
        if (pleasureDeltaRuntime == delta) return;

        // ランタイム補正値を更新（セーブ対象外）
        pleasureDeltaRuntime = delta;

        // パラメータ変更通知（UI更新・再計算トリガーなど）
        OnParamChanged?.Invoke();
    }

    /// <summary>
    /// ランタイム専用の「不快値補正（デルタ）」を設定する。
    /// - この補正値はセーブしない（ゲーム内一時効果：バフ/デバフ/装備補正など想定）
    /// - 値が変わったときだけ通知を出す
    /// </summary>
    /// <param name="delta">不快値に加算する補正値（正/負どちらも可）</param>
    public void SetRuntimeUnPleasantDelta(int delta)
    {
        // 同じ値なら変更なし
        if (unPleasantDeltaRuntime == delta) return;

        // ランタイム補正値を更新（セーブ対象外）
        unPleasantDeltaRuntime = delta;

        // パラメータ変更通知
        OnParamChanged?.Invoke();
    }


    // -------------------------
    // 快・不快値、計算処理（セーブしない）
    // -------------------------

    /// <summary>
    /// パラメータ合算メソッド
    /// ゲーム内で使う「合算結果」を返す。
    /// 快値 - 不快値
    /// </summary>
    public int GetMoodScore()
    {
        return pleasureValue - unPleasantValue;
    }

}



// ------呼び出し使用例------

//// 起動時（Manager/Singletonなど）
//playerStateManager = new PlayerStateManager(playerStateDataSO);

//// ロード直後
//playerStateManager.LoadFromPlayerParameter(save.PlayerParameter);

//// ゲーム中
//playerStateManager.AddPleasure(+10);
//playerStateManager.AddUnPleasant(-5);
//int mood = playerStateManager.GetMoodScore(); // マイナス許容

//// 状態変更
//playerStateManager.TrySetPlayerStateId("state_bird");

//// セーブ時（ファイル化する前に書き戻す）
//playerStateManager.WriteToPlayerParameter(save.PlayerParameter);
//// ファイル保存成功後に
//playerStateManager.ClearDirty();
