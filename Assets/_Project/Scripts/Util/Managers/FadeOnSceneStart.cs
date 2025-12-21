using UnityEngine;


/// <summary>
/// シーン開始時に「フェードイン中ならフェードアウトを再生する」ためのコンポーネント。
/// 例：前シーンからフェードイン状態で遷移してきた場合に、到着先で画面を開ける演出を行う。
/// </summary>
public class FadeOnSceneStart : MonoBehaviour
{

    /// <summary>
    /// フェードアウトにかける時間（秒）。
    /// Inspector から調整する想定。
    /// </summary>
    [SerializeField] float duration = 1.0f;


    /// <summary>
    /// Unityのライフサイクル：Start は最初のフレーム更新前に1回だけ呼ばれる。
    /// シーン開始時にフェード状態を確認し、必要ならフェードアウトを実行する。
    /// </summary>
    void Start()
    {
        if (FadeManager.Instance != null && FadeManager.Instance.IsFadeIn)
        {
            FadeManager.Instance.PlayFadeOut(duration, null);
        }
    }
}
