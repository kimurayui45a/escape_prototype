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
































//using System.IO;
//using UnityEngine;

///// <summary>
///// セーブデータ本体。
///// バイナリ（BinaryWriter/Reader）に対して、自身の内容をWrite/Readできる設計にしている。
///// 
///// 
///// </summary>
//public class SaveData
//{
//    /// <summary>
//    /// 実際に保存したいパラメータ群。
//    /// JsonUtility で扱えるように [Serializable] を付ける。
//    /// 
//    /// ここにフィールドを追加すると、保存される内容が増える。
//    /// （追加/削除が互換性に影響するため、将来はマイグレーションも考慮する）
//    /// </summary>
//    [System.Serializable]
//    public class Param
//    {
//        /// <summary>あとで削除（不要なので）</summary>
//        public int Day = 0;


//        public PlayerData Player = new PlayerData();

//        //ゲームバージョン
//        //セーブ日時
//        //未セーブデータフラグ
//        //設定データ（オブジェクト）
//        //未セーブデータ（オブジェクト）
//        //セーブデータ1（オブジェクト）
//        //セーブデータ2（オブジェクト）
//        //セーブデータ3（オブジェクト）

//    }

//    /// <summary>
//    /// 保存対象のパラメータ。
//    /// 外部からの書き換えを防ぐため private set。
//    /// </summary>
//    public Param SaveParam { get; private set; } = new Param();

//    /// <summary>
//    /// バイナリ出力処理。
//    /// 現状は「Param をJson文字列にして writer.Write(string)」している。
//    /// （バイナリ形式の中に、実体はJson文字列として入っている）
//    /// </summary>
//    public void Write(BinaryWriter writer)
//    {
//        // Param をJson文字列化（prettyPrint=trueで改行付き。容量が気になるなら false 推奨）
//        var json = JsonUtility.ToJson(SaveParam, false);

//        // 文字列としてバイナリに書き込む
//        writer.Write(json);
//    }

//    /// <summary>
//    /// バイナリ読み込み処理。
//    /// writer.WriteしたJson文字列を reader.ReadString() で取り出し、Param に復元する。
//    /// </summary>
//    public void Read(BinaryReader reader)
//    {
//        // Json文字列を読み取り
//        var json = reader.ReadString();

//        // Param型として復元し、SaveParamにセット
//        SaveParam = JsonUtility.FromJson<Param>(json);

//        // 旧データ・破損対策（nullガード）
//        if (SaveParam == null) SaveParam = new Param();
//        if (SaveParam.Player == null) SaveParam.Player = new PlayerData();

//        SaveParam.Player.Validate();
//    }

//    // 橋渡しを SaveData 側に用意しておくと呼び出し側が汚れにくい
//    public void CaptureFromRuntime(int day, PlayerState playerState)
//    {
//        SaveParam.Day = day;
//        SaveParam.Player = (playerState != null) ? playerState.ToData() : new PlayerData();
//    }

//    public void ApplyToRuntime(PlayerState playerState, out int day)
//    {
//        day = SaveParam.Day;
//        if (playerState != null) playerState.Apply(SaveParam.Player);
//    }
//}