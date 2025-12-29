using System;
using System.Text;
using UnityEngine;

public static class BinaryDataManager
{
    /// <summary>
    /// T -> UTF8(JSON) bytes
    /// </summary>
    public static byte[] ToBytes<T>(T data)
    {
        // JsonUtility は参照型 null を渡すと例外になり得るので最低限のガード
        if (data == null)
        {
            // 読み戻し可能な最小JSON（型によっては完全復元できないが、落ちるよりはマシ）
            return Encoding.UTF8.GetBytes("{}");
        }

        var json = JsonUtility.ToJson(data, false);
        return Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// UTF8(JSON) bytes -> T
    /// </summary>
    public static bool TryFromBytes<T>(byte[] bytes, out T result) where T : new()
    {
        result = new T();

        if (bytes == null || bytes.Length == 0) return false;

        try
        {
            var json = Encoding.UTF8.GetString(bytes);
            var obj = JsonUtility.FromJson<T>(json);
            if (obj == null) return false;

            result = obj;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[BinaryDataManager] Deserialize failed: {ex.Message}");
            return false;
        }
    }
}
