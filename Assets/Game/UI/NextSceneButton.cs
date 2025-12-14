using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;   // ※このサンプルでは使っていないので、不要であれば削除してOK
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// シングルトンな FadeManager を使って、
/// ・ボタンから次のシーンへフェードイン付きで遷移する
/// ・シーン開始時にフェードアウトしてからボタンを押せるようにする
/// といった動きをテストするためのクラス。
/// </summary>
public class NextSceneButton : MonoBehaviour
{
    /// <summary>
    /// 次のシーンへ進むためのボタン。
    /// インスペクタで Button コンポーネントを持つオブジェクトを割り当てる。
    /// </summary>
    [SerializeField]
    Button buttonNextScene;

    /// <summary>
    /// 遷移先のシーン名。
    /// Build Settings に登録されているシーン名を設定しておく。
    /// </summary>
    [SerializeField]
    string TitleScene;

    // Start is called before the first frame update
    void Start()
    {
        // ボタンにクリックイベントを登録する
        buttonNextScene.onClick.AddListener(() =>
        {
            // 1. フェードイン（画面を暗くする）を 1.5 秒かけて再生
            // 2. フェードイン完了後に指定したシーンへ遷移
            FadeManager.Instance.PlayFadeIn(1.5f, () =>
            {
                // シーンを即時切り替え（同期ロード）
                SceneManager.LoadScene(TitleScene);

                // 非同期でシーンを読み込みたい場合はこちらを使用
                // SceneManager.LoadSceneAsync(nextSceneName);
            });
        });

        // シーン開始時点で、すでにフェードイン状態で暗い画面になっているかどうかを確認
        if (FadeManager.Instance.IsFadeIn)
        {
            // フェードイン中（画面が暗い状態）なら、
            // 次シーンへのボタンは一時的に押せないようにしておく
            buttonNextScene.interactable = false;

            // フェードアウト（画面を明るく戻す）を 1.5 秒かけて再生
            // フェードアウト完了後にボタンを再度押せるようにする
            FadeManager.Instance.PlayFadeOut(1.5f, () =>
            {
                buttonNextScene.interactable = true;
            });
        }
        else
        {
            // 既にフェードアウト済み（画面が見えている）なら、
            // 最初からボタンを押せる状態にしておく
            buttonNextScene.interactable = true;
        }
    }
}
