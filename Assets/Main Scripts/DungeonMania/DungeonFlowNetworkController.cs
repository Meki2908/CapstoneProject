using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class DungeonFlowNetworkController : NetworkBehaviour
{
    public enum DungeonFlowPhase
    {
        None = 0,
        WaitingPeers = 1,
        Intro = 2,
        WaveBanner = 3,
        Countdown = 4,
        Combat = 5,
        BetweenWaves = 6,
        Victory = 7,
        Defeat = 8
    }

    public static DungeonFlowNetworkController Instance { get; private set; }

    [Networked] public DungeonFlowPhase Phase { get; private set; }
    [Networked] public int CurrentWave { get; private set; }
    [Networked] public int PhaseStartTick { get; private set; }
    [Networked] public float PhaseDurationSeconds { get; private set; }
    [Networked] int PhaseVersion { get; set; }

    readonly HashSet<PlayerRef> _readyPlayers = new HashSet<PlayerRef>();
    bool _reportedLocalReady;
    DungeonWaveManager _waveManager;

    public override void Spawned()
    {
        Instance = this;
        if (Object != null && Object.HasStateAuthority)
        {
            _readyPlayers.Clear();
            if (Runner != null && Runner.IsRunning)
                _readyPlayers.Add(Runner.LocalPlayer);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        TryReportLocalReady();

        if (Object == null || Object.HasStateAuthority)
            return;

        if (_waveManager == null)
            _waveManager = DungeonWaveManager.Instance;
        if (_waveManager == null)
            return;

        _waveManager.MirrorApplyHostPhase(
            Phase,
            CurrentWave,
            GetPhaseElapsedSeconds(),
            PhaseDurationSeconds);
    }

    public void HostResetDungeonFlow()
    {
        if (Object == null || !Object.HasStateAuthority)
            return;

        _readyPlayers.Clear();
        if (Runner != null && Runner.IsRunning)
            _readyPlayers.Add(Runner.LocalPlayer);
        _reportedLocalReady = false;
        HostSetPhase(DungeonFlowPhase.None, 0, 0f);
    }

    public void HostSetPhase(DungeonFlowPhase phase, int wave, float durationSeconds)
    {
        if (Object == null || !Object.HasStateAuthority || Runner == null || !Runner.IsRunning)
            return;

        Phase = phase;
        CurrentWave = Mathf.Max(0, wave);
        PhaseStartTick = Runner.Tick.Raw;
        PhaseDurationSeconds = Mathf.Max(0f, durationSeconds);
        PhaseVersion++;
    }

    public IEnumerator HostWaitForPartyReady(float timeoutSeconds)
    {
        if (Object == null || !Object.HasStateAuthority || Runner == null || !Runner.IsRunning)
            yield break;

        _readyPlayers.Add(Runner.LocalPlayer);

        float deadline = Time.realtimeSinceStartup + Mathf.Max(0f, timeoutSeconds);
        while (Time.realtimeSinceStartup < deadline)
        {
            if (AreAllActivePlayersReady())
                yield break;
            yield return null;
        }

        Debug.LogWarning("[DungeonFlow] Ready gate timeout. Continue intro with current ready players.");
    }

    bool AreAllActivePlayersReady()
    {
        if (Runner == null || !Runner.IsRunning)
            return true;

        foreach (var player in Runner.ActivePlayers)
        {
            if (!_readyPlayers.Contains(player))
                return false;
        }
        return true;
    }

    float GetPhaseElapsedSeconds()
    {
        if (Runner == null || !Runner.IsRunning)
            return 0f;
        int tickDelta = Runner.Tick.Raw - PhaseStartTick;
        if (tickDelta <= 0)
            return 0f;
        return tickDelta * Runner.DeltaTime;
    }

    void TryReportLocalReady()
    {
        if (_reportedLocalReady)
            return;
        if (Runner == null || !Runner.IsRunning)
            return;
        if (Character.LocalCharacter == null || !Character.LocalCharacter.gameObject.activeInHierarchy)
            return;

        _reportedLocalReady = true;
        RPC_ReportDungeonSceneReady();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_ReportDungeonSceneReady(RpcInfo info = default)
    {
        if (Object == null || !Object.HasStateAuthority || Runner == null || !Runner.IsRunning)
            return;

        PlayerRef source = info.Source == PlayerRef.None ? Runner.LocalPlayer : info.Source;
        _readyPlayers.Add(source);
    }
}
