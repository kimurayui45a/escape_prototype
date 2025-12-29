using System;
using System.IO;
using UnityEngine;

/// <summary>
/// セーブ/ロードの「I/O（ファイル操作）」を担当するクラス。
/// 
/// 役割：
/// - 保存先ディレクトリ/パスの決定（persistentDataPath 配下など）
/// - ファイルの読み書き（WriteAllBytes/ReadAllBytes）
/// - 破損しにくい保存（tmpに書いてから置換するアトミック保存）
/// - ヘッダ（SaveFileFormat）と payload（SaveData）を組み立てて 1 ファイルにする
/// - Load 時にフォーマットバージョンで読み分け（マイグレーションの入口）
/// 
/// 注意：
/// - MonoBehaviour ではないためシーン遷移しても instance は static 参照として残る（Unity終了で消える）
/// - 暗号化/圧縮など「payload全体に対する処理」は原則 SaveManager で行うと責務が分離しやすい
/// </summary>
public class SaveManager
{
    /// <summary>
    /// シングルトン実体。
    /// </summary>
    private static SaveManager instance;

    /// <summary>
    /// シングルトン入口。
    /// 初回アクセス時に new SaveManager() で生成する。
    /// </summary>
    public static SaveManager Instance => instance ?? (instance = new SaveManager());

    /// <summary>
    /// セーブデータの保存先ディレクトリを返す。
    /// 例：{persistentDataPath}/save
    /// </summary>
    string MakeSaveDir()
        => Path.Combine(Application.persistentDataPath, "save");

    /// <summary>
    /// fileName から保存ファイルのフルパスを生成する。
    /// 例：{persistentDataPath}/save/saveData.dat
    /// </summary>
    string MakeSavePath(string fileName)
        => Path.Combine(MakeSaveDir(), fileName);

    /// <summary>
    /// SaveData を指定ファイル名で保存する。
    /// 
    /// ファイル構造：
    /// [SaveFileFormatヘッダ] + [payload(SaveDataの中身)]
    /// 
    /// 破損対策：
    /// - いきなり本番ファイルを書き換えず、.tmp に書いてから置換する（アトミック保存）
    /// </summary>
    public void Save(SaveData saveData, string fileName)
    {
        // 保存先本体パス
        var path = MakeSavePath(fileName);

        // 一時ファイルパス（途中で落ちても本体を壊さないため）
        var tempPath = path + ".tmp";

        // 保存先ディレクトリ
        var dir = MakeSaveDir();

        try
        {
            // ディレクトリが無ければ作成
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            // ---- payload 生成（ゲームデータ本体）----
            // SaveData 側で JSON→UTF8 bytes を作る（ヘッダはここでは扱わない）
            byte[] payload = saveData.ToPayloadBytes();

            // ---- flags（必要になったら Compressed/Encrypted を立てる）----
            // 例：payload を圧縮した場合は SaveFlags.Compressed を付ける、など
            var flags = SaveFileFormat.SaveFlags.None;

            // ---- ヘッダ + payload を 1 つの byte[] に組み立てる ----
            // MemoryStream に書き込み → 最後に ToArray() でファイル用のbyte列にする
            byte[] fileBytes;
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                // ファイル先頭：ヘッダ書き込み（Magic/FormatVersion/Flags/PayloadLength）
                SaveFileFormat.WriteHeader(w, flags, payload.Length);

                // ヘッダの直後：payload を生バイトとして書く
                // ※ BinaryWriter.Write(byte[]) は「長さ情報を付けずに、そのまま書く」
                w.Write(payload);

                // 念のためフラッシュ
                w.Flush();

                // MemoryStream の中身を取り出して、ファイルに書ける形にする
                fileBytes = ms.ToArray();
            }

            // ---- アトミック保存（tmp → move）----
            // 1) temp に書く
            if (File.Exists(tempPath)) File.Delete(tempPath);
            File.WriteAllBytes(tempPath, fileBytes);

            // 2) 既存の本体があれば消す（環境によっては Move が上書きできないため）
            if (File.Exists(path)) File.Delete(path);

            // 3) temp を本体にリネーム（置換）
            File.Move(tempPath, path);

            Debug.Log("Saved to " + path);
        }
        catch (Exception e)
        {
            Debug.LogError($"Save failed: {e}");

            // エラー時は tmp が残る可能性があるので掃除する（掃除も例外を握りつぶす）
            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
        }
    }

    /// <summary>
    /// 指定ファイル名から SaveData をロードする。
    /// 
    /// - ファイルが存在しなければ null
    /// - ヘッダを読んで、フォーマットバージョンで読み方を分岐する（マイグレーションの入口）
    /// </summary>
    public SaveData Load(string fileName)
    {
        // 読み込み先パス
        var path = MakeSavePath(fileName);

        // 無ければロード不能
        if (!File.Exists(path)) return null;

        try
        {
            // ファイルを丸ごとメモリに読み込む
            // ※ セーブファイルが巨大化する場合は Stream で逐次読みが望ましい
            byte[] bytes = File.ReadAllBytes(path);

            // MemoryStream に載せて BinaryReader で順に読む
            using (var ms = new MemoryStream(bytes))
            using (var r = new BinaryReader(ms))
            {
                // ---- ヘッダを読む ----
                // Magic/Version/Flags/PayloadLength を取得し、不正なら例外が飛ぶ
                var header = SaveFileFormat.ReadHeader(r);

                // ---- フォーマットバージョンで読み分け（マイグレーションの入口）----
                // 例：case 2: LoadV2(r, header); を追加していく
                switch (header.FormatVersion)
                {
                    case 1:
                        return LoadV1(r, header);

                    default:
                        // 「新しすぎる」or「未知」なら弾く
                        Debug.LogError($"Unsupported save format version: {header.FormatVersion}");
                        return null;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Load failed: {e}");
            return null;
        }
    }

    /// <summary>
    /// フォーマット v1 の読み込み。
    /// 
    /// - payloadLength 分だけ ReadBytes で切り出す
    /// - flags に応じて復号/解凍（必要になったら）を行う
    /// - SaveData に payload を渡して復元する
    /// </summary>
    private SaveData LoadV1(BinaryReader r, SaveFileFormat.Header header)
    {
        // ---- payload取り出し ----
        // header.PayloadLength はヘッダに書かれていた「payloadのバイト数」
        var payload = r.ReadBytes(header.PayloadLength);

        // 破損などで payloadLength 分取れなかった場合を検出
        if (payload == null || payload.Length != header.PayloadLength)
            throw new InvalidDataException("Payload length mismatch (corrupted save).");

        // ---- flags に応じて復号/解凍（今回は未実装）----
        // ※ 実装する場合は「保存時に行った順番の逆」で戻すのが基本
        // if (header.Flags.HasFlag(SaveFileFormat.SaveFlags.Encrypted)) payload = Decrypt(payload);
        // if (header.Flags.HasFlag(SaveFileFormat.SaveFlags.Compressed)) payload = Decompress(payload);

        // payload（JSON UTF-8 bytes）を SaveData に渡して復元
        var ret = new SaveData();
        ret.FromPayloadBytes(payload);
        return ret;
    }

    /// <summary>
    /// 指定ファイル名のセーブデータを削除する。
    /// </summary>
    public void DeleteSave(string fileName)
    {
        var path = MakeSavePath(fileName);
        if (File.Exists(path)) File.Delete(path);
    }

    /// <summary>
    /// セーブディレクトリごと削除する（全セーブデータ削除）。
    /// </summary>
    public void DeleteSaveAll()
    {
        var dir = MakeSaveDir();
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }
}






// 将来的に下記で実装する


//using System;
//using System.IO;
//using UnityEngine;

///// <summary>
///// セーブ/ロードの入出力を担当するクラス。
///// - 保存先ディレクトリ（persistentDataPath配下）の管理
///// - payload（JSON→UTF8 bytes）の生成/復元は BinaryDataManager に委譲
///// - ファイルフォーマット（Magic/Version/Flags/PayloadLength）は SaveFileFormat に従う
///// - .tmp を使った「安全な書き込み」（書き込み完了後に本体へ置換）
///// 
///// 注意:
///// - MonoBehaviour ではない純C#シングルトン
///// - 現状はスレッドセーフではない（基本メインスレッド運用想定）
///// </summary>
//public sealed class SaveManager : ISaveManager
//{

//    private static SaveManager instance;

//    public static SaveManager Instance => instance ??= new SaveManager();


//    /// <summary>
//    /// セーブファイル格納ディレクトリのフルパス。
//    /// 例: Application.persistentDataPath/Saves
//    /// </summary>
//    public string SaveDirectory { get; }

//    /// <summary>
//    /// ログ出力を行うか。
//    /// </summary>
//    public bool EnableLog { get; set; } = true;

//    /// <summary>
//    /// コンストラクタ（外部から new させないため private）。
//    /// subDirectoryName を変えると保存先のサブディレクトリを切り替えられる。
//    /// </summary>
//    private SaveManager(string subDirectoryName = "Saves")
//    {
//        // persistentDataPath はOS/端末ごとの永続領域
//        SaveDirectory = Path.Combine(Application.persistentDataPath, subDirectoryName);

//        // 起動時点でディレクトリを保証
//        EnsureDirectory();
//    }


//    // =========================================================
//    // Save
//    // =========================================================

//    /// <summary>
//    /// 共通セーブメソッド
//    /// 
//    /// 任意データを指定ファイル名で保存する。
//    /// 
//    /// 保存形式:
//    /// - [SaveFileFormat.Header] + [payload bytes]
//    ///   payload は BinaryDataManager により「JSON → UTF8 bytes」に変換されたもの。
//    /// 
//    /// 安全性:
//    /// - 直接本体ファイルを書き換えず、いったん .tmp に書き込んでから置換することで、
//    ///   保存中にクラッシュ/例外が起きても「壊れた本体」を残しにくくする。
//    /// </summary>
//    /// <typeparam name="T">保存したいデータ型</typeparam>
//    /// <param name="fileName">保存ファイル名（SaveDirectory 配下）</param>
//    /// <param name="data">保存するデータ</param>
//    public void Save<T>(string fileName, T data)
//    {
//        // 保存先ディレクトリが消されている/初回で存在しない可能性に備え、毎回保証しておく
//        EnsureDirectory();

//        // 保存先のフルパスを組み立てる（SaveDirectory + fileName）
//        var fullPath = GetFullPath(fileName);

//        // 安全な置換のために一時ファイルへ書く（例: xxx.dat.tmp）
//        var tempPath = fullPath + ".tmp";

//        // データ本体を payload（byte[]）に変換する
//        // ここでは「JSON文字列 → UTF8バイト列」という実装（BinaryDataManager に委譲）
//        var payload = BinaryDataManager.ToBytes(data);

//        // 保存オプション（圧縮/暗号化など）を示すフラグ
//        // 現状は何もしないため None
//        var flags = SaveFileFormat.SaveFlags.None;

//        try
//        {
//            // ★ 一時ファイルへ「ヘッダ + payload」を書き込む
//            // FileMode.Create   : 既に存在していても作り直す（上書き）
//            // FileAccess.Write  : 書き込み専用
//            // FileShare.None    : 書き込み中に他が掴めないようにする（読み/書きの競合防止）
//            using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
//            using (var w = new BinaryWriter(fs))
//            {
//                // 先頭にヘッダを書く
//                // - Magic（セーブファイル識別）
//                // - FormatVersion（互換性判定）
//                // - Flags（圧縮/暗号化などの有無）
//                // - PayloadLength（この後に続くpayloadの長さ）
//                SaveFileFormat.WriteHeader(w, flags, payload.Length);

//                // ヘッダの直後に payload を書く（復元時は PayloadLength 分だけ読む）
//                w.Write(payload);
//            }

//            // ★ 本体ファイルへ置換する
//            // 既存ファイルがある場合は削除してから Move
//            // （OSによっては Move が上書きできないため Delete→Move を採用）
//            if (File.Exists(fullPath)) File.Delete(fullPath);
//            File.Move(tempPath, fullPath);

//            // 任意ログ
//            if (EnableLog)
//                Debug.Log($"[SaveManager] Saved: {fullPath} (payload {payload.Length} bytes)");
//        }
//        catch (Exception ex)
//        {
//            // 失敗時：途中まで書かれた .tmp が残る可能性があるため、可能なら削除する
//            // （削除失敗しても本体は基本的に無事なので握りつぶす）
//            TryDeleteSilently(tempPath);

//            if (EnableLog)
//                Debug.LogError($"[SaveManager] Save failed: {fullPath}\n{ex}");

//            // 保存失敗は上位でハンドリングできるよう例外を再スロー
//            throw;
//        }
//    }


//    // =========================================================
//    // Load
//    // =========================================================

//    /// <summary>
//    /// 共通ロードメソッド
//    /// 
//    /// 指定ファイル名から読み込みを試みる。
//    /// - 成功時 true（out data に復元結果）
//    /// - 失敗時 false（out data は new T() のまま）
//    /// 
//    /// 互換性:
//    /// - SaveFileFormat ヘッダ付きファイルを基本として読む
//    /// - ヘッダが無い（旧形式）場合は「全体をpayload」として読むレガシーフォールバックも行う
//    /// </summary>
//    public bool TryLoad<T>(string fileName, out T data) where T : new()
//    {
//        // 呼び出し側が null を掴まないように初期値を先に作っておく
//        data = new T();

//        var fullPath = GetFullPath(fileName);

//        // ファイルが存在しない場合は正常系として false
//        if (!File.Exists(fullPath))
//        {
//            if (EnableLog) Debug.Log($"[SaveManager] ロードできるファイルがありません: {fullPath}");
//            return false;
//        }

//        try
//        {
//            // 読み込みは共有可（FileShare.Read）
//            using (var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
//            using (var r = new BinaryReader(fs))
//            {
//                SaveFileFormat.Header h;

//                try
//                {
//                    // ★ ヘッダを読む（Magic/Version/Flags/PayloadLength）
//                    h = SaveFileFormat.ReadHeader(r);
//                }
//                catch (InvalidDataException) // TryFromBytes が成功すると、out data に復元した T が代入される
//                {
//                    // ★ レガシー（ヘッダ無し）として全体を payload とみなして読む
//                    fs.Position = 0;
//                    var legacyBytes = r.ReadBytes((int)fs.Length);

//                    if (!BinaryDataManager.TryFromBytes(legacyBytes, out data))
//                    {
//                        if (EnableLog) Debug.LogWarning($"[SaveManager] ファイルの読み込みに失敗しました: {fullPath}");
//                        return false;
//                    }

//                    if (EnableLog)
//                        Debug.Log($"[SaveManager] ファイルの読み込みに成功しました: {fullPath} ({legacyBytes.Length} bytes)");
//                    return true;
//                }

//                // 未来バージョン（こちらが対応していない新形式）なら弾く
//                // ※ 旧形式の読み対応を増やすなら、ここで version 分岐を作る
//                if (h.FormatVersion > SaveFileFormat.CurrentFormatVersion)
//                {
//                    if (EnableLog)
//                        Debug.LogWarning($"[SaveManager] ファイルフォーマットのバージョンが対応していません: {h.FormatVersion} (current {SaveFileFormat.CurrentFormatVersion})");
//                    return false;
//                }

//                // payload 長が不正なら弾く（破損/攻撃的入力対策）
//                // ※より堅くするなら「残りストリーム長 >= PayloadLength」もチェック推奨
//                if (h.PayloadLength < 0) return false;

//                // payload 部分だけ読む
//                var payload = r.ReadBytes(h.PayloadLength);

//                // 読めた長さが足りない＝途中で切れている（破損）
//                if (payload.Length != h.PayloadLength)
//                {
//                    if (EnableLog) Debug.LogWarning($"[SaveManager] ファイルデータが破損しています: {fullPath}");
//                    return false;
//                }

//                // flags（圧縮/暗号化）がある場合、ここで payload を復号/伸長してから復元する
//                // if (h.Flags.HasFlag(SaveFileFormat.SaveFlags.Compressed)) payload = Decompress(payload);
//                // if (h.Flags.HasFlag(SaveFileFormat.SaveFlags.Encrypted))  payload = Decrypt(payload);

//                // payload を T に復元
//                if (!BinaryDataManager.TryFromBytes(payload, out data))
//                {
//                    if (EnableLog) Debug.LogWarning($"[SaveManager] ファイルの読み込みに失敗しました: {fullPath}");
//                    return false;
//                }

//                if (EnableLog)
//                    Debug.Log($"[SaveManager] ファイルの読み込みに成功しました: {fullPath} (payload {payload.Length} bytes, flags={h.Flags}, ver={h.FormatVersion})");

//                return true;
//            }
//        }
//        catch (Exception ex)
//        {
//            // I/O例外など
//            if (EnableLog) Debug.LogError($"[SaveManager] 予期せぬエラーが発生しました: {fullPath}\n{ex}");
//            return false;
//        }
//    }

//    // =========================================================
//    // File utilities
//    // =========================================================

//    /// <summary>
//    /// 指定ファイルが存在するか（SaveDirectory 配下を前提）。
//    /// </summary>
//    public bool Exists(string fileName) => File.Exists(GetFullPath(fileName));


//    /// <summary>
//    /// ファイル削除メソッド
//    /// 指定ファイルを削除する。
//    /// 
//    /// 挙動:
//    /// - 対象ファイルが存在しない場合は何もしない（no-op）
//    — - 存在する場合は File.Delete で削除する
//    /// - 削除に失敗した場合は例外を投げる（呼び出し側でハンドリング可能）
//    /// 
//    /// 用途例:
//    /// - 未セーブデータ（unsaved.dat）を不要になったタイミングで消す
//    /// - セーブスロットを消去するなど
//    /// </summary>
//    /// <param name="fileName">SaveDirectory 配下のファイル名</param>
//    public void Delete(string fileName)
//    {
//        // 保存ディレクトリ + ファイル名 からフルパスを生成
//        var fullPath = GetFullPath(fileName);

//        // ファイルが無ければ削除不要なので何もしない
//        // （「削除したい」という意図に対して“既に無い”は成功扱い）
//        if (!File.Exists(fullPath)) return;

//        try
//        {
//            // ファイル削除（アクセス権・ロック等で例外が起き得る）
//            File.Delete(fullPath);

//            // 任意ログ
//            if (EnableLog)
//                Debug.Log($"[SaveManager] Deleted: {fullPath}");
//        }
//        catch (Exception ex)
//        {
//            // 削除失敗は状況によって重大（ロック/権限/IO障害）なのでログして呼び出し側へ伝える
//            if (EnableLog)
//                Debug.LogError($"[SaveManager] Delete failed: {fullPath}\n{ex}");

//            // 呼び出し側で復旧・リトライ・ユーザー通知などできるよう例外を再スロー
//            throw;
//        }
//    }

//    // =========================================================
//    // Private helpers
//    // =========================================================

//    /// <summary>
//    /// SaveDirectory が存在しなければ作成する。
//    /// </summary>
//    private void EnsureDirectory()
//    {
//        if (!Directory.Exists(SaveDirectory))
//            Directory.CreateDirectory(SaveDirectory);
//    }

//    /// <summary>
//    /// 保存ディレクトリとファイル名からフルパスを組み立てる。
//    /// </summary>
//    private string GetFullPath(string fileName) => Path.Combine(SaveDirectory, fileName);

//    /// <summary>
//    /// 例外を握りつぶして削除を試す。
//    /// - .tmp 後始末など「失敗しても致命ではない」用途向け
//    /// </summary>
//    private static void TryDeleteSilently(string path)
//    {
//        try
//        {
//            if (File.Exists(path)) File.Delete(path);
//        }
//        catch
//        {
//            // 例外を無視して関数を終える
//        }
//    }
//}





