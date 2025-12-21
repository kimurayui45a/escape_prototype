

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;


/// <summary>
/// 画面全体のフェードイン／フェードアウトを管理する（想定：シングルトン）
/// - fadeImage のアルファを補間して暗転／明転する
/// - 暗転中のみ NowLoading を表示する
/// - シーンロード完了イベントを監視し、暗転状態で次シーンに来たら自動で明転する
/// </summary>
public class FadeManager : SingletonMonoBehaviour<FadeManager>
{

    /// <summary>
    /// 画面全体を覆うフェード用 Image（黒など）
    /// アルファ値を変化させることで暗転／明転を表現する
    /// </summary>
    [SerializeField] Image fadeImage;

    /// <summary>
    /// ローディング中に表示する UI（暗転中にだけONにする想定）
    /// </summary>
    [SerializeField] GameObject nowLoading;


    // 時間設定をインスペクタで調整できるようにする
    [Header("Fade Settings")]
    [SerializeField] float defaultFadeInSeconds = 1.5f;
    [SerializeField] float defaultFadeOutSeconds = 1.5f;
    // Now Loading を最低この秒数だけは画面に出し続けるための下限時間
    [SerializeField] float minNowLoadingVisibleSeconds = 0.3f;

    /// <summary>
    /// 現在「暗転中（画面が暗い）」かどうか
    /// - FadeIn 完了で true
    /// - FadeOut 完了で false
    /// </summary>
    public bool IsFadeIn { get; private set; } = false;

    /// <summary>
    /// 現在走っているフェードコルーチンの参照
    /// 次のフェード開始時に止めることで多重実行を防ぐ
    /// </summary>
    Coroutine fadeCoroutine;

    // ロード連続呼び出し対策（多重ロード防止）
    Coroutine loadSceneCoroutine;

    /// <summary>
    /// 生成直後に呼ばれる初期化タイミング
    /// - sceneLoaded を購読して、シーン遷移後に自動で明転できるようにする
    /// </summary>
    void Awake()
    {

        //DontDestroyOnLoad(gameObject);

        // シーンロード完了を監視（次シーンに切り替わった瞬間を捕まえる）
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    /// 破棄時にイベント購読を解除する（メモリリーク／多重購読防止）
    /// DontDestroyOnLoad 前提でも、念のため入れておくのが安全
    /// </summary>
    void OnDestroy()
    {
        // DontDestroy の想定でも、念のため解除
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// シーンロード完了時に呼ばれるコールバック
    /// - 暗転状態（IsFadeIn==true）なら自動で明転（FadeOut）を開始する
    /// - 明転状態なら NowLoading を消しておく
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 暗転状態で次シーンに来たら自動で明転
        if (IsFadeIn)
        {
            // ここで 1.5 秒固定にしているが、必要なら可変にしたり定数化する
            PlayFadeOut(defaultFadeOutSeconds, null);
        }
        else
        {
            // 明転状態ならローディング表示は常に OFF
            if (nowLoading != null) nowLoading.SetActive(false);
        }
    }

    /// <summary>
    /// フェードイン → 非同期ロード（NowLoading表示）→ シーン切り替え
    /// シーン切り替え後のフェードアウトは OnSceneLoaded が担当する
    /// </summary>
    public void LoadSceneWithFade(string sceneName)
    {
        // 多重実行防止
        if (loadSceneCoroutine != null)
        {
            StopCoroutine(loadSceneCoroutine);
            loadSceneCoroutine = null;
        }

        loadSceneCoroutine = StartCoroutine(LoadSceneWithFadeCoroutine(sceneName));
    }

    IEnumerator LoadSceneWithFadeCoroutine(string sceneName)
    {
        // まず暗転（フェードイン）してからロードする
        bool fadeInDone = false;

        PlayFadeIn(defaultFadeInSeconds, () => fadeInDone = true);

        // フェードイン完了待ち
        while (!fadeInDone) yield return null;

        // ★ 重要：NowLoading をONにしたフレームを「1フレーム描画させる」
        // （これがないと、環境によってはONにして即ロード完了→切り替えで見えないことがある）
        yield return null;

        // 非同期ロード開始（ここからロード中もフレームが進むのでUIが描画される）
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);

        // ロードが完了しても、こちらが許可するまでシーンを切り替えない
        op.allowSceneActivation = false;

        // ：最低表示時間を保証するためのタイマー
        float elapsed = 0f;

        // progress は 0.9 で「ロード完了手前」まで行き、allowSceneActivation=true で切り替わる
        while (op.progress < 0.9f || elapsed < minNowLoadingVisibleSeconds)
        {
            elapsed += Time.unscaledDeltaTime; // TimeScale=0でも進む（必要なら）
            yield return null;
        }

        // ここでシーン切り替えを許可
        op.allowSceneActivation = true;

        // 切り替え完了まで待つ（OnSceneLoaded が発火して PlayFadeOut が走る）
        while (!op.isDone) yield return null;

        loadSceneCoroutine = null;
    }

