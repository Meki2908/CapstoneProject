using System;
using Fusion;
using UnityEngine;

public partial class Character
{
    [Networked, Capacity(BlacksmithLocalSync.InvCapacity)]
    public NetworkArray<NetInvEntry> NetBlacksmithInventory { get; }

    [Networked, Capacity(BlacksmithLocalSync.EquipSlots)]
    public NetworkArray<NetEquipEntry> NetBlacksmithEquipment { get; }

    [Networked, Capacity(BlacksmithLocalSync.WeaponGemCapacity)]
    public NetworkArray<NetWeaponGemEntry> NetBlacksmithWeaponGems { get; }

    [Networked] public int NetBlacksmithRevision { get; set; }
    [Networked] public NetworkBool NetBlacksmithHydrated { get; set; }

    public event Action<BlacksmithSocketNetResult, int> BlacksmithSocketResolved;
    public event Action<BlacksmithSimpleNetResult> BlacksmithFuseResolved;
    public event Action<BlacksmithSimpleNetResult> BlacksmithUnequipResolved;
    public event Action<BlacksmithSimpleNetResult> BlacksmithRemoveGemResolved;

    int _lastBlacksmithRequestTick = int.MinValue;
    int _lastAppliedBlacksmithRevision = -1;
    bool _blacksmithHydratePushInFlight;

    public static bool IsOnlineFusionBlacksmith(NetworkRunner runner)
    {
        return runner != null && runner.IsRunning && runner.GameMode != GameMode.Single;
    }

    void BlacksmithNetworkOnSpawned()
    {
        if (HasInputAuthority)
            _lastAppliedBlacksmithRevision = NetBlacksmithRevision;
    }

    void BlacksmithNetworkOnRenderChange(string change)
    {
        if (change == nameof(NetBlacksmithRevision) && HasInputAuthority)
            TryApplyBlacksmithRevisionToLocal();
    }

    void TryApplyBlacksmithRevisionToLocal()
    {
        if (!HasInputAuthority || NetBlacksmithRevision == _lastAppliedBlacksmithRevision)
            return;

        _lastAppliedBlacksmithRevision = NetBlacksmithRevision;
        BlacksmithLocalSync.ImportArraysToLocal(
            NetBlacksmithInventory,
            NetBlacksmithEquipment,
            NetBlacksmithWeaponGems,
            saveToDisk: true);
    }

    public void EnsureBlacksmithNetworkHydrated()
    {
        if (Object == null || !Object.IsValid || Runner == null || !Runner.IsRunning || !HasInputAuthority)
            return;
        if (_blacksmithHydratePushInFlight)
            return;

        if (HasStateAuthority)
        {
            ApplyLocalBlacksmithHydrateOnAuthority();
            return;
        }

        _blacksmithHydratePushInFlight = true;
        var inv = new NetInvEntry[BlacksmithLocalSync.InvCapacity];
        var equip = new NetEquipEntry[BlacksmithLocalSync.EquipSlots];
        var gems = new NetWeaponGemEntry[BlacksmithLocalSync.WeaponGemCapacity];
        BlacksmithLocalSync.ExportLocalToScratch(inv, equip, gems);

        RPC_PushBlacksmithInvChunk(0, inv[0], inv[1], inv[2], inv[3], inv[4], inv[5], inv[6], inv[7],
            inv[8], inv[9], inv[10], inv[11], inv[12], inv[13], inv[14], inv[15]);
        RPC_PushBlacksmithInvChunk(1, inv[16], inv[17], inv[18], inv[19], inv[20], inv[21], inv[22], inv[23],
            inv[24], inv[25], inv[26], inv[27], inv[28], inv[29], inv[30], inv[31]);
        RPC_PushBlacksmithInvChunk(2, inv[32], inv[33], inv[34], inv[35], inv[36], inv[37], inv[38], inv[39],
            inv[40], inv[41], inv[42], inv[43], inv[44], inv[45], inv[46], inv[47]);
        RPC_PushBlacksmithInvChunk(3, inv[48], inv[49], inv[50], inv[51], inv[52], inv[53], inv[54], inv[55],
            inv[56], inv[57], inv[58], inv[59], inv[60], inv[61], inv[62], inv[63]);
        RPC_PushBlacksmithFinishHydrate(
            equip[0], equip[1], equip[2], equip[3],
            gems[0], gems[1], gems[2], gems[3], gems[4], gems[5], gems[6], gems[7], gems[8], gems[9], gems[10], gems[11]);
    }

    void ApplyLocalBlacksmithHydrateOnAuthority()
    {
        BlacksmithLocalSync.ExportLocalToArrays(
            NetBlacksmithInventory,
            NetBlacksmithEquipment,
            NetBlacksmithWeaponGems);
        NetBlacksmithHydrated = true;
        NetBlacksmithRevision++;
        _blacksmithHydratePushInFlight = false;
    }

    void BumpBlacksmithRevision()
    {
        NetBlacksmithRevision++;
    }

    bool TryDedupeBlacksmithRequest()
    {
        if (Runner == null)
            return false;
        int tick = (int)Runner.Tick;
        if (tick == _lastBlacksmithRequestTick)
            return false;
        _lastBlacksmithRequestTick = tick;
        return true;
    }

    bool AuthorityEnsureBlacksmithReady()
    {
        if (!(bool)NetBlacksmithHydrated)
            return false;
        return InventoryManager.Instance != null
            && RefinementManager.Instance != null
            && SocketingManager.Instance != null;
    }

    Item NetGetItemById(int itemId)
    {
        return InventoryManager.Instance != null ? InventoryManager.Instance.GetItemById(itemId) : null;
    }

    int NetGetInvAmount(int itemId, int rarity = -1)
    {
        int total = 0;
        for (int i = 0; i < NetBlacksmithInventory.Length; i++)
        {
            var e = NetBlacksmithInventory.Get(i);
            if (e.IsEmpty || e.ItemId != itemId)
                continue;
            if (rarity >= 0 && e.Rarity != rarity)
                continue;
            total += e.Amount;
        }
        return total;
    }

    int NetFindInvIndex(int itemId, int rarity)
    {
        for (int i = 0; i < NetBlacksmithInventory.Length; i++)
        {
            var e = NetBlacksmithInventory.Get(i);
            if (!e.IsEmpty && e.ItemId == itemId && (rarity < 0 || e.Rarity == rarity))
                return i;
        }
        return -1;
    }

    bool NetRemoveInv(int itemId, int amount, int rarity, out float removedRoll)
    {
        removedRoll = -1f;
        int idx = NetFindInvIndex(itemId, rarity);
        if (idx < 0)
            return false;

        var e = NetBlacksmithInventory.Get(idx);
        if (e.Amount < amount)
            return false;

        removedRoll = e.RollValue;
        e.Amount -= amount;
        if (e.Amount <= 0)
            e = NetInvEntry.Empty;
        NetBlacksmithInventory.Set(idx, e);
        return true;
    }

    bool NetAddInv(int itemId, int amount, int rarity, float roll)
    {
        int idx = NetFindInvIndex(itemId, rarity);
        if (idx >= 0)
        {
            var e = NetBlacksmithInventory.Get(idx);
            e.Amount += amount;
            if (roll >= 0f)
                e.RollValue = roll;
            NetBlacksmithInventory.Set(idx, e);
            return true;
        }

        for (int i = 0; i < NetBlacksmithInventory.Length; i++)
        {
            var e = NetBlacksmithInventory.Get(i);
            if (!e.IsEmpty)
                continue;
            NetBlacksmithInventory.Set(i, new NetInvEntry
            {
                ItemId = itemId,
                Amount = amount,
                Rarity = rarity,
                RollValue = roll
            });
            return true;
        }

        return false;
    }

    void NetReturnEquipToInv(int slotIndex)
    {
        var equip = NetBlacksmithEquipment.Get(slotIndex);
        if (equip.IsEmpty)
            return;

        NetAddInv(equip.ItemId, 1, equip.Rarity, equip.StatRoll);
    }

    void NetClearEquipGems(ref NetEquipEntry equip)
    {
        equip.GemId0 = equip.GemId1 = equip.GemId2 = equip.GemId3 = -1;
        equip.GemRoll0 = equip.GemRoll1 = equip.GemRoll2 = equip.GemRoll3 = 0f;
    }

    BlacksmithRefineNetResult AuthorityTryRefine(int equipSlotIndex, int materialItemId, out int oldLevel, out int newLevel)
    {
        oldLevel = 0;
        newLevel = 0;

        if (!AuthorityEnsureBlacksmithReady())
            return BlacksmithRefineNetResult.SystemError;

        if (equipSlotIndex < 0 || equipSlotIndex >= BlacksmithLocalSync.EquipSlots)
            return BlacksmithRefineNetResult.InvalidRequest;

        var equip = NetBlacksmithEquipment.Get(equipSlotIndex);
        if (equip.IsEmpty)
            return BlacksmithRefineNetResult.NoEquipment;

        var stone = NetGetItemById(materialItemId);
        if (stone == null || stone.itemType != ItemType.Material || stone.refinementTier <= 0)
            return BlacksmithRefineNetResult.NoStone;

        oldLevel = equip.EnhancementLevel;
        newLevel = oldLevel;
        if (oldLevel >= EquipmentManager.MAX_ENHANCEMENT_LEVEL)
            return BlacksmithRefineNetResult.MaxLevel;

        if (NetGetInvAmount(materialItemId) <= 0)
            return BlacksmithRefineNetResult.NoStone;

        float rate = RefinementManager.Instance.CalculateRefineRate(oldLevel, stone.refinementTier);
        if (!NetRemoveInv(materialItemId, 1, -1, out _))
            return BlacksmithRefineNetResult.NoStone;

        float roll = UnityEngine.Random.Range(0f, 1f);
        if (roll <= rate)
        {
            newLevel = oldLevel + 1;
            equip.EnhancementLevel = newLevel;
            NetBlacksmithEquipment.Set(equipSlotIndex, equip);
            BumpBlacksmithRevision();
            return BlacksmithRefineNetResult.Success;
        }

        newLevel = Mathf.Max(0, oldLevel - 1);
        if (newLevel < oldLevel)
        {
            equip.EnhancementLevel = newLevel;
            NetBlacksmithEquipment.Set(equipSlotIndex, equip);
        }
        BumpBlacksmithRevision();
        return BlacksmithRefineNetResult.Fail;
    }

    BlacksmithSimpleNetResult AuthorityTryFuse(int materialItemId)
    {
        if (!AuthorityEnsureBlacksmithReady())
            return BlacksmithSimpleNetResult.SystemError;

        var sourceStone = NetGetItemById(materialItemId);
        if (sourceStone == null || sourceStone.refinementTier <= 0 || sourceStone.refinementTier >= 7)
            return BlacksmithSimpleNetResult.InvalidRequest;
        if (NetGetInvAmount(materialItemId) < 4)
            return BlacksmithSimpleNetResult.InvalidRequest;

        var resultStone = RefinementManager.Instance.GetFusionResultStone(sourceStone);
        if (resultStone == null)
            return BlacksmithSimpleNetResult.Fail;

        if (!NetRemoveInv(materialItemId, 4, -1, out _))
            return BlacksmithSimpleNetResult.Fail;

        if (!NetAddInv(resultStone.id, 1, (int)resultStone.rarity, -1f))
            return BlacksmithSimpleNetResult.SystemError;

        BumpBlacksmithRevision();
        return BlacksmithSimpleNetResult.Success;
    }

    BlacksmithSocketNetResult AuthorityTrySocketWeapon(int weaponTypeValue, int gemSlotIndex, int gemId, int crystalId)
    {
        if (!AuthorityEnsureBlacksmithReady())
            return BlacksmithSocketNetResult.SystemError;

        var weaponType = (WeaponType)weaponTypeValue;
        if (weaponType == WeaponType.None || gemSlotIndex < 0 || gemSlotIndex >= BlacksmithLocalSync.WeaponGemSlotsPerWeapon)
            return BlacksmithSocketNetResult.InvalidSlot;

        var gem = NetGetItemById(gemId);
        var crystal = NetGetItemById(crystalId);
        if (gem == null || gem.itemType != ItemType.Gems)
            return BlacksmithSocketNetResult.NoGem;
        if (crystal == null || crystal.itemType != ItemType.CrystalStone)
            return BlacksmithSocketNetResult.NoCrystal;
        if (NetGetInvAmount(gemId) <= 0)
            return BlacksmithSocketNetResult.NoGem;
        if (NetGetInvAmount(crystalId) <= 0)
            return BlacksmithSocketNetResult.NoCrystal;

        float successRate = SocketingManager.Instance.CalculateSuccessRate(gem.rarity, crystal.rarity);
        if (!NetRemoveInv(crystalId, 1, -1, out _))
            return BlacksmithSocketNetResult.NoCrystal;

        float roll = UnityEngine.Random.Range(0f, 1f);
        if (roll <= successRate)
        {
            int flat = BlacksmithLocalSync.WeaponGemFlatIndex(weaponType, gemSlotIndex);
            var current = NetBlacksmithWeaponGems.Get(flat);
            if (!current.IsEmpty)
                NetAddInv(current.GemId, 1, (int)(NetGetItemById(current.GemId)?.rarity ?? Rarity.Common), current.RollValue);

            if (!NetRemoveInv(gemId, 1, -1, out float gemRoll))
            {
                NetAddInv(crystalId, 1, (int)crystal.rarity, -1f);
                return BlacksmithSocketNetResult.NoGem;
            }

            if (gemRoll < 0f)
                gemRoll = gem.gemValuePercent;

            NetBlacksmithWeaponGems.Set(flat, new NetWeaponGemEntry { GemId = gemId, RollValue = gemRoll });
            BumpBlacksmithRevision();
            return BlacksmithSocketNetResult.Success;
        }

        NetRemoveInv(gemId, 1, -1, out _);
        BumpBlacksmithRevision();
        return BlacksmithSocketNetResult.Fail;
    }

    BlacksmithSocketNetResult AuthorityTrySocketEquipment(int equipSlotIndex, int equipId, int equipRarity, int crystalId)
    {
        if (!AuthorityEnsureBlacksmithReady())
            return BlacksmithSocketNetResult.SystemError;

        if (equipSlotIndex < 0 || equipSlotIndex >= BlacksmithLocalSync.EquipSlots)
            return BlacksmithSocketNetResult.InvalidSlot;

        var equipItem = NetGetItemById(equipId);
        var crystal = NetGetItemById(crystalId);
        if (equipItem == null || equipItem.itemType != ItemType.Equipment)
            return BlacksmithSocketNetResult.NoGem;
        if (crystal == null || crystal.itemType != ItemType.CrystalStone)
            return BlacksmithSocketNetResult.NoCrystal;
        if (NetGetInvAmount(equipId, equipRarity) <= 0)
            return BlacksmithSocketNetResult.NoGem;
        if (NetGetInvAmount(crystalId) <= 0)
            return BlacksmithSocketNetResult.NoCrystal;

        float successRate = SocketingManager.Instance.CalculateSuccessRate((Rarity)equipRarity, crystal.rarity);
        if (!NetRemoveInv(crystalId, 1, -1, out _))
            return BlacksmithSocketNetResult.NoCrystal;

        float roll = UnityEngine.Random.Range(0f, 1f);
        if (roll <= successRate)
        {
            if (!NetRemoveInv(equipId, 1, equipRarity, out float statRoll))
            {
                NetAddInv(crystalId, 1, (int)crystal.rarity, -1f);
                return BlacksmithSocketNetResult.NoGem;
            }

            if (statRoll < 0f)
                statRoll = 1f;

            NetReturnEquipToInv(equipSlotIndex);
            var entry = NetEquipEntry.Empty;
            entry.ItemId = equipId;
            entry.Rarity = equipRarity;
            entry.StatRoll = statRoll;
            entry.EnhancementLevel = 0;
            NetBlacksmithEquipment.Set(equipSlotIndex, entry);
            BumpBlacksmithRevision();
            return BlacksmithSocketNetResult.Success;
        }

        BumpBlacksmithRevision();
        return BlacksmithSocketNetResult.Fail;
    }

    BlacksmithSimpleNetResult AuthorityTryRemoveWeaponGem(int weaponTypeValue, int gemSlotIndex)
    {
        if (!AuthorityEnsureBlacksmithReady())
            return BlacksmithSimpleNetResult.SystemError;

        var weaponType = (WeaponType)weaponTypeValue;
        if (weaponType == WeaponType.None || gemSlotIndex < 0 || gemSlotIndex >= BlacksmithLocalSync.WeaponGemSlotsPerWeapon)
            return BlacksmithSimpleNetResult.InvalidRequest;

        int flat = BlacksmithLocalSync.WeaponGemFlatIndex(weaponType, gemSlotIndex);
        var current = NetBlacksmithWeaponGems.Get(flat);
        if (current.IsEmpty)
            return BlacksmithSimpleNetResult.Fail;

        NetBlacksmithWeaponGems.Set(flat, NetWeaponGemEntry.Empty);
        BumpBlacksmithRevision();
        return BlacksmithSimpleNetResult.Success;
    }

    BlacksmithSimpleNetResult AuthorityTryRemoveEquipGem(int equipSlotIndex, int gemSlotIndex)
    {
        if (!AuthorityEnsureBlacksmithReady())
            return BlacksmithSimpleNetResult.SystemError;

        if (equipSlotIndex < 0 || equipSlotIndex >= BlacksmithLocalSync.EquipSlots
            || gemSlotIndex < 0 || gemSlotIndex >= BlacksmithLocalSync.EquipGemSlots)
            return BlacksmithSimpleNetResult.InvalidRequest;

        var equip = NetBlacksmithEquipment.Get(equipSlotIndex);
        if (equip.IsEmpty)
            return BlacksmithSimpleNetResult.Fail;

        switch (gemSlotIndex)
        {
            case 0:
                if (equip.GemId0 < 0) return BlacksmithSimpleNetResult.Fail;
                equip.GemId0 = -1; equip.GemRoll0 = 0f; break;
            case 1:
                if (equip.GemId1 < 0) return BlacksmithSimpleNetResult.Fail;
                equip.GemId1 = -1; equip.GemRoll1 = 0f; break;
            case 2:
                if (equip.GemId2 < 0) return BlacksmithSimpleNetResult.Fail;
                equip.GemId2 = -1; equip.GemRoll2 = 0f; break;
            case 3:
                if (equip.GemId3 < 0) return BlacksmithSimpleNetResult.Fail;
                equip.GemId3 = -1; equip.GemRoll3 = 0f; break;
            default:
                return BlacksmithSimpleNetResult.InvalidRequest;
        }

        NetBlacksmithEquipment.Set(equipSlotIndex, equip);
        BumpBlacksmithRevision();
        return BlacksmithSimpleNetResult.Success;
    }

    BlacksmithSimpleNetResult AuthorityTryUnequip(int equipSlotIndex)
    {
        if (!AuthorityEnsureBlacksmithReady())
            return BlacksmithSimpleNetResult.SystemError;

        if (equipSlotIndex < 0 || equipSlotIndex >= BlacksmithLocalSync.EquipSlots)
            return BlacksmithSimpleNetResult.InvalidRequest;

        var equip = NetBlacksmithEquipment.Get(equipSlotIndex);
        if (equip.IsEmpty)
            return BlacksmithSimpleNetResult.Fail;

        NetReturnEquipToInv(equipSlotIndex);
        NetClearEquipGems(ref equip);
        equip.ItemId = -1;
        equip.Rarity = 0;
        equip.StatRoll = 1f;
        equip.EnhancementLevel = 0;
        NetBlacksmithEquipment.Set(equipSlotIndex, equip);
        BumpBlacksmithRevision();
        return BlacksmithSimpleNetResult.Success;
    }

    // ─── Public request API (InputAuthority) ───────────────────────

    public bool TryRequestBlacksmithRefine(int equipSlotIndex, int materialItemId)
    {
        if (Runner == null || !Runner.IsRunning || Object == null || !Object.IsValid || !HasInputAuthority)
            return false;
        if (!TryDedupeBlacksmithRequest())
            return false;

        EnsureBlacksmithNetworkHydrated();
        RPC_RequestBlacksmithRefine(equipSlotIndex, materialItemId);
        return true;
    }

    public bool TryRequestBlacksmithFuse(int materialItemId)
    {
        if (Runner == null || !Runner.IsRunning || Object == null || !Object.IsValid || !HasInputAuthority)
            return false;
        if (!TryDedupeBlacksmithRequest())
            return false;

        EnsureBlacksmithNetworkHydrated();
        RPC_RequestBlacksmithFuse(materialItemId);
        return true;
    }

    public bool TryRequestBlacksmithSocketWeapon(int weaponTypeValue, int gemSlotIndex, int gemId, int crystalId)
    {
        if (Runner == null || !Runner.IsRunning || Object == null || !Object.IsValid || !HasInputAuthority)
            return false;
        if (!TryDedupeBlacksmithRequest())
            return false;

        EnsureBlacksmithNetworkHydrated();
        RPC_RequestBlacksmithSocketWeapon(weaponTypeValue, gemSlotIndex, gemId, crystalId);
        return true;
    }

    public bool TryRequestBlacksmithSocketEquipment(int equipSlotIndex, int equipId, int equipRarity, int crystalId)
    {
        if (Runner == null || !Runner.IsRunning || Object == null || !Object.IsValid || !HasInputAuthority)
            return false;
        if (!TryDedupeBlacksmithRequest())
            return false;

        EnsureBlacksmithNetworkHydrated();
        RPC_RequestBlacksmithSocketEquipment(equipSlotIndex, equipId, equipRarity, crystalId);
        return true;
    }

    public bool TryRequestBlacksmithRemoveWeaponGem(int weaponTypeValue, int gemSlotIndex)
    {
        if (Runner == null || !Runner.IsRunning || Object == null || !Object.IsValid || !HasInputAuthority)
            return false;
        if (!TryDedupeBlacksmithRequest())
            return false;

        EnsureBlacksmithNetworkHydrated();
        RPC_RequestBlacksmithRemoveWeaponGem(weaponTypeValue, gemSlotIndex);
        return true;
    }

    public bool TryRequestBlacksmithRemoveEquipGem(int equipSlotIndex, int gemSlotIndex)
    {
        if (Runner == null || !Runner.IsRunning || Object == null || !Object.IsValid || !HasInputAuthority)
            return false;
        if (!TryDedupeBlacksmithRequest())
            return false;

        EnsureBlacksmithNetworkHydrated();
        RPC_RequestBlacksmithRemoveEquipGem(equipSlotIndex, gemSlotIndex);
        return true;
    }

    public bool TryRequestBlacksmithUnequip(int equipSlotIndex)
    {
        if (Runner == null || !Runner.IsRunning || Object == null || !Object.IsValid || !HasInputAuthority)
            return false;
        if (!TryDedupeBlacksmithRequest())
            return false;

        EnsureBlacksmithNetworkHydrated();
        RPC_RequestBlacksmithUnequip(equipSlotIndex);
        return true;
    }

    // ─── Hydrate RPCs ────────────────────────────────────────────

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_PushBlacksmithInvChunk(int chunkIndex,
        NetInvEntry e0, NetInvEntry e1, NetInvEntry e2, NetInvEntry e3,
        NetInvEntry e4, NetInvEntry e5, NetInvEntry e6, NetInvEntry e7,
        NetInvEntry e8, NetInvEntry e9, NetInvEntry e10, NetInvEntry e11,
        NetInvEntry e12, NetInvEntry e13, NetInvEntry e14, NetInvEntry e15)
    {
        int start = chunkIndex * 16;
        NetInvEntry[] entries = { e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15 };
        for (int i = 0; i < entries.Length; i++)
        {
            int idx = start + i;
            if (idx >= 0 && idx < NetBlacksmithInventory.Length)
                NetBlacksmithInventory.Set(idx, entries[i]);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_PushBlacksmithFinishHydrate(
        NetEquipEntry eq0, NetEquipEntry eq1, NetEquipEntry eq2, NetEquipEntry eq3,
        NetWeaponGemEntry g0, NetWeaponGemEntry g1, NetWeaponGemEntry g2, NetWeaponGemEntry g3,
        NetWeaponGemEntry g4, NetWeaponGemEntry g5, NetWeaponGemEntry g6, NetWeaponGemEntry g7,
        NetWeaponGemEntry g8, NetWeaponGemEntry g9, NetWeaponGemEntry g10, NetWeaponGemEntry g11)
    {
        NetEquipEntry[] equip = { eq0, eq1, eq2, eq3 };
        for (int i = 0; i < equip.Length; i++)
            NetBlacksmithEquipment.Set(i, equip[i]);

        NetWeaponGemEntry[] gems = { g0, g1, g2, g3, g4, g5, g6, g7, g8, g9, g10, g11 };
        for (int i = 0; i < gems.Length; i++)
            NetBlacksmithWeaponGems.Set(i, gems[i]);

        NetBlacksmithHydrated = true;
        NetBlacksmithRevision++;
        RPC_BlacksmithHydrateAck();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    void RPC_BlacksmithHydrateAck()
    {
        _blacksmithHydratePushInFlight = false;
        _lastAppliedBlacksmithRevision = NetBlacksmithRevision;
    }

    // ─── Operation RPCs ────────────────────────────────────────────

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestBlacksmithRefine(int equipSlotIndex, int materialItemId)
    {
        if (!(bool)NetBlacksmithHydrated)
        {
            RPC_BlacksmithRefineResult((int)BlacksmithRefineNetResult.SystemError, equipSlotIndex, 0, 0);
            return;
        }

        var result = AuthorityTryRefine(equipSlotIndex, materialItemId, out int oldLevel, out int newLevel);
        RPC_BlacksmithRefineResult((int)result, equipSlotIndex, oldLevel, newLevel);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    void RPC_BlacksmithRefineResult(int resultCode, int equipSlotIndex, int oldLevel, int newLevel)
    {
        var result = Enum.IsDefined(typeof(BlacksmithRefineNetResult), resultCode)
            ? (BlacksmithRefineNetResult)resultCode
            : BlacksmithRefineNetResult.SystemError;
        TryApplyBlacksmithRevisionToLocal();
        BlacksmithRefineResolved?.Invoke(result, equipSlotIndex, oldLevel, newLevel);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestBlacksmithFuse(int materialItemId)
    {
        var result = !(bool)NetBlacksmithHydrated
            ? BlacksmithSimpleNetResult.SystemError
            : AuthorityTryFuse(materialItemId);
        RPC_BlacksmithFuseResult((int)result);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    void RPC_BlacksmithFuseResult(int resultCode)
    {
        var result = Enum.IsDefined(typeof(BlacksmithSimpleNetResult), resultCode)
            ? (BlacksmithSimpleNetResult)resultCode
            : BlacksmithSimpleNetResult.SystemError;
        TryApplyBlacksmithRevisionToLocal();
        BlacksmithFuseResolved?.Invoke(result);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestBlacksmithSocketWeapon(int weaponTypeValue, int gemSlotIndex, int gemId, int crystalId)
    {
        var result = !(bool)NetBlacksmithHydrated
            ? BlacksmithSocketNetResult.SystemError
            : AuthorityTrySocketWeapon(weaponTypeValue, gemSlotIndex, gemId, crystalId);
        RPC_BlacksmithSocketResult((int)result, 0);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestBlacksmithSocketEquipment(int equipSlotIndex, int equipId, int equipRarity, int crystalId)
    {
        var result = !(bool)NetBlacksmithHydrated
            ? BlacksmithSocketNetResult.SystemError
            : AuthorityTrySocketEquipment(equipSlotIndex, equipId, equipRarity, crystalId);
        RPC_BlacksmithSocketResult((int)result, 1);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    void RPC_BlacksmithSocketResult(int resultCode, int tabCode)
    {
        var result = Enum.IsDefined(typeof(BlacksmithSocketNetResult), resultCode)
            ? (BlacksmithSocketNetResult)resultCode
            : BlacksmithSocketNetResult.SystemError;
        TryApplyBlacksmithRevisionToLocal();
        BlacksmithSocketResolved?.Invoke(result, tabCode);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestBlacksmithRemoveWeaponGem(int weaponTypeValue, int gemSlotIndex)
    {
        var result = !(bool)NetBlacksmithHydrated
            ? BlacksmithSimpleNetResult.SystemError
            : AuthorityTryRemoveWeaponGem(weaponTypeValue, gemSlotIndex);
        RPC_BlacksmithRemoveGemResult((int)result);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestBlacksmithRemoveEquipGem(int equipSlotIndex, int gemSlotIndex)
    {
        var result = !(bool)NetBlacksmithHydrated
            ? BlacksmithSimpleNetResult.SystemError
            : AuthorityTryRemoveEquipGem(equipSlotIndex, gemSlotIndex);
        RPC_BlacksmithRemoveGemResult((int)result);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    void RPC_BlacksmithRemoveGemResult(int resultCode)
    {
        var result = Enum.IsDefined(typeof(BlacksmithSimpleNetResult), resultCode)
            ? (BlacksmithSimpleNetResult)resultCode
            : BlacksmithSimpleNetResult.SystemError;
        TryApplyBlacksmithRevisionToLocal();
        BlacksmithRemoveGemResolved?.Invoke(result);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestBlacksmithUnequip(int equipSlotIndex)
    {
        var result = !(bool)NetBlacksmithHydrated
            ? BlacksmithSimpleNetResult.SystemError
            : AuthorityTryUnequip(equipSlotIndex);
        RPC_BlacksmithUnequipResult((int)result);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    void RPC_BlacksmithUnequipResult(int resultCode)
    {
        var result = Enum.IsDefined(typeof(BlacksmithSimpleNetResult), resultCode)
            ? (BlacksmithSimpleNetResult)resultCode
            : BlacksmithSimpleNetResult.SystemError;
        TryApplyBlacksmithRevisionToLocal();
        BlacksmithUnequipResolved?.Invoke(result);
    }
}
