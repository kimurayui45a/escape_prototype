using UnityEngine;

public class Manager : SingletonMonoBehaviour<Manager>
{
    [SerializeField]
    FadeManager fadeManager;

    [SerializeField]
    SaveTest saveTest;

    [SerializeField]
    GameSessionManager gameSessionManager;

    //[SerializeField]
    //PlaySaveData playSaveData;

    // 一意にしたいマネージャーを記述する
    public FadeManager FadeManager => fadeManager;
    public SaveTest SaveTest => saveTest;
    public GameSessionManager GameSessionManager => gameSessionManager;

    //public PlaySaveData PlaySaveData => playSaveData;

}
