using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// Trigger コライダーへの侵入／退出を検知して UnityEvent を発火するコンポーネント。
/// - layerMask に含まれるレイヤーの Collider のみ対象
/// - Inspector からイベントを登録して、コードを書かずに反応を差し込める
/// </summary>
public class ColliderEvent : MonoBehaviour
{
    /// <summary>
    /// 対象とするレイヤーのマスク。
    /// ここに含まれないレイヤーの Collider は無視する。
    /// </summary>
    [SerializeField]
    LayerMask layerMask;

    /// <summary>
    /// Trigger に入ってきたときに発火するイベント。
    /// 外部からは「読み取り専用（代入不可）」で公開し、Invoke はこのクラス内部でのみ行う。
    /// Inspector から直接登録したい場合は [SerializeField] UnityEvent にする設計もある。
    /// </summary>
    public UnityEvent OnEnter { get; private set; } = new UnityEvent();

    /// <summary>
    /// Trigger から出ていったときに発火するイベント。
    /// </summary>
    public UnityEvent OnExit { get; private set; } = new UnityEvent();

    /// <summary>
    /// Unityコールバック：Trigger に他の Collider が侵入したときに呼ばれる。
    /// 注意：この GameObject 側の Collider は "Is Trigger" が有効である必要がある。
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        // other のレイヤーが layerMask に含まれているか判定する。
        // layerMask.value はビット列。 (1 << layer) で対象レイヤーのビットを立て、AND して 0 なら含まれていない。
        if ((layerMask.value & (1 << other.gameObject.layer)) == 0)
            return;

        // 侵入イベント発火（登録済みのコールバックを実行）
        OnEnter.Invoke();
    }

    /// <summary>
    /// Unityコールバック：Trigger から他の Collider が退出したときに呼ばれる。
    /// </summary>
    void OnTriggerExit(Collider other)
    {
        // 対象レイヤー以外は無視
        if ((layerMask.value & (1 << other.gameObject.layer)) == 0)
            return;
        // 退出イベント発火
        OnExit.Invoke();
    }
}
