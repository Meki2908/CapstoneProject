using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Lobby overlay trong scene gameplay. Trước đây dùng NGO — giờ ẩn lobby ngay (chờ lobby Fusion sau).
/// </summary>
public class NetworkLobbyManager : MonoBehaviour
{
    public static NetworkLobbyManager Instance { get; private set; }

    [SerializeField] private GameObject lobbyPanel;

    /// <summary>Luôn true sau Start — không chờ NGO.</summary>
    public bool GameStarted { get; private set; } = true;

    public event Action OnGameStarted;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (lobbyPanel != null)
            lobbyPanel.SetActive(false);
        GameStarted = true;
        OnGameStarted?.Invoke();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
