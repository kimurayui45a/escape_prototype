using UnityEngine;

public class CameraEvent : MonoBehaviour
{
    // カメラのトランスフォームの見渡す場合
    //[SerializeField]
    //Transform eventCamera;

    // カメラごと渡す場合
    [SerializeField]
    Camera eventCamera;

    [SerializeField]
    ColliderEvent colliderEvent;

    void Start()
    {
        colliderEvent.OnEnter.AddListener(() =>
        {
            CameraManager.Instance.SetCamera(eventCamera);
        });

        colliderEvent.OnExit.AddListener(() =>
        {
            CameraManager.Instance.ResetCamera();
        });
    }
}
