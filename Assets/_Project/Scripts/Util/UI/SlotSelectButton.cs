using UnityEngine;
using UnityEngine.UI;

public class SlotSelectButton : MonoBehaviour
{
    [SerializeField] private int slotNumber = 1;

    [SerializeField] Button slotSelectButton;

    private void Start()
    {
        if (slotSelectButton == null) return;
        slotSelectButton.onClick.AddListener(OnClick);
    }

    public void OnClick()
    {

        var manager = Manager.Instance.GameSessionManager;

        manager.SelectSlot(slotNumber);

        Debug.Log($"[UI] SelectedSlot => {manager.SelectedSlot}");
    }
}
