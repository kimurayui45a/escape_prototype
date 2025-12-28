using UnityEngine;


/// <summary>
/// 各アイテムを管理するSO
/// </summary>
[CreateAssetMenu(menuName = "Master/GameItem SO")]
public class GameItemSO : ScriptableObject
{
    // アイテムID
    // 共通アイテム）CO0001
    // 特殊アイテム）EX0001
    [Header("アイテムID")]
    public string GameItemId;

    // アイテム名
    [Header("アイテム名")]
    public string GameItemName;

    // アイテムイメージ
    [Header("アイテムイメージ")]
    public Sprite GameItemImage;

    // アイテム入手ヒント
    [Header("アイテム入手ヒント")]
    public string GameItemHintText;

    // 未所持アイテムイメージ
    [Header("未所持イメージ")]
    public Sprite GameItemNoImage;

}