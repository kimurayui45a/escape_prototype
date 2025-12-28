using System;

/// <summary>
/// 日時管理用ユーティリティクラス。
/// 時刻取得・変換の処理を提供する。
/// - 保存・比較：UTC の Unix 秒（long）
/// - 表示：JST へ変換して文字列化
/// </summary>
public static class TimeUtil
{
    /// <summary>
    /// JST（UTC+9）の固定オフセット。
    /// </summary>
    private static readonly TimeSpan JstOffset = TimeSpan.FromHours(9);

    /// <summary>
    /// 現在時刻取得メソッド
    /// 現在時刻を UTC 基準の Unix 秒（long）で返す。
    /// セーブデータ（LastPlaySaveTime / GameEndTime）にはこの値を保存する。
    /// </summary>
    public static long NowUtcUnixSeconds()
    {
        long unixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return unixSeconds;
    }

    /// <summary>
    /// UTC の Unix 秒（long）を DateTimeOffset（UTC）に復元する。
    /// </summary>
    /// <param name="utcUnixSeconds">UTC基準のUnix秒</param>
    public static DateTimeOffset FromUtcUnixSeconds(long utcUnixSeconds)
    {
        DateTimeOffset utc = DateTimeOffset.FromUnixTimeSeconds(utcUnixSeconds);
        return utc;
    }

    /// <summary>
    /// JST時刻表示メソッド
    /// UTC の Unix 秒（long）を JST の表示文字列へ変換する。
    /// </summary>
    /// <param name="utcUnixSeconds">UTC基準のUnix秒</param>
    /// <param name="format">日時フォーマット</param>
    public static string FormatJst(long utcUnixSeconds, string format = "yyyy-MM-dd HH:mm:ss")
    {
        DateTimeOffset jst = ToJst(utcUnixSeconds);
        string text = jst.ToString(format) + " (JST)";
        return text;
    }

    /// <summary>
    /// UTC時刻表示メソッド
    /// UTC の Unix 秒（long）を UTC の表示文字列へ変換する（デバッグ用）。
    /// </summary>
    /// <param name="utcUnixSeconds">UTC基準のUnix秒</param>
    /// <param name="format">日時フォーマット</param>
    public static string FormatUtc(long utcUnixSeconds, string format = "yyyy-MM-dd HH:mm:ss")
    {
        DateTimeOffset utc = FromUtcUnixSeconds(utcUnixSeconds);
        string text = utc.ToString(format) + " (UTC)";
        return text;
    }

    /// <summary>
    /// JST変換メソッド
    /// UTC の Unix 秒（long）を JST の DateTimeOffset に変換する（表示用）。
    /// 保存値は UTC のままにしておき、表示が必要な箇所でのみ変換する。
    /// </summary>
    /// <param name="utcUnixSeconds">UTC基準のUnix秒</param>
    private static DateTimeOffset ToJst(long utcUnixSeconds)
    {
        DateTimeOffset utc = FromUtcUnixSeconds(utcUnixSeconds);
        DateTimeOffset jst = utc.ToOffset(JstOffset);
        return jst;
    }

}