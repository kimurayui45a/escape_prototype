using UnityEngine;

public class Manager : SingletonMonoBehaviour<Manager>
{
    [SerializeField]
    FadeManager fadeManager;

    [SerializeField]
    SaveTest saveTest;

    // 一意にしたいマネージャーを記述する
    public FadeManager FadeManager => fadeManager;
    public SaveTest SaveTest => saveTest;
}
