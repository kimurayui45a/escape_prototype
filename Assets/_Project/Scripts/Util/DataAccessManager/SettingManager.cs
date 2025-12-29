using System;
using UnityEngine;


/// <summary>
/// 設定値管理クラス
/// 設定値（SettingData）の更新・正規化・ロード適用・セーブ書き戻しを担うランタイム管理クラス。
/// - I/O（ファイル読み書き）はしない：外側のSaveSystem等が担当
/// - 値更新時は範囲補正（Clamp）し、Dirtyと通知を管理する
/// </summary>
public class SettingManager
{
    // パラメータ変動範囲
    private const int VolumeMin = 1;
    private const int VolumeMax = 100;
    private const int TextSpeedMin = 1;
    private const int TextSpeedMax = 5;

    // セーブ対象値（元値）
    private bool mute;
    private int bgm;
    private int se;
    private int textSpeed;

    /// <summary>変更フラグ：未保存の変更があるか</summary>
    public bool IsDirty { get; private set; }

    /// <summary>変更通知：（UI更新やAudio反映に使用）</summary>
    public event Action OnChanged;

    /// <summary>
    /// 設定値ロードメソッド
    /// ロードした SettingData を適用する（I/Oなし）。
    /// ロード直後は未変更扱いなので IsDirty を false に戻す。
    /// </summary>
    public void LoadFromSettingData(SettingData data)
    {
        if (data == null) data = new SettingData();

        mute = data.MuteFlag;
        bgm = NormalizeVolume(data.BGM);
        se = NormalizeVolume(data.SE);
        textSpeed = NormalizeTextSpeed(data.TextSpeed);

        IsDirty = false;
        OnChanged?.Invoke();
    }

    /// <summary>
    /// 設定値セーブメソッド
    /// 現在値を SettingData に書き戻す（I/Oなし）。
    /// ※実際にファイル保存できたかはここでは分からないため、
    /// Dirty解除は保存成功後に外側で行うのが堅い。
    /// </summary>
    public void WriteToSettingData(SettingData data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        data.MuteFlag = mute;
        data.BGM = bgm;
        data.SE = se;
        data.TextSpeed = textSpeed;
    }

    public void ClearDirty() => IsDirty = false;


    // ---- 読み取り用プロパティ ----

    public bool MuteFlag => mute;
    public int BGM => bgm;
    public int SE => se;
    public int TextSpeed => textSpeed;

    // ---- 更新メソッド（変更があった時だけDirty/通知）----

    /// <summary>
    /// ミュート設定を更新する。
    /// 変更がない場合は何もせず終了（無駄な保存・通知を避ける）。
    /// </summary>
    /// <param name="value">ミュートON/OFF</param>
    public void SetMute(bool value)
    {
        // 既に同じ値なら更新不要
        if (mute == value) return;

        // 値を更新
        mute = value;

        // 変更フラグを立てて、購読者（UIなど）へ通知する
        MarkDirtyAndNotify();
    }

    /// <summary>
    /// BGM 音量を更新する。
    /// 入力値は正規化（範囲内に丸める）してから保持する。
    /// </summary>
    /// <param name="value">入力された音量（例：1～100想定）</param>
    public void SetBgmVolume(int value)
    {
        // 想定範囲に収める（Clamp等）
        var v = NormalizeVolume(value);

        // 正規化後も値が変わらないなら更新不要
        if (bgm == v) return;

        // 値を更新
        bgm = v;

        // 変更フラグ + 通知
        MarkDirtyAndNotify();
    }

    /// <summary>
    /// SE（効果音）音量を更新する。
    /// 入力値は正規化（範囲内に丸める）してから保持する。
    /// </summary>
    /// <param name="value">入力された音量（例：1～100想定）</param>
    public void SetSeVolume(int value)
    {
        // 想定範囲に収める（Clamp等）
        var v = NormalizeVolume(value);

        // 正規化後も値が変わらないなら更新不要
        if (se == v) return;

        // 値を更新
        se = v;

        // 変更フラグ + 通知
        MarkDirtyAndNotify();
    }

    /// <summary>
    /// 文字送り速度を更新する。
    /// 入力値は正規化（範囲内に丸める）してから保持する。
    /// </summary>
    /// <param name="value">入力された速度（例：1～5想定）</param>
    public void SetTextSpeed(int value)
    {
        // 想定範囲に収める（Clamp等）
        var v = NormalizeTextSpeed(value);

        // 正規化後も値が変わらないなら更新不要
        if (textSpeed == v) return;

        // 値を更新
        textSpeed = v;

        // 変更フラグ + 通知
        MarkDirtyAndNotify();
    }


    // ---- 補助（Audio側で扱いやすい値が欲しい場合）----
    /// <summary>
    /// BGM音量を 0.0～1.0 の正規化値として返す。
    /// - mute が ON の場合は常に 0 を返す（実音量を強制ゼロ）
    /// - mute が OFF の場合は「1～100」を「0.01～1.0」に変換する
    /// </summary>
    /// <returns>BGM音量（0.0～1.0）</returns>
    public float GetBgmVolume01()
    {
        // ミュート時は音量を 0 として扱う
        if (mute) return 0f;

        // int(1～100) を float(0.01～1.0) に変換
        return bgm / 100f;
    }

    /// <summary>
    /// SE音量を 0.0～1.0 の正規化値として返す。
    /// - mute が ON の場合は常に 0 を返す
    /// - mute が OFF の場合は「1～100」を「0.01～1.0」に変換する
    /// </summary>
    /// <returns>SE音量（0.0～1.0）</returns>
    public float GetSeVolume01()
    {
        // ミュート時は音量を 0 として扱う
        if (mute) return 0f;

        // int(1～100) を float(0.01～1.0) に変換
        return se / 100f;
    }

    // ---- 内部 ----

    /// <summary>
    /// 設定が変更されたことを示す内部処理。
    /// - IsDirty を true にして「未保存の変更あり」をマーク
    /// - 変更通知イベントを発火し、UIやAudio側に反映させる
    /// </summary>
    private void MarkDirtyAndNotify()
    {
        IsDirty = true;
        OnChanged?.Invoke();
    }

    /// <summary>
    /// 音量値を仕様範囲に正規化（丸め込み）する。
    /// 仕様が「1～100」なので 0 は許可しない（ミュートは別フラグで表現する）。
    /// </summary>
    /// <param name="value">入力値</param>
    /// <returns>範囲内に収めた音量値</returns>
    private int NormalizeVolume(int value)
    {
        // VolumeMin / VolumeMax はクラス内の定数・readonly等で定義しておく想定
        return Mathf.Clamp(value, VolumeMin, VolumeMax);
    }

    /// <summary>
    /// 文字送り速度を仕様範囲に正規化（丸め込み）する。
    /// </summary>
    /// <param name="value">入力値</param>
    /// <returns>範囲内に収めた文字送り速度</returns>
    private int NormalizeTextSpeed(int value)
    {
        // TextSpeedMin / TextSpeedMax はクラス内の定数・readonly等で定義しておく想定
        return Mathf.Clamp(value, TextSpeedMin, TextSpeedMax);
    }

}

// ------呼び出し使用例------

// // 起動時
// settingManager = new SettingManager();

// // ロード後
// settingManager.LoadFromSettingData(save.Param.SettingData);

// // ゲーム中に更新（UIスライダー/トグル等から）
// settingManager.SetMute(true);
// settingManager.SetBgmVolume(80);
// settingManager.SetTextSpeed(5);

// // セーブ時：書き戻し
// settingManager.WriteToSettingData(save.Param.SettingData);
// // ファイル保存成功後に
// settingManager.ClearDirty();
