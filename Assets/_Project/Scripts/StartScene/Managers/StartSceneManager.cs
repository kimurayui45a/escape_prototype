using System.Collections;
using UnityEngine;


/// <summary>
/// スタートシーンマネージャー
/// ユーザー操作なしで、一定時間ロゴを表示した後にフェードして次のシーンへ遷移する。
/// 
/// ① 起動時、黒画面
/// ② ロゴを一定時間表示
/// ③ FadeManagerを呼び出し画面遷移
/// </summary>
public class StartSceneManager : MonoBehaviour
{
    [Header("遷移先シーン名")]
    [SerializeField]
    private string nextSceneName = "TitleScene";

    [Header("ロゴ表示時間（秒）")]
    [SerializeField]
    private float logoHoldSeconds = 1.5f;

    [Header("初期表示の黒画面")]
    [SerializeField]
    private CanvasGroup fadeOverlay;

    private void Awake()
    {
        // フェード用の CanvasGroup が未設定だと動作できないため明示的にエラー
        if (fadeOverlay == null)
        {
            Debug.LogError("[StartSceneManager] fadeOverlay (CanvasGroup) is not assigned.");
        }
    }

    private void Start()
    {
        // 自動遷移開始
        StartCoroutine(RunSequence());
    }

    // 表示管理メソッド
    private IEnumerator RunSequence()
    {
        var fadeManager = Manager.Instance.FadeManager;

        if (fadeOverlay == null) yield break;

        // ---- ローカル変数でフェード時間を管理（ここを変えるだけで調整可能）----
        float fadeSeconds = 0.75f;

        // 1) 起動直後は黒（Alpha=1）から開始してフェードイン（明るくする）
        fadeOverlay.alpha = 1f;
        fadeOverlay.blocksRaycasts = true; // ロゴ中にタップ/クリック等を拾わせない
        yield return FadeAlpha(1f, 0f, fadeSeconds);
        fadeOverlay.blocksRaycasts = false;

        // 2) ロゴ表示時間（ユーザー操作不要）
        if (logoHoldSeconds > 0f)
        {
            yield return new WaitForSeconds(logoHoldSeconds);
        }

        // 3) シーン遷移
        // FadeManager が無い場合の保険
        if (fadeManager == null)
        {
            Debug.LogError("[NextSceneButton] FadeManager がありません。");
            yield break;
        }

        fadeManager.LoadSceneWithFade(nextSceneName);

    }

    /// <summary>
    /// CanvasGroup の alpha を start -> end に時間をかけて変化させる。
    /// </summary>
    /// 

    // フェード調整メソッド
    // （起動後の黒画面のアルファ値の調整）
    private IEnumerator FadeAlpha(float start, float end, float durationSeconds)
    {
        // duration が 0 以下なら即時反映
        if (durationSeconds <= 0f)
        {
            fadeOverlay.alpha = end;
            yield break;
        }

        float elapsed = 0f;
        fadeOverlay.alpha = start;

        while (elapsed < durationSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / durationSeconds);
            fadeOverlay.alpha = Mathf.Lerp(start, end, t);
            yield return null;
        }

        fadeOverlay.alpha = end;
    }
}
