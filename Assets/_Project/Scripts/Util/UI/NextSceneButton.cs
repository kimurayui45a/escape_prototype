using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FadeManager を使ってシーン遷移を行うボタン制御。
/// - クリックで「暗転 → Now Loading 表示 → 非同期ロード → シーン切替 → 自動明転」
///   （※ロード処理は FadeManager.LoadSceneWithFade が担当）
/// - このスクリプト側ではフェードアウトを開始しない（FadeManager.OnSceneLoaded に一任）
/// </summary>
public class NextSceneButton : MonoBehaviour
{
    /// <summary>
    /// 次のシーンへ進むためのボタン。
    /// </summary>
    [SerializeField] Button buttonNextScene;

    /// <summary>
    /// 遷移先のシーン名（Build Settings に登録済みの名前）。
    /// </summary>
    [SerializeField] string titleScene;

    void Start()
    {
        // 参照未設定の事故を早期に分かるようにする（実行継続はする）
        if (buttonNextScene == null)
        {
            Debug.LogError("[NextSceneButton] buttonNextScene is not assigned.");
            return;
        }

        if (string.IsNullOrEmpty(titleScene))
        {
            Debug.LogError("[NextSceneButton] titleScene is empty.");
            return;
        }

        // クリックイベント登録
        buttonNextScene.onClick.AddListener(OnClickNext);

        // 初期状態は押せる（フェード状態に応じた押下制御は FadeManager 側で統一する方が安定）
        buttonNextScene.interactable = true;
    }

    void OnDestroy()
    {
        // 破棄時に購読解除（シーン遷移のテストでオブジェクトが作り直されることがあるため）
        if (buttonNextScene != null)
        {
            buttonNextScene.onClick.RemoveListener(OnClickNext);
        }
    }

    /// <summary>
    /// ボタンクリック時：FadeManager にシーン遷移一式を委譲する。
    /// </summary>
    void OnClickNext()
    {
        // 多重クリック防止
        buttonNextScene.interactable = false;

        // FadeManager が無い場合の保険
        if (FadeManager.Instance == null)
        {
            Debug.LogError("[NextSceneButton] FadeManager.Instance is null. Ensure FadeManager exists in the scene.");
            buttonNextScene.interactable = true;
            return;
        }

        // 暗転～ロード～シーン切替を FadeManager 側で実行
        FadeManager.Instance.LoadSceneWithFade(titleScene);

        // 注意：
        // ここで interactable を戻さない。
        // シーンが切り替わればこのボタン自体が破棄される想定のため。
        // （同一シーン内で使い回すなら、FadeManager 側の完了通知を作って戻す設計にする）
    }
}
