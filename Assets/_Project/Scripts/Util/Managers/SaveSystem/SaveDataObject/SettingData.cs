using UnityEngine;


/// <summary>
/// 設定値フィールドクラス
/// </summary>
[System.Serializable]
public class SettingData
{
    // ミュート
    public bool Mute = false;

    // BGM音量（1～100）
    public int BGM = 50;

    // SE音量（1～100）
    public int SE = 50;

    // 文字送り（1～5）
    public int TextSpeed = 3;

}