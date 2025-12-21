using UnityEngine;

/// <summary>
/// カメラの切り替え／位置・回転の上書き／初期状態へのリセットを担当するマネージャ。
/// Singleton として全体から参照される想定。
/// </summary>
public class CameraManager : MonoBehaviour
{
    /// <summary>
    /// Singletonインスタンス（実体）。
    /// </summary>
    static CameraManager instance;

    /// <summary>
    /// Singletonアクセサ。
    /// - 未生成ならシーン内から検索して拾う。
    /// - 生成責務までは持たない（Factoryではない）。
    /// </summary>
    public static CameraManager Instance
    {
        get
        {
            // インスタンス参照が未設定なら、シーン内から1つ探してキャッシュする
            if (instance == null)
            {
                instance = FindAnyObjectByType<CameraManager>();
            }
            return instance;
        }
    }

    /// <summary>
    /// “基準となる”メインカメラ。
    /// ResetCamera() ではこのカメラに戻し、初期位置・回転へ復元する。
    /// </summary>
    [SerializeField]
    Camera mainCamera;

    /// <summary>
    /// 起動時に記録した mainCamera の初期位置。
    /// </summary>
    Vector3 position;

    /// <summary>
    /// 起動時に記録した mainCamera の初期回転。
    /// </summary>
    Quaternion rotation;

    /// <summary>
    /// 現在有効化されているカメラ参照。
    /// SetCamera(Camera) で切り替える対象。
    /// </summary>
    Camera currentCamera;

    /// <summary>
    /// Unityライフサイクル：Awake
    /// - Singleton重複を排除
    /// - mainCamera の初期状態（位置・回転）を保存
    /// - currentCamera を mainCamera に初期化
    /// </summary>
    void Awake()
    {
        // Singleton（同種のオブジェクトが複数存在したら、後から来た方を破棄）
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        // mainCamera の初期トランスフォームを保持（リセット用）
        position = mainCamera.transform.position;
        rotation = mainCamera.transform.rotation;

        // 現在カメラは mainCamera として扱う
        currentCamera = mainCamera;
    }

    /// <summary>
    /// カメラ状態を初期化する。
    /// - 現在カメラを一旦無効化
    /// - mainCamera を currentCamera として有効化
    /// - mainCamera の Transform を起動時の位置・回転へ戻す
    /// </summary>
    public void ResetCamera()
    {
        // 現在カメラを無効化（切り替え演出の最低限）
        currentCamera.enabled = false;

        // メインカメラへ戻す
        currentCamera = mainCamera;
        currentCamera.enabled = true;

        // メインカメラの位置・回転を初期値へ復元
        mainCamera.transform.position = position;
        mainCamera.transform.rotation = rotation;
    }

    /// <summary>
    /// mainCamera の Transform（位置・回転）だけを、指定Transformに合わせる。
    /// 注意：有効カメラの切り替えは行わない（enabled制御しない）。
    /// </summary>
    public void SetCamera(Transform cameraTransform)
    {
        mainCamera.transform.position = cameraTransform.position;
        mainCamera.transform.rotation = cameraTransform.rotation;
    }

    /// <summary>
    /// 有効カメラを切り替える（Cameraコンポーネント単位でのスイッチ）。
    /// - 現在カメラを無効化
    /// - 渡されたカメラを currentCamera として有効化
    /// </summary>
    public void SetCamera(Camera camera)
    {
        currentCamera.enabled = false;
        currentCamera = camera;
        currentCamera.enabled = true;
    }
}
