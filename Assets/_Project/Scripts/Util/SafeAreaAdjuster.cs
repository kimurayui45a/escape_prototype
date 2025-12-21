using UnityEngine;

/// <summary>
/// 端末のセーフエリア（ノッチ・ホームバーなどを除いた描画可能領域）に
/// この RectTransform を自動でフィットさせるコンポーネント。
/// Canvas 配下の最上位パネルなどにアタッチして使う想定。
/// </summary>
public class SafeAreaAdjuster : MonoBehaviour
{
    // セーフエリアを適用する対象の RectTransform
    RectTransform rectTransform;

    void Awake()
    {
        // この GameObject に付いている RectTransform を取得
        rectTransform = GetComponent<RectTransform>();

        // 起動時に一度セーフエリアを適用
        ApplySafeArea();
    }

    /// <summary>
    /// 端末のセーフエリア情報を取得し、それに合わせてアンカーを調整する。
    /// </summary>
    void ApplySafeArea()
    {
        // 端末のセーフエリア（ピクセル座標）を取得
        Rect safeArea = Screen.safeArea;

        // セーフエリア左下の座標をアンカー最小値の元にする（ピクセル単位）
        Vector2 anchorMin = safeArea.position;

        // セーフエリア右上の座標 = 左下 + サイズ（ピクセル単位）
        Vector2 anchorMax = safeArea.position + safeArea.size;

        // ピクセル座標を 0～1 のビューポート座標（アンカー用）に変換
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        // RectTransform のアンカーをセーフエリアの範囲に合わせて設定
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
    }
}
