using UnityEngine;
using UnityEngine.UI;

public class TitleButton : MonoBehaviour
{
    [Header("遷移先シーン名")]
    [SerializeField]
    private string nextSceneName = "TitleScene";

    [SerializeField] Button titleButton;

    private void Start()
    {
        if (titleButton == null) return;
        titleButton.onClick.AddListener(OnClick);
    }

    public void OnClick()
    {
        var manager = Manager.Instance;

        manager.GameSessionManager.ClearSlot();

        Debug.Log($"[UI] SelectedSlot => {manager.GameSessionManager.SelectedSlot}");

        // 例：タイトルへ遷移
        manager.FadeManager.LoadSceneWithFade(nextSceneName);
    }
}
