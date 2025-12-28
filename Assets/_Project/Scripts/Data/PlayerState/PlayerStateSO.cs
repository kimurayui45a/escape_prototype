using UnityEngine;


/// <summary>
/// 各プレイヤー状態を管理するSO
/// </summary>
[CreateAssetMenu(menuName = "Master/PlayerState SO")]
public class PlayerStateSO : ScriptableObject
{
    // プレイヤー状態ID
    // state_plant
    // state_insect
    // state_bird
    // state_animal
    [Header("プレイヤー状態ID")]
    public string PlayerStateId;

    // プレイヤー状態名
    // Plant
    // Insect
    // Bird
    // Animal
    [Header("プレイヤー状態名")]
    public string PlayerStateName; 

}