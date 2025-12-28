using UnityEngine;
using UnityEngine.EventSystems;


/// <summary>
/// スタートシーンマネージャー
/// タイトル画面で「どこかをクリック/タップしたら次のシーンへ遷移」する制御。
/// 
/// UIのImgにアタッチ
/// - 透明な全画面Panel（Imageなど）に付けて、IPointerClickHandlerで拾う想定
/// - 二重押下防止あり
/// - 遷移は FadeManager.LoadSceneWithFade を利用
/// </summary>
public class TitleSceneManager : MonoBehaviour, IPointerClickHandler
{
    [Header("遷移先シーン名（例: MenuScene）")]
    [SerializeField] private string nextSceneName = "MenuScene";

    // 入力を有効にするかのフラグ
    //　trueで遷移発生
    //　falseで入力無効
    [Header("入力を有効にするか（デバッグ用）")]
    [SerializeField] private bool acceptInput = true;

    // 連打/多重遷移防止
    private bool hasRequestedTransition = false;

    /// <summary>
    /// 画面クリック/タップで呼ばれる（EventSystem経由）。
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!acceptInput) return;
        RequestTransition();
    }

    /// <summary>
    /// キーボードでも進めたい場合の保険（任意）。
    /// 例えばSpace/Enterで開始など。
    /// </summary>
    private void Update()
    {
        if (!acceptInput) return;
        if (hasRequestedTransition) return;

        // 任意：PC操作の保険
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            RequestTransition();
        }
    }

    /// <summary>
    /// メニュー画面遷移メソッド
    /// 遷移リクエスト（1回だけ通す）。
    /// </summary>
    private void RequestTransition()
    {
        if (hasRequestedTransition) return;
        hasRequestedTransition = true;

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("[TitleSceneManager] nextSceneName is empty.", this);
            hasRequestedTransition = false; // 設定ミス時は再試行できるよう戻す
            return;
        }

        // Manager / FadeManager が存在する前提の呼び出し
        if (Manager.Instance == null)
        {
            Debug.LogError("[TitleSceneManager] Manager.Instance is null. Ensure Manager exists and is initialized.", this);
            hasRequestedTransition = false;
            return;
        }

        var fadeManager = Manager.Instance.FadeManager;
        if (fadeManager == null)
        {
            Debug.LogError("[TitleSceneManager] FadeManager is null on Manager.Instance.", this);
            hasRequestedTransition = false;
            return;
        }

        fadeManager.LoadSceneWithFade(nextSceneName);
    }
}
