using UnityEngine;


/// <summary>
/// 各シーンを管理するSO
/// 管理対象はゲームシーンのみ（タイトル等のシーンは管理対象外）
/// </summary>
[CreateAssetMenu(menuName = "Master/Scene SO")]
public class SceneSO : ScriptableObject
{
    // シーンID
    // scene_plant
    // scene_insect
    // scene_bird
    // scene_animal
    [Header("シーンID")]
    public string SceneId;

    // シーン名
    // PlantScene
    // InsectScene
    // BirdScene
    // AnimalScene
    [Header("シーン名")]
    public string SceneName;

}