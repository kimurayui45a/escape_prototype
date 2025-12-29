
//using System.Collections.Generic;
//using System;

//public sealed class PlayerController
//{
//    private readonly SaveData saveData;
//    internal PlayerController(SaveData saveData) => this.saveData = saveData;


//    // プレイヤー状態更新（PlayerStateManagerとPlayerStateDataSOのメソッドと調整する必要あり）
//    // public void SetPlayerState(string stateId)
//    // {
//    //     // saveData.EnsureWorking();
//    //     saveData.WorkingPlayGameData.PlayerParameter.PlayerStateId = stateId ?? "state_plant";
//    //     // 必要ならここで SaveUnsaved() まで呼ぶ（運用方針次第）
//    // }

//    // 快値更新
//    public void SetPleasure(int value)
//    {
//        // saveData.EnsureWorking();
//        saveData.WorkingPlayGameData.PlayerParameter.PleasureValue = value;
//    }

//    // 不快値更新
//    public void SetUnpleasant(int value)
//    {
//        // saveData.EnsureWorking();
//        saveData.WorkingPlayGameData.PlayerParameter.UnPleasantValue = value;
//    }

//    // 終了時にいたシーンの更新（SceneDataManagerとSceneDataSOのメソッドと調整する必要あり）
//    // public void SetLastScene(string sceneId)
//    // {
//    //     // saveData.EnsureWorking();
//    //     saveData.WorkingPlayGameData.LastSceneId = sceneId ?? "";
//    // }


//    // 最終セーブ日時（使う？？）
//    public void SetLastSaveTime()
//    {
//        // saveData.EnsureWorking();
//        var now = TimeUtil.NowUtcUnixSeconds();
//        saveData.WorkingPlayGameData.LastPlaySaveTime = now;
//    }


//    // ---- コレクション更新（例） ----

//    public void AddClearEvent(string eventId)
//    {
//        // saveData.EnsureWorking();

//        // 配列は扱いにくいので、内部では List 化してから戻すのが安全
//        var src = saveData.WorkingPlayGameData.PlayerParameter.ClearEvent ?? Array.Empty<string>();
//        if (Array.IndexOf(src, eventId) >= 0) return; // 重複防止（必要なら）

//        var list = new List<string>(src) { eventId };
//        saveData.WorkingPlayGameData.PlayerParameter.ClearEvent = list.ToArray();

//        // saveData.MarkUnsavedDirty();
//    }

//    public void AddOwnedItem(OwnedItemEntry item)
//    {
//        // saveData.EnsureWorking();

//        var list = saveData.WorkingPlayGameData.PlayerParameter.OwnedItemList;
//        list.Add(item);

//        // saveData.MarkUnsavedDirty();
//    }

//    public void RemoveOwnedItem(Predicate<OwnedItemEntry> predicate)
//    {
//        // saveData.EnsureWorking();

//        var list = saveData.WorkingPlayGameData.PlayerParameter.OwnedItemList;
//        list.RemoveAll(predicate);

//        // saveData.MarkUnsavedDirty();
//    }
//}

