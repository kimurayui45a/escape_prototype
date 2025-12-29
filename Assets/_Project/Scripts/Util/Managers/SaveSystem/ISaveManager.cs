//using System;
//using System.IO;
//using UnityEngine;


//// SaveManagerのインターフェース
//public interface ISaveManager
//{
//    void Save<T>(string fileName, T data);
//    bool TryLoad<T>(string fileName, out T data) where T : new();
//    bool Exists(string fileName);
//    void Delete(string fileName);
//}