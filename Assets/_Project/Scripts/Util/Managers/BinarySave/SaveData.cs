
using System.IO;

using UnityEngine;

/// <summary>
/// セーブデータ本体。
/// バイナリ（BinaryWriter/Reader）に対して、自身の内容をWrite/Readできる設計にしている。
/// </summary>
public class SaveData
{
    /// <summary>
    /// 実際に保存したいパラメータ群（例：日数、好感度）。
    /// JsonUtilityで変換しやすいように [Serializable] を付けている。
    /// </summary>
    [System.Serializable]
    public class Param
    {
        public int Day = 0;
        public PlayerData Player = new PlayerData();
    }

    /// <summary>
    /// 保存対象のパラメータ。
    /// 外部からの書き換えを防ぐため private set。
    /// </summary>
    public Param SaveParam { get; private set; } = new Param();

    /// <summary>
    /// バイナリ出力処理。
    /// 現状は「Param をJson文字列にして writer.Write(string)」している。
    /// （バイナリ形式の中に、実体はJson文字列として入っている）
    /// </summary>
    public void Write(BinaryWriter writer)
    {
        // Param をJson文字列化（prettyPrint=trueで改行付き。容量が気になるなら false 推奨）
        var json = JsonUtility.ToJson(SaveParam, false);

        // 文字列としてバイナリに書き込む
        writer.Write(json);
    }

    /// <summary>
    /// バイナリ読み込み処理。
    /// writer.WriteしたJson文字列を reader.ReadString() で取り出し、Param に復元する。
    /// </summary>
    public void Read(BinaryReader reader)
    {
        // Json文字列を読み取り
        var json = reader.ReadString();

        // Param型として復元し、SaveParamにセット
        SaveParam = JsonUtility.FromJson<Param>(json);

        // 旧データ・破損対策（nullガード）
        if (SaveParam == null) SaveParam = new Param();
        if (SaveParam.Player == null) SaveParam.Player = new PlayerData();

        SaveParam.Player.Validate();
    }

    // ★ 追加：橋渡しを SaveData 側に用意しておくと呼び出し側が汚れにくい
    public void CaptureFromRuntime(int day, PlayerState playerState)
    {
        SaveParam.Day = day;
        SaveParam.Player = (playerState != null) ? playerState.ToData() : new PlayerData();
    }

    public void ApplyToRuntime(PlayerState playerState, out int day) // ★
    {
        day = SaveParam.Day;
        if (playerState != null) playerState.Apply(SaveParam.Player);
    }
}