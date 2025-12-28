using System;
using System.IO;
using UnityEngine;

/// <summary>
/// セーブファイルの「フォーマット（ファイル構造）」を定義するクラス。
/// - ヘッダの構造（何をどの順番で書くか）を一箇所に集約する
/// - SaveManager（I/O）や SaveData（中身）から、互換性や識別の責務を切り分ける
/// </summary>
public static class SaveFileFormat
{
    /// <summary>
    /// 4バイトの識別子（マジック値）。
    /// 別ファイルや破損ファイルを読み込もうとした際に「これはセーブファイルではない」と即検出するための固定値。
    /// 
    /// 注意：
    /// - int (Int32) で書く場合、一般的な環境はリトルエンディアンなので
    ///   ファイル上の並びは "SAVE" の見た目通りにならないことがある。
    /// - ただし Read/Write が対になっていれば動作上は問題ない。
    /// </summary>
    public const int Magic = 0x53415645; // 'S''A''V''E' 相当

    /// <summary>
    /// セーブファイル形式のバージョン（フォーマットバージョン）。
    /// - ゲームのアプリバージョン（1.2.3等）とは別物
    /// - 「ヘッダ構造やpayloadの持ち方」を変更したときに上げる
    /// - 旧フォーマットを読みたい場合は Load 側で version 分岐して対応する
    /// </summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>
    /// セーブファイルに適用したオプションを表すフラグ。
    /// 例：
    /// - 圧縮して保存したか
    /// - 暗号化して保存したか
    /// 
    /// [Flags] 属性によりビットORで複数を同時に表現できる。
    /// </summary>
    [Flags]
    public enum SaveFlags : byte
    {
        /// <summary>オプションなし</summary>
        None = 0,

        /// <summary>payloadが圧縮されている</summary>
        Compressed = 1 << 0,

        /// <summary>payloadが暗号化されている</summary>
        Encrypted = 1 << 1,
    }

    /// <summary>
    /// ファイル先頭に置くヘッダ情報（読み取った結果をまとめて扱うための構造体）。
    /// 
    /// 典型的にはファイル構造は以下：
    /// [Magic(int)][FormatVersion(int)][Flags(byte)][PayloadLength(int)][Payload(bytes...)]
    /// </summary>
    public struct Header
    {
        /// <summary>マジック値（セーブファイル識別用）</summary>
        public int Magic;

        /// <summary>フォーマットバージョン（互換性判定用）</summary>
        public int FormatVersion;

        /// <summary>圧縮/暗号化などのオプションフラグ</summary>
        public SaveFlags Flags;

        /// <summary>payload（本体データ）のバイト長</summary>
        public int PayloadLength;
    }

    /// <summary>
    /// ヘッダを書き込む。
    /// - SaveManager.Save() などから呼び出して、ファイル先頭にメタ情報を固定順で書く。
    /// - payloadLength は「この後に続く payload のバイト数」を指定する。
    /// </summary>
    /// <param name="w">BinaryWriter（書き込み先ストリーム）</param>
    /// <param name="flags">圧縮/暗号化などのオプション</param>
    /// <param name="payloadLength">payload のバイト長</param>
    public static void WriteHeader(BinaryWriter w, SaveFlags flags, int payloadLength)
    {
        // ファイル識別子（まずこれが一致しないなら別ファイル）
        w.Write(Magic);

        // 現在のフォーマットバージョンを書き込む
        // これにより Load 時に version 分岐して読み方を変えられる
        w.Write(CurrentFormatVersion);

        // フラグ（byte で省サイズ）
        w.Write((byte)flags);

        // payload の長さ（この後 ReadBytes(payloadLength) などで切り出すため）
        w.Write(payloadLength);
    }

    /// <summary>
    /// ヘッダを読み込む。
    /// - SaveManager.Load() などから呼び出し、先頭から固定順で読み取る。
    /// - 読み取った値の妥当性チェック（magic一致、長さの異常など）をここで行う。
    /// </summary>
    /// <param name="r">BinaryReader（読み込み元ストリーム）</param>
    /// <returns>読み取ったヘッダ</returns>
    /// <exception cref="InvalidDataException">
    /// magic不一致（別ファイル/破損）またはpayloadLength不正などの場合に投げる。
    /// </exception>
    public static Header ReadHeader(BinaryReader r)
    {
        // 先頭から決め打ち順で読む（WriteHeader と同じ順序である必要がある）
        var h = new Header
        {
            Magic = r.ReadInt32(),
            FormatVersion = r.ReadInt32(),
            Flags = (SaveFlags)r.ReadByte(),
            PayloadLength = r.ReadInt32(),
        };

        // セーブファイルかどうかの一次判定
        if (h.Magic != Magic)
            throw new InvalidDataException("Not a save file (magic mismatch).");

        // 破損や攻撃的データを考慮した最低限のガード
        // ※ 実運用では「残りストリーム長よりPayloadLengthが大きい」もチェックするとより安全
        if (h.PayloadLength < 0)
            throw new InvalidDataException("Invalid payload length.");

        return h;
    }
}
