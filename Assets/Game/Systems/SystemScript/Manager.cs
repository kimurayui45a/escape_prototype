using UnityEngine;

public class Manager : SingletonMonoBehaviour<Manager>
{
    [SerializeField]
    FadeManager fadeManager;

    // 一意にしたいマネージャーを記述する
    public FadeManager FadeManager => fadeManager;
}
