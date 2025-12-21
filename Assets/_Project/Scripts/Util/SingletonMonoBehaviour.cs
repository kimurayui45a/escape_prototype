using UnityEngine;

/// <summary>
/// 任意の MonoBehaviour を「シーンをまたいでひとつだけ」保持したいときの
/// 汎用シングルトン基底クラス。
/// 継承側で `class MyMgr : SingletonMonoBehaviour<MyMgr>` のように使う。
/// </summary>
public class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    // 現在の唯一インスタンス（Domain Reload で静的変数は初期化される想定）
    static T instance;

    // アプリ終了中のフラグ（終了処理中に新規生成を防ぐ）
    static bool isQuitting = false;

    /// <summary>
    /// 外部からの参照口。必要ならここで自動生成も行う。
    /// 注意：Editor停止直後など isQuitting 中は null を返す。
    /// </summary>
    public static T Instance => GetOrCreateInstance();

    /// <summary>
    /// 既存インスタンス取得 or 生成（なければ新規 GameObject を作って AddComponent）
    /// </summary>
    static T GetOrCreateInstance()
    {
        if (isQuitting)
        {
            // アプリ終了処理中は生成しない（ログスパムや残骸生成の防止）
            return null;
        }

        if (instance == null)
        {
            // シーン上から既存を探す（Unity 2022+ の FindFirstObjectByType）
            instance = FindFirstObjectByType<T>();

            if (instance == null)
            {
                // 見つからなければ自前で生成（ヒエラルキー直下）
                var obj = new GameObject(typeof(T).ToString());
                instance = obj.AddComponent<T>();

                // シーン遷移で破棄されないように(これのおかげで消えない)
                DontDestroyOnLoad(obj);
            }
        }
        return instance;
    }

    /// <summary>
    /// シングルトン生成時に一度だけ呼ばれるフック。
    /// 継承先で初期化をまとめたい場合に override する。
    /// （Awake/Start より先に・安全に行いたい処理向け）
    /// </summary>
    protected virtual void OnCreateSingleton() { }

    /// <summary>
    /// シングルトン破棄時のフック。継承先でリソース解放等を行う。
    /// onDestroyを直接使用するとinstanceされたもの以外でも処理はいるためシングルトンから呼び出す
    /// </summary>
    protected virtual void OnDestroySingleton() { }

    void Awake()
    {
        // すでに別インスタンスが存在する場合は自分を破棄（重複生成防止）
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            // 初回の確定登録
            instance = this as T;

            //親が付いていたら外してルート化（見た目は維持）
            if (transform.parent != null)
                transform.SetParent(null, true);

            // シーン遷移でも残す
            DontDestroyOnLoad(instance.gameObject);

            // 生成フック（継承先での初期化ポイント）
            OnCreateSingleton();
        }
    }

    void OnDestroy()
    {
        // 自分が現インスタンスなら破棄フック→参照をクリア
        if (instance == this as T)
        {
            OnDestroySingleton();
            instance = null;
        }
    }

    void OnApplicationQuit()
    {
        // 終了中フラグを立てて、終了処理中のインスタンス自動生成を抑止
        isQuitting = true;
    }
}
