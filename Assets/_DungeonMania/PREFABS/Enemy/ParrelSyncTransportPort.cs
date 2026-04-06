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
        // Port 0 trong scene YAML khiến bind/offset ParrelSync sai — chuẩn hóa giống Testboss.
        if (basePort == 0)
        {
            const ushort defaultPort = 7777;
            utp.SetConnectionData(cd.Address, defaultPort, cd.ServerListenAddress);
            cd = utp.ConnectionData;
            basePort = cd.Port;
        }
        ushort newPort = (ushort)(basePort + cloneIndex + 1);

        if (newPort == basePort) return;

        utp.SetConnectionData(cd.Address, newPort, cd.ServerListenAddress);
        Debug.Log($"[ParrelSync] Clone index {cloneIndex}: UnityTransport port {basePort} → {newPort} (tránh trùng cổng với editor gốc).");
    }

    /// <summary>
    /// Cổng khi <see cref="NetworkManager.StartClient"/>: đọc từ UnityTransport, 0→7777.
    /// Không áp offset clone — offset chỉ dùng khi bind host trên clone; client join Editor (7777) sẽ sai nếu áp offset.
    /// Join host clone cùng máy (vd. 7778): đặt <c>MultiplayerManager.connectPort</c> &gt; 0.
    /// </summary>
    public static ushort GetClientConnectPortWithoutCloneOffset(UnityTransport utp)
    {
        if (utp == null) return 7777;
        var cd = utp.ConnectionData;
        ushort p = cd.Port;
        if (p == 0)
            p = 7777;
        return p;
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
