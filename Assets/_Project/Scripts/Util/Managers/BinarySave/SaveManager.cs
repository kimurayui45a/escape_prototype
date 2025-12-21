using System.IO;
using UnityEngine;

/// <summary>
/// セーブ/ロードを担当するマネージャ（シングルトン）。
/// MonoBehaviour ではなく通常クラスなので、シーン遷移しても Instance は維持される（static参照が残るため）。
/// </summary>
public class SaveManager
{
    /// <summary>
    /// シングルトン実体（初回アクセス時に生成）
    /// </summary>
    private static SaveManager instance;

    /// <summary>
    /// 外部から取得するシングルトン入口。
    /// instance が未生成なら new SaveManager() で生成する。
    /// </summary>
    public static SaveManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new SaveManager();
            }
            return instance;
        }
    }
    
    // バージョン文字列
    private const string FileVersion = "0.0.1";

    /// <summary>
    /// セーブデータ保存用ディレクトリ（persistentDataPath 配下）を作るためのパスを返す
    /// 例: .../AppName/save
    /// </summary>
    string MakeSaveDir()
    {
        // Path.Combine：複数pathの結合メソッド
        return Path.Combine(Application.persistentDataPath, "save");
    }

    /// <summary>
    /// ファイル名からセーブデータのフルパスを組み立てる
    /// 例: .../AppName/save/saveData.dat
    /// </summary>
    string MakeSavePath(string fileName)
    {
        return Path.Combine(MakeSaveDir(), fileName);
    }

    /// <summary>
    /// SaveData を指定ファイル名で保存する（バイナリ形式）。
    /// 内部的には MemoryStream に書き込み → byte[] にしてファイルにWriteAllBytesする。
    /// </summary>
    public void Save(SaveData saveData, string fileName)
    {
        // 保存先フルパスを生成
        var path = MakeSavePath(fileName);
        var tempPath = path + ".tmp"; // 一時ファイル
        var dir = MakeSaveDir();      // 使い回し


        try // 例外で落ちないように
        {
            // 保存ディレクトリが無ければ作成
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            byte[] bytes;

            // メモリ上に一旦書き込んでからファイルに保存する
            using (MemoryStream stream = new MemoryStream())
            {
                // バイナリ書き込み用ライター
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    // 先頭にバージョン文字列を書いておく（互換性チェック用）
                    writer.Write(FileVersion);

                    // SaveData 側の書き込み処理（実際のデータ）を呼ぶ
                    saveData.Write(writer);

                    writer.Flush();
                    bytes = stream.ToArray();
                }
                // temp を作る
                File.WriteAllBytes(tempPath, bytes);

                // ここで暗号化/圧縮を入れる想定（現在は未実装）
                // 例: stream.ToArray() を暗号化してから保存する、など

                // MemoryStreamの中身（byte配列）をファイルに書き込む
                // 置換（同名があれば上書き）
                if (File.Exists(path)) File.Delete(path);
                File.Move(tempPath, path);

                Debug.Log("Saved to " + path);
            }

        }
        catch (System.Exception e)
        {
            Debug.LogError($"Save failed: {e}");

            // temp が残っていたら掃除
            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
        }
    }

    /// <summary>
    /// 指定ファイル名から SaveData をロードする。
    /// ファイルが無い場合は null を返す。
    /// </summary>
    public SaveData Load(string fileName)
    {
        // 読み込み先フルパス
        var path = MakeSavePath(fileName);

        // ファイルが存在しないならロード不能
        if (!File.Exists(path)) return null;

        Debug.Log("Load to " + path);

        try
        {
            // ファイルのバイト列を全読み込み
            byte[] bytes = File.ReadAllBytes(path);

            // ここで復号化/解凍を入れる想定（現在は未実装）
            // 例: bytes を復号してから MemoryStream に渡す、など


            // バイト列をメモリストリームに載せて、BinaryReaderで順に読む
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    // 先頭のバージョン文字列を読む
                    var version = reader.ReadString();

                    // 想定バージョンと一致しない場合は互換性なしとして失敗扱い
                    if (version != FileVersion)
                    {
                        Debug.LogError("セーブデータのバージョンが異なります");
                        return null;
                    }

                    // 返却する SaveData を生成
                    var ret = new SaveData();

                    // SaveData 側の読み込み処理（実際のデータ）を呼ぶ
                    ret.Read(reader);

                    return ret;
                }
            }

        }
        catch (System.Exception e)
        {
            Debug.LogError($"Load failed: {e}");
            return null;
        }
    }

    /// <summary>
    /// 指定ファイル名のセーブデータを削除する
    /// </summary>
    public void DeleteSave(string fileName)
    {
        var path = MakeSavePath(fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// セーブディレクトリごと削除（全セーブデータ削除）
    /// </summary>
    public void DeleteSaveAll()
    {
        var path = MakeSaveDir();
        if (Directory.Exists(path))
        {
            // true: サブディレクトリ/ファイルも含めて削除
            Directory.Delete(path, true);
        }
    }
}