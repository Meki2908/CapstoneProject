using System;
using System.IO;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// ParrelSync clone chạy cùng máy với project gốc: cùng cổng UTP (mặc định 7777) sẽ bind thất bại.
/// Thư mục clone có dạng .../TênProject_clone_0 — tăng cổng theo chỉ số clone trước khi StartHost/StartServer.
/// </summary>
public static class ParrelSyncTransportPort
{
    const string CloneFolderMarker = "_clone_";

    public static void ApplyClonePortOffsetIfNeeded(NetworkManager networkManager)
    {
        if (networkManager == null) return;
        if (!TryGetParrelSyncCloneIndex(out int cloneIndex)) return;

        var utp = networkManager.GetComponent<UnityTransport>();
        if (utp == null) return;

        var cd = utp.ConnectionData;
        ushort basePort = cd.Port;
        ushort newPort = (ushort)(basePort + cloneIndex + 1);

        if (newPort == basePort) return;

        utp.SetConnectionData(cd.Address, newPort, cd.ServerListenAddress);
        Debug.Log($"[ParrelSync] Clone index {cloneIndex}: UnityTransport port {basePort} → {newPort} (tránh trùng cổng với editor gốc).");
    }

    static bool TryGetParrelSyncCloneIndex(out int cloneIndex)
    {
        cloneIndex = -1;
        try
        {
            string assets = Application.dataPath;
            if (string.IsNullOrEmpty(assets)) return false;

            string projectFolder = Path.GetDirectoryName(assets);
            if (string.IsNullOrEmpty(projectFolder)) return false;

            int idx = projectFolder.LastIndexOf(CloneFolderMarker, StringComparison.Ordinal);
            if (idx < 0) return false;

            string tail = projectFolder.Substring(idx + CloneFolderMarker.Length);
            int n = 0;
            while (n < tail.Length && char.IsDigit(tail[n])) n++;
            if (n == 0) return false;

            return int.TryParse(tail.Substring(0, n), out cloneIndex);
        }
        catch
        {
            return false;
        }
    }
}
