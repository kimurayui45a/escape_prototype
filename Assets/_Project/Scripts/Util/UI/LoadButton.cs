using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 各シーンの UI に付ける
/// Save/Load ボタンを、その時点で存在する SaveTest に紐づける
/// （InspectorのOnClickにSaveTestを直接入れない）
/// </summary>
public class LoadButton : MonoBehaviour
{
    [SerializeField] Button loadButton;

    private void Start()
    {
        if (loadButton == null) return;
        loadButton.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        Manager.Instance.SaveTest.Load();

        // 多重発火確認用
        Debug.Log($"Loadボタンクリック frame={Time.frameCount} id={GetInstanceID()}");
    }


    // ---万が一多重にリスナーが登録されていたり、発火していたら下記を使う---
    //private void OnEnable()
    //{
    //    if (loadButton == null) return;
    //    loadButton.onClick.RemoveListener(OnClick);
    //    loadButton.onClick.AddListener(OnClick);
    //}

    //private void OnDisable()
    //{
    //    if (loadButton == null) return;
    //    loadButton.onClick.RemoveListener(OnClick);
    //}

}
