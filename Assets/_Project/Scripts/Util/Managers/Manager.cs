using UnityEngine;

public class Manager : SingletonMonoBehaviour<Manager>
{
    [SerializeField]
    FadeManager fadeManager;

    [SerializeField]
    SaveTest saveTest;

    [SerializeField]
    PlayerState playerState;

    // 一意にしたいマネージャーを記述する
    public FadeManager FadeManager => fadeManager;
    public SaveTest SaveTest => saveTest;
    public PlayerState PlayerState => playerState;
}