    /// <summary>
    /// 明→暗（0→1）へフェードする
    /// - フェード完了後に IsFadeIn=true にし、NowLoading を ON にする
    /// - onEnd を呼んで次処理（LoadScene など）へつなぐ
    /// </summary>
    public void PlayFadeIn(float duration, System.Action onEnd)
    {
        StartFade(0, 1, duration, () =>
        {
            // 暗転完了
            IsFadeIn = true;

            // 暗転状態になったら NowLoading を表示
            if (nowLoading != null) nowLoading.SetActive(true);

            // 呼び出し元に完了通知
            onEnd?.Invoke();
        });
    }

    /// <summary>
    /// 暗→明（1→0）へフェードする
    /// - フェード完了後に IsFadeIn=false にし、NowLoading を OFF にする
    /// </summary>
    public void PlayFadeOut(float duration, System.Action onEnd)
    {
        StartFade(1, 0, duration, () =>
        {
            // 明転完了
            IsFadeIn = false;

            // 明転が終わったら NowLoading を消す（重要）
            if (nowLoading != null) nowLoading.SetActive(false);

            // 呼び出し元に完了通知
            onEnd?.Invoke();
        });
    }

    /// <summary>
    /// フェード開始の共通処理
    /// - すでにフェード中なら停止してから新しいフェードを開始する
    /// </summary>
    void StartFade(float startAlpha, float endAlpha, float duration, System.Action onEnd)
    {
        // 多重実行防止：前のコルーチンを止める
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        // フェードコルーチン開始
        fadeCoroutine = StartCoroutine(FadeCoroutine(startAlpha, endAlpha, duration, onEnd));
    }

    /// <summary>
    /// アルファ値を startAlpha → endAlpha へ duration 秒で補間する
    /// </summary>
    IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float duration, System.Action onEnd)
    {

        // フェードが始まる瞬間は一旦ローディングを消しておく
        // （暗転完了で表示したいので、開始時はOFF）
        if (nowLoading != null) nowLoading.SetActive(false);

        float time = 0f;

        // duration の間、フレームごとにアルファ更新
        while (time < duration)
        {
            time += Time.deltaTime;

            // duration=0対策を明示
            float t = (duration <= 0f) ? 1f : Mathf.Clamp01(time / duration);

            // 線形補間でアルファを計算
            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);

            // fadeImage のアルファのみ変更
            if (fadeImage != null)
            {
                var c = fadeImage.color;
                c.a = alpha;
                fadeImage.color = c;
            }

            // 次フレームへ
            yield return null;
        }

        // 最終値を誤差なくきっちり反映
        if (fadeImage != null)
        {
            var final = fadeImage.color;
            final.a = endAlpha;
            fadeImage.color = final;
        }

        // コルーチン参照をクリア（次回 StartFade で「フェード中判定」に使える）
        fadeCoroutine = null;

        // 完了コールバック
        onEnd?.Invoke();
    }
}






//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;

///// <summary>
///// 画面全体のフェードイン・フェードアウトを管理するシングルトン。
///// ・フェード用のImageのアルファ値を時間経過で変化させる
///// ・フェード中／完了後に「Now Loading」表示のON/OFFを切り替える
///// ・シーンをまたいでも生き残る(DontDestroyOnLoad)
///// </summary>
//public class FadeManager : SingletonMonoBehaviour<FadeManager>
//{
//    /// <summary>
//    /// 画面全体を覆うフェード用の Image。
//    /// 黒(または任意の色)＋アルファを変化させることでフェード演出を行う。
//    /// </summary>
//    [SerializeField]
//    Image fadeImage;

//    /// <summary>
//    /// ローディング中に表示する「Now Loading」オブジェクト。
//    /// フェードイン完了時に ON、フェードアウト完了時に OFF にする想定。
//    /// </summary>
//    [SerializeField]
//    GameObject nowLoading;

//    /// <summary>
//    /// 現在、フェードイン状態（画面が暗い状態）かどうか。
//    /// endAlpha > 0 で true、0 で false に更新される。
//    /// 外部から「今は暗転中か？」を調べるのに使用する。
//    /// </summary>
//    public bool IsFadeIn { get; set; } = false;

