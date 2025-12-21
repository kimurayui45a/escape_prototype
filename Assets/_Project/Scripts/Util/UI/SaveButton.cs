using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ★ 各シーンの UI に付ける
/// Save ボタンを、その時点で存在する SaveTest に紐づける
/// （InspectorのOnClickにSaveTestを直接入れない）
/// </summary>
public class SaveButton : MonoBehaviour
{
    [SerializeField] Button saveButton;

    SaveTest saveTest; // 実行時に取得する

    void Start()
    {
        // シーン上から見つける（SaveTestをDontDestroyで常駐させるなら、常に取れる）
        saveTest = FindFirstObjectByType<SaveTest>(); // Unity 2023+
        // 古い版なら FindObjectOfType<SaveTest>();

        if (saveTest == null)
        {
            Debug.LogError("[SaveUiBinder] SaveTest not found in scene.");
            return;
        }

        // ここで確実に紐づけ
        if (saveButton != null) saveButton.onClick.AddListener(saveTest.Save);
    }

    void OnDestroy()
    {
        // 解除（多重登録事故防止）
        if (saveButton != null) saveButton.onClick.RemoveListener(saveTest.Save);
    }
}
