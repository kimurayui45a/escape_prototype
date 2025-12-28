using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 各シーンの UI に付ける
/// Save ボタンを、その時点で存在する SaveTest に紐づける
/// （InspectorのOnClickにSaveTestを直接入れない）
/// </summary>
public class SaveButton : MonoBehaviour
{
    [SerializeField] Button saveButton;

 
    private void Start()
    {
        if (saveButton == null) return;
        saveButton.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        Manager.Instance.SaveTest.Save();

        // 多重発火確認用
        Debug.Log($"Saveボタンクリック frame={Time.frameCount} id={GetInstanceID()}");
    }

    // ---万が一多重にリスナーが登録されていたり、発火していたら下記を使う---
    //private void OnEnable()
    //{
    //    if (saveButton == null) return;
    //    saveButton.onClick.RemoveListener(OnClick);
    //    saveButton.onClick.AddListener(OnClick);
    //}

    //private void OnDisable()
    //{
    //    if (saveButton == null) return;
    //    saveButton.onClick.RemoveListener(OnClick);
    //}

}
