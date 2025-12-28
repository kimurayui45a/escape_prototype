using UnityEngine;


/// <summary>
/// 各イベントを管理するSO
/// 管理対象はゲームイベントのみ
/// </summary>
[CreateAssetMenu(menuName = "Master/Event SO")]
public class EventSO : ScriptableObject
{
    // イベントID
    // 形式）Event001
    [Header("イベントID")]
    public string EventId;

    // イベント名
    [Header("イベント名")]
    public string EventName;

    // イベント所属シーン（SceneSO.SceneIdと同じものを登録する）
    [Header("イベント所属シーン")]
    public string EventSceneId;

    // アンロック条件リスト
    [Header("アンロック条件リスト")]
    public EventSO[] UnlockEventList;

}