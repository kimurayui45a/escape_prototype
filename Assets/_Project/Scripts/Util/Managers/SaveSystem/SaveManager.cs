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













//using System.IO;
//using UnityEngine;

///// <summary>
///// セーブ/ロードを担当するマネージャ（シングルトン）。
///// MonoBehaviour ではなく通常クラスなので、シーン遷移しても Instance は維持される（static参照が残るため）。
///// </summary>
//public class SaveManager
//{
//    /// <summary>
//    /// シングルトン実体（初回アクセス時に生成）
//    /// </summary>
//    private static SaveManager instance;

//    /// <summary>
//    /// 外部から取得するシングルトン入口。
//    /// instance が未生成なら new SaveManager() で生成する。
//    /// </summary>
//    public static SaveManager Instance
//    {
//        get
//        {
//            if (instance == null)
//            {
//                instance = new SaveManager();
//            }
//            return instance;
//        }
//    }

//    // バージョン文字列
//    private const string FileVersion = "0.0.1";

//    /// <summary>
//    /// セーブデータ保存用ディレクトリ（persistentDataPath 配下）を作るためのパスを返す
//    /// 例: .../AppName/save
//    /// </summary>
//    string MakeSaveDir()
//    {
//        // Path.Combine：複数pathの結合メソッド
//        return Path.Combine(Application.persistentDataPath, "save");
//    }

//    /// <summary>
//    /// ファイル名からセーブデータのフルパスを組み立てる
//    /// 例: .../AppName/save/saveData.dat
//    /// </summary>
//    string MakeSavePath(string fileName)
//    {
//        return Path.Combine(MakeSaveDir(), fileName);
//    }

//    /// <summary>
//    /// SaveData を指定ファイル名で保存する（バイナリ形式）。
//    /// 内部的には MemoryStream に書き込み → byte[] にしてファイルにWriteAllBytesする。
//    /// </summary>
//    public void Save(SaveData saveData, string fileName)
//    {
//        // 保存先フルパスを生成
//        var path = MakeSavePath(fileName);
//        var tempPath = path + ".tmp"; // 一時ファイル
//        var dir = MakeSaveDir();      // 使い回し


//        try // 例外で落ちないように
//        {
//            // 保存ディレクトリが無ければ作成
//            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

//            byte[] bytes;

//            // メモリ上に一旦書き込んでからファイルに保存する
//            using (MemoryStream stream = new MemoryStream())
//            {
//                // バイナリ書き込み用ライター
//                using (BinaryWriter writer = new BinaryWriter(stream))
//                {
//                    // 先頭にバージョン文字列を書いておく（互換性チェック用）
//                    writer.Write(FileVersion);

//                    // SaveData 側の書き込み処理（実際のデータ）を呼ぶ
//                    saveData.Write(writer);

//                    writer.Flush();
//                    bytes = stream.ToArray();
//                }
//                // temp を作る
//                File.WriteAllBytes(tempPath, bytes);

//                // ここで暗号化/圧縮を入れる想定（現在は未実装）
//                // 例: stream.ToArray() を暗号化してから保存する、など

//                // MemoryStreamの中身（byte配列）をファイルに書き込む
//                // 置換（同名があれば上書き）
//                if (File.Exists(path)) File.Delete(path);
//                File.Move(tempPath, path);

//                Debug.Log("Saved to " + path);
//            }

//        }
//        catch (System.Exception e)
//        {
//            Debug.LogError($"Save failed: {e}");

//            // temp が残っていたら掃除
//            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
//        }
//    }

//    /// <summary>
//    /// 指定ファイル名から SaveData をロードする。
//    /// ファイルが無い場合は null を返す。
//    /// </summary>
//    public SaveData Load(string fileName)
//    {
//        // 読み込み先フルパス
//        var path = MakeSavePath(fileName);

//        // ファイルが存在しないならロード不能
//        if (!File.Exists(path)) return null;

//        Debug.Log("Load to " + path);

//        try
//        {
//            // ファイルのバイト列を全読み込み
//            byte[] bytes = File.ReadAllBytes(path);

//            // ここで復号化/解凍を入れる想定（現在は未実装）
//            // 例: bytes を復号してから MemoryStream に渡す、など


//            // バイト列をメモリストリームに載せて、BinaryReaderで順に読む
//            using (MemoryStream stream = new MemoryStream(bytes))
//            {
//                using (BinaryReader reader = new BinaryReader(stream))
//                {
//                    // 先頭のバージョン文字列を読む
//                    var version = reader.ReadString();

//                    // 想定バージョンと一致しない場合は互換性なしとして失敗扱い
//                    if (version != FileVersion)
//                    {
//                        Debug.LogError("セーブデータのバージョンが異なります");
//                        return null;
//                    }

//                    // 返却する SaveData を生成
//                    var ret = new SaveData();

//                    // SaveData 側の読み込み処理（実際のデータ）を呼ぶ
//                    ret.Read(reader);

//                    return ret;
//                }
//            }

//        }
//        catch (System.Exception e)
//        {
//            Debug.LogError($"Load failed: {e}");
//            return null;
//        }
//    }

//    /// <summary>
//    /// 指定ファイル名のセーブデータを削除する
//    /// </summary>
//    public void DeleteSave(string fileName)
//    {
//        var path = MakeSavePath(fileName);
//        if (File.Exists(path))
//        {
//            File.Delete(path);
//        }
//    }

//    /// <summary>
//    /// セーブディレクトリごと削除（全セーブデータ削除）
//    /// </summary>
//    public void DeleteSaveAll()
//    {
//        var path = MakeSaveDir();
//        if (Directory.Exists(path))
//        {
//            // true: サブディレクトリ/ファイルも含めて削除
//            Directory.Delete(path, true);
//        }
//    }
//}