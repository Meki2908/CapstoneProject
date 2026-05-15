using System;
using UnityEngine;

public static class DungeonPartyRuntime
{
    public static event Action OnChanged;

    public static int InviteId { get; private set; }
    public static bool InviteActive { get; private set; }
    public static bool CanHostStart { get; private set; }
    public static bool AnyDeclined { get; private set; }
    public static int AcceptedCount { get; private set; }
    public static int RequiredAcceptCount { get; private set; }
    public static string TargetSceneName { get; private set; } = string.Empty;
    public static DungeonDifficulty TargetDifficulty { get; private set; } = DungeonDifficulty.Normal;
    public static int TargetMapType { get; private set; }

    public static string PendingInviteSceneName { get; private set; } = string.Empty;
    public static DungeonDifficulty PendingInviteDifficulty { get; private set; } = DungeonDifficulty.Normal;
    public static int PendingInviteMapType { get; private set; }

    public static int RetryVotes { get; private set; }
    public static int RetryRequired { get; private set; }
    public static bool RetryVoteActive { get; private set; }
    public static bool RetryAllReady { get; private set; }

    public static int AlivePlayers { get; private set; }
    public static int TotalPlayers { get; private set; }
    public static bool AllPlayersDead { get; private set; }

    public static bool EnableDebugLogs { get; set; } = true;

    static float _inviteEndsAtRealtime;
    static int _localRespondedInviteId = -1;
    static bool _localRespondedInvite;

    public static float InviteRemainingSeconds =>
        InviteActive ? Mathf.Max(0f, _inviteEndsAtRealtime - Time.realtimeSinceStartup) : 0f;

    public static bool LocalRespondedCurrentInvite =>
        _localRespondedInvite && _localRespondedInviteId == InviteId;

    public static bool LocalShouldSeeInvitationPanel =>
        InviteActive && !IsLocalHost() && !LocalRespondedCurrentInvite;

    public static bool IsLocalHost()
    {
        var c = Character.LocalCharacter;
        return c != null && c.IsHostAuthorityForParty();
    }

    public static void SetPendingInvite(string sceneName, DungeonDifficulty difficulty, int mapType)
    {
        PendingInviteSceneName = sceneName ?? string.Empty;
        PendingInviteDifficulty = difficulty;
        PendingInviteMapType = mapType;
        Dbg($"SetPendingInvite scene={PendingInviteSceneName} diff={PendingInviteDifficulty} mapType={PendingInviteMapType}");
    }

    public static void MarkLocalInviteResponded()
    {
        if (!InviteActive)
            return;
        _localRespondedInvite = true;
        _localRespondedInviteId = InviteId;
        Dbg("MarkLocalInviteResponded");
        NotifyChanged();
    }

    public static void ApplyInviteState(
        int inviteId,
        bool inviteActive,
        int acceptedCount,
        int requiredAcceptCount,
        float remainingSeconds,
        string targetSceneName,
        int difficultyRaw,
        int mapType,
        bool canHostStart,
        bool anyDeclined)
    {
        bool inviteChanged = InviteId != inviteId;
        InviteId = inviteId;
        InviteActive = inviteActive;
        AcceptedCount = Mathf.Max(0, acceptedCount);
        RequiredAcceptCount = Mathf.Max(0, requiredAcceptCount);
        _inviteEndsAtRealtime = Time.realtimeSinceStartup + Mathf.Max(0f, remainingSeconds);
        TargetSceneName = targetSceneName ?? string.Empty;
        TargetDifficulty = Enum.IsDefined(typeof(DungeonDifficulty), difficultyRaw)
            ? (DungeonDifficulty)difficultyRaw
            : DungeonDifficulty.Normal;
        TargetMapType = mapType;
        CanHostStart = canHostStart;
        AnyDeclined = anyDeclined;

        if (inviteChanged)
        {
            _localRespondedInvite = false;
            _localRespondedInviteId = -1;
        }

        if (!InviteActive)
        {
            _localRespondedInvite = false;
            _localRespondedInviteId = -1;
        }

        Dbg(
            $"ApplyInviteState id={InviteId} active={InviteActive} accepted={AcceptedCount}/{RequiredAcceptCount} " +
            $"remaining={remainingSeconds:F1}s scene={TargetSceneName} diff={TargetDifficulty} " +
            $"canStart={CanHostStart} declined={AnyDeclined} isHost={IsLocalHost()} " +
            $"responded={LocalRespondedCurrentInvite} showInvitation={LocalShouldSeeInvitationPanel} localChar={(Character.LocalCharacter != null)}");

        NotifyChanged();
    }

    public static void ApplyRetryState(int retryVotes, int retryRequired, bool active, bool allReady)
    {
        RetryVotes = Mathf.Max(0, retryVotes);
        RetryRequired = Mathf.Max(0, retryRequired);
        RetryVoteActive = active;
        RetryAllReady = allReady;
        NotifyChanged();
    }

    public static void ApplyAliveState(int alivePlayers, int totalPlayers, bool allPlayersDead)
    {
        AlivePlayers = Mathf.Max(0, alivePlayers);
        TotalPlayers = Mathf.Max(0, totalPlayers);
        AllPlayersDead = allPlayersDead;
        NotifyChanged();
    }

    public static void ClearInviteState()
    {
        ApplyInviteState(
            inviteId: InviteId,
            inviteActive: false,
            acceptedCount: 0,
            requiredAcceptCount: 0,
            remainingSeconds: 0f,
            targetSceneName: string.Empty,
            difficultyRaw: (int)DungeonDifficulty.Normal,
            mapType: 0,
            canHostStart: false,
            anyDeclined: false);
    }

    public static void ClearRetryState()
    {
        ApplyRetryState(0, 0, false, false);
    }

    static void Dbg(string msg)
    {
        if (!EnableDebugLogs)
            return;
        Debug.Log($"[DungeonPartyRuntime] {msg}");
    }

    static void NotifyChanged()
    {
        OnChanged?.Invoke();
    }
}