//    /// <summary>
//    /// 現在再生中のフェード用コルーチン。
//    /// フェード開始時に保持し、次のフェード開始時に一旦止めるために使う。
//    /// </summary>
//    Coroutine fadeCoroutine;

//    //private void Start()
//    //{
//    //    // この FadeManager をシーン切り替え時に破棄しないようにする。
//    //    // （複数シーンをまたいで同じフェードマネージャを使うため）
//    //    DontDestroyOnLoad(this);
//    //    // ※ 一般的には DontDestroyOnLoad(gameObject); と書くことが多い。
//    //}

//    /// <summary>
//    /// 画面を「明 → 暗」へフェードインさせる。
//    /// duration の時間をかけて 0 → 1 にアルファ値を変化させる。
//    /// フェード完了後に isFadeEnd コールバックを呼び出す。
//    /// </summary>
//    /// <param name="duration">フェードにかける秒数</param>
//    /// <param name="isFadeEnd">フェード完了時に呼ばれるコールバック</param>
//    public void PlayFadeIn(float duration, System.Action isFadeEnd)
//    {
//        // すでにフェード処理中なら一度止める（多重再生を防ぐ）
//        if (fadeCoroutine != null)
//        {
//            StopCoroutine(fadeCoroutine);
//            fadeCoroutine = null;
//        }

//        // アルファ値 0 → 1 へ変化させるコルーチンを開始
//        fadeCoroutine = StartCoroutine(FadeCoroutine(0, 1, duration, isFadeEnd));
//    }

//    /// <summary>
//    /// 画面を「暗 → 明」へフェードアウトさせる。
//    /// duration の時間をかけて 1 → 0 にアルファ値を変化させる。
//    /// フェード完了後に isFadeEnd コールバックを呼び出す。
//    /// </summary>
//    /// <param name="duration">フェードにかける秒数</param>
//    /// <param name="isFadeEnd">フェード完了時に呼ばれるコールバック</param>
//    public void PlayFadeOut(float duration, System.Action isFadeEnd)
//    {
//        // すでにフェード処理中なら一度止める（多重再生を防ぐ）
//        if (fadeCoroutine != null)
//        {
//            StopCoroutine(fadeCoroutine);
//            fadeCoroutine = null;
//        }

//        // アルファ値 1 → 0 へ変化させるコルーチンを開始
//        fadeCoroutine = StartCoroutine(FadeCoroutine(1, 0, duration, isFadeEnd));
//    }

//    /// <summary>
//    /// アルファ値を startAlpha から endAlpha へ、duration 秒かけて補間するコルーチン。
//    /// フェード中は fadeImage.color.a を書き換え続ける。
//    /// 終了後に IsFadeIn を更新し、必要なら nowLoading を ON にしてからコールバックを呼ぶ。
//    /// </summary>
//    /// <param name="startAlpha">開始時のアルファ値</param>
//    /// <param name="endAlpha">終了時のアルファ値</param>
//    /// <param name="duration">変化にかける時間（秒）</param>
//    /// <param name="isFadeEnd">フェード完了時に呼ばれるコールバック</param>
//    IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float duration, System.Action isFadeEnd)
//    {
//        float time = 0;

//        // フェード処理開始時点ではローディング表示は一旦 OFF にしておく
//        nowLoading.SetActive(false);

//        // duration 秒に達するまでフレームごとにアルファ値を更新
//        while (time < duration)
//        {
//            time += Time.deltaTime;

//            // 線形補間でアルファ値を計算
//            var alpha = startAlpha + (endAlpha - startAlpha) * time / duration;

//            // Image の色を取得して、アルファのみ書き換える
//            var tempColor = fadeImage.color;
//            tempColor.a = alpha;
//            fadeImage.color = tempColor;

//            // 1フレーム待つ
//            yield return null;
//        }

//        // 最終的に endAlpha をきっちり適用（誤差対策）
//        {
//            var tempColor = fadeImage.color;
//            tempColor.a = endAlpha;
//            fadeImage.color = tempColor;
//        }

//        // コルーチン終了したのでハンドルをクリア
//        fadeCoroutine = null;

//        // endAlpha が 0 より大きければ「暗転中」とみなす
//        IsFadeIn = endAlpha > 0;

//        // 暗転状態になったときだけ NowLoading を ON にする
//        if (IsFadeIn)
//        {
//            nowLoading.SetActive(true);
//        }

//        // フェード完了コールバックを呼ぶ（nullチェック付き呼び出し）
//        isFadeEnd?.Invoke();
//    }
//}
