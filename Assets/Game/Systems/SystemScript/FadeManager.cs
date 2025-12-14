using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 画面全体のフェードイン・フェードアウトを管理するシングルトン。
/// ・フェード用のImageのアルファ値を時間経過で変化させる
/// ・フェード中／完了後に「Now Loading」表示のON/OFFを切り替える
/// ・シーンをまたいでも生き残る(DontDestroyOnLoad)
/// </summary>
public class FadeManager : SingletonMonoBehaviour<FadeManager>
{
    /// <summary>
    /// 画面全体を覆うフェード用の Image。
    /// 黒(または任意の色)＋アルファを変化させることでフェード演出を行う。
    /// </summary>
    [SerializeField]
    Image fadeImage;

    /// <summary>
    /// ローディング中に表示する「Now Loading」オブジェクト。
    /// フェードイン完了時に ON、フェードアウト完了時に OFF にする想定。
    /// </summary>
    [SerializeField]
    GameObject nowLoading;

    /// <summary>
    /// 現在、フェードイン状態（画面が暗い状態）かどうか。
    /// endAlpha > 0 で true、0 で false に更新される。
    /// 外部から「今は暗転中か？」を調べるのに使用する。
    /// </summary>
    public bool IsFadeIn { get; set; } = false;

    /// <summary>
    /// 現在再生中のフェード用コルーチン。
    /// フェード開始時に保持し、次のフェード開始時に一旦止めるために使う。
    /// </summary>
    Coroutine fadeCoroutine;

    //private void Start()
    //{
    //    // この FadeManager をシーン切り替え時に破棄しないようにする。
    //    // （複数シーンをまたいで同じフェードマネージャを使うため）
    //    DontDestroyOnLoad(this);
    //    // ※ 一般的には DontDestroyOnLoad(gameObject); と書くことが多い。
    //}

    /// <summary>
    /// 画面を「明 → 暗」へフェードインさせる。
    /// duration の時間をかけて 0 → 1 にアルファ値を変化させる。
    /// フェード完了後に isFadeEnd コールバックを呼び出す。
    /// </summary>
    /// <param name="duration">フェードにかける秒数</param>
    /// <param name="isFadeEnd">フェード完了時に呼ばれるコールバック</param>
    public void PlayFadeIn(float duration, System.Action isFadeEnd)
    {
        // すでにフェード処理中なら一度止める（多重再生を防ぐ）
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        // アルファ値 0 → 1 へ変化させるコルーチンを開始
        fadeCoroutine = StartCoroutine(FadeCoroutine(0, 1, duration, isFadeEnd));
    }

    /// <summary>
    /// 画面を「暗 → 明」へフェードアウトさせる。
    /// duration の時間をかけて 1 → 0 にアルファ値を変化させる。
    /// フェード完了後に isFadeEnd コールバックを呼び出す。
    /// </summary>
    /// <param name="duration">フェードにかける秒数</param>
    /// <param name="isFadeEnd">フェード完了時に呼ばれるコールバック</param>
    public void PlayFadeOut(float duration, System.Action isFadeEnd)
    {
        // すでにフェード処理中なら一度止める（多重再生を防ぐ）
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        // アルファ値 1 → 0 へ変化させるコルーチンを開始
        fadeCoroutine = StartCoroutine(FadeCoroutine(1, 0, duration, isFadeEnd));
    }

    /// <summary>
    /// アルファ値を startAlpha から endAlpha へ、duration 秒かけて補間するコルーチン。
    /// フェード中は fadeImage.color.a を書き換え続ける。
    /// 終了後に IsFadeIn を更新し、必要なら nowLoading を ON にしてからコールバックを呼ぶ。
    /// </summary>
    /// <param name="startAlpha">開始時のアルファ値</param>
    /// <param name="endAlpha">終了時のアルファ値</param>
    /// <param name="duration">変化にかける時間（秒）</param>
    /// <param name="isFadeEnd">フェード完了時に呼ばれるコールバック</param>
    IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float duration, System.Action isFadeEnd)
    {
        float time = 0;

        // フェード処理開始時点ではローディング表示は一旦 OFF にしておく
        nowLoading.SetActive(false);

        // duration 秒に達するまでフレームごとにアルファ値を更新
        while (time < duration)
        {
            time += Time.deltaTime;

            // 線形補間でアルファ値を計算
            var alpha = startAlpha + (endAlpha - startAlpha) * time / duration;

            // Image の色を取得して、アルファのみ書き換える
            var tempColor = fadeImage.color;
            tempColor.a = alpha;
            fadeImage.color = tempColor;

            // 1フレーム待つ
            yield return null;
        }

        // 最終的に endAlpha をきっちり適用（誤差対策）
        {
            var tempColor = fadeImage.color;
            tempColor.a = endAlpha;
            fadeImage.color = tempColor;
        }

        // コルーチン終了したのでハンドルをクリア
        fadeCoroutine = null;

        // endAlpha が 0 より大きければ「暗転中」とみなす
        IsFadeIn = endAlpha > 0;

        // 暗転状態になったときだけ NowLoading を ON にする
        if (IsFadeIn)
        {
            nowLoading.SetActive(true);
        }

        // フェード完了コールバックを呼ぶ（nullチェック付き呼び出し）
        isFadeEnd?.Invoke();
    }
}
