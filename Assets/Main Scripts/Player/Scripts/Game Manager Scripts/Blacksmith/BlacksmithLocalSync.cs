using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Copies blacksmith-related data between local singleton managers and per-player Fusion arrays.
/// </summary>
public static class BlacksmithLocalSync
{
    public const int InvCapacity = 64;
    public const int EquipSlots = 4;
    public const int EquipGemSlots = 4;
    public const int WeaponGemSlotsPerWeapon = 3;
    public const int WeaponGemCapacity = 12;

    public static int WeaponGemFlatIndex(WeaponType weaponType, int gemSlotIndex)
    {
        return (int)weaponType * WeaponGemSlotsPerWeapon + gemSlotIndex;
    }

    public static void ExportLocalToArrays(
        NetworkArray<NetInvEntry> inv,
        NetworkArray<NetEquipEntry> equip,
        NetworkArray<NetWeaponGemEntry> weaponGems)
    {
        ClearArrays(inv, equip, weaponGems);
        ExportInventory(inv);
        ExportEquipment(equip);
        ExportWeaponGems(weaponGems);
    }

    static void ClearArrays(
        NetworkArray<NetInvEntry> inv,
        NetworkArray<NetEquipEntry> equip,
        NetworkArray<NetWeaponGemEntry> weaponGems)
    {
        for (int i = 0; i < inv.Length; i++)
            inv.Set(i, NetInvEntry.Empty);
        for (int i = 0; i < equip.Length; i++)
            equip.Set(i, NetEquipEntry.Empty);
        for (int i = 0; i < weaponGems.Length; i++)
            weaponGems.Set(i, NetWeaponGemEntry.Empty);
    }

    static void ExportInventory(NetworkArray<NetInvEntry> inv)
    {
        if (InventoryManager.Instance == null)
            return;

        int idx = 0;
        foreach (var (item, amount, rarity, rolls) in InventoryManager.Instance.GetAllItemsWithRarityAndRolls())
        {
            if (item == null || amount <= 0 || idx >= inv.Length)
                break;

            float roll = -1f;
            if (rolls != null && rolls.Count > 0)
                roll = rolls[0];

            inv.Set(idx++, new NetInvEntry
            {
                ItemId = item.id,
                Amount = amount,
                Rarity = (int)rarity,
                RollValue = roll
            });
        }
    }

    static void ExportEquipment(NetworkArray<NetEquipEntry> equip)
    {
        if (EquipmentManager.Instance == null)
            return;

        for (int slot = 0; slot < EquipSlots && slot < equip.Length; slot++)
        {
            var item = EquipmentManager.Instance.GetEquippedItemByIndex(slot);
            if (item == null)
                continue;

            var entry = new NetEquipEntry
            {
                ItemId = item.id,
                Rarity = (int)EquipmentManager.Instance.GetEquippedRarity(slot),
                StatRoll = EquipmentManager.Instance.GetEquipStatRoll(slot),
                EnhancementLevel = EquipmentManager.Instance.GetEnhancementLevel(slot)
            };

            for (int g = 0; g < EquipGemSlots; g++)
            {
                var gem = EquipmentManager.Instance.GetEquippedGem(slot, g);
                int gemId = gem != null ? gem.id : -1;
                float gemRoll = gemId >= 0
                    ? EquipmentManager.Instance.GetRolledGemValue(slot, g)
                    : 0f;

                switch (g)
                {
                    case 0: entry.GemId0 = gemId; entry.GemRoll0 = gemRoll; break;
                    case 1: entry.GemId1 = gemId; entry.GemRoll1 = gemRoll; break;
                    case 2: entry.GemId2 = gemId; entry.GemRoll2 = gemRoll; break;
                    case 3: entry.GemId3 = gemId; entry.GemRoll3 = gemRoll; break;
                }
            }

            equip.Set(slot, entry);
        }
    }

    static void ExportWeaponGems(NetworkArray<NetWeaponGemEntry> weaponGems)
    {
        if (WeaponGemManager.Instance == null)
            return;

        foreach (WeaponType wt in new[] { WeaponType.Sword, WeaponType.Axe, WeaponType.Mage })
        {
            for (int s = 0; s < WeaponGemSlotsPerWeapon; s++)
            {
                int flat = WeaponGemFlatIndex(wt, s);
                if (flat < 0 || flat >= weaponGems.Length)
                    continue;

                var gem = WeaponGemManager.Instance.GetEquippedGem(wt, s);
                if (gem == null)
                    continue;

                weaponGems.Set(flat, new NetWeaponGemEntry
                {
                    GemId = gem.id,
                    RollValue = WeaponGemManager.Instance.GetRolledGemValue(wt, s)
                });
            }
        }
    }

    /// <summary>
    /// Export local managers into scratch arrays (used before RPC hydrate).
    /// </summary>
    public static void ExportLocalToScratch(
        NetInvEntry[] inv,
        NetEquipEntry[] equip,
        NetWeaponGemEntry[] weaponGems)
    {
        if (inv == null || equip == null || weaponGems == null)
            return;

        for (int i = 0; i < inv.Length; i++)
            inv[i] = NetInvEntry.Empty;
        for (int i = 0; i < equip.Length; i++)
            equip[i] = NetEquipEntry.Empty;
        for (int i = 0; i < weaponGems.Length; i++)
            weaponGems[i] = NetWeaponGemEntry.Empty;

        int idx = 0;
        if (InventoryManager.Instance != null)
        {
            foreach (var (item, amount, rarity, rolls) in InventoryManager.Instance.GetAllItemsWithRarityAndRolls())
            {
                if (item == null || amount <= 0 || idx >= inv.Length)
                    break;

                float roll = -1f;
                if (rolls != null && rolls.Count > 0)
                    roll = rolls[0];

                inv[idx++] = new NetInvEntry
                {
                    ItemId = item.id,
                    Amount = amount,
                    Rarity = (int)rarity,
                    RollValue = roll
                };
            }
        }

        if (EquipmentManager.Instance != null)
        {
            for (int slot = 0; slot < EquipSlots && slot < equip.Length; slot++)
            {
                var item = EquipmentManager.Instance.GetEquippedItemByIndex(slot);
                if (item == null)
                    continue;

                var entry = new NetEquipEntry
                {
                    ItemId = item.id,
                    Rarity = (int)EquipmentManager.Instance.GetEquippedRarity(slot),
                    StatRoll = EquipmentManager.Instance.GetEquipStatRoll(slot),
                    EnhancementLevel = EquipmentManager.Instance.GetEnhancementLevel(slot)
                };

                entry.GemId0 = EquipmentManager.Instance.GetEquippedGem(slot, 0)?.id ?? -1;
                entry.GemId1 = EquipmentManager.Instance.GetEquippedGem(slot, 1)?.id ?? -1;
                entry.GemId2 = EquipmentManager.Instance.GetEquippedGem(slot, 2)?.id ?? -1;
                entry.GemId3 = EquipmentManager.Instance.GetEquippedGem(slot, 3)?.id ?? -1;
                entry.GemRoll0 = entry.GemId0 >= 0 ? EquipmentManager.Instance.GetRolledGemValue(slot, 0) : 0f;
                entry.GemRoll1 = entry.GemId1 >= 0 ? EquipmentManager.Instance.GetRolledGemValue(slot, 1) : 0f;
                entry.GemRoll2 = entry.GemId2 >= 0 ? EquipmentManager.Instance.GetRolledGemValue(slot, 2) : 0f;
                entry.GemRoll3 = entry.GemId3 >= 0 ? EquipmentManager.Instance.GetRolledGemValue(slot, 3) : 0f;
                equip[slot] = entry;
            }
        }

        if (WeaponGemManager.Instance != null)
        {
            foreach (WeaponType wt in new[] { WeaponType.Sword, WeaponType.Axe, WeaponType.Mage })
            {
                for (int s = 0; s < WeaponGemSlotsPerWeapon; s++)
                {
                    int flat = WeaponGemFlatIndex(wt, s);
                    if (flat < 0 || flat >= weaponGems.Length)
                        continue;

                    var gem = WeaponGemManager.Instance.GetEquippedGem(wt, s);
                    if (gem == null)
                        continue;

                    weaponGems[flat] = new NetWeaponGemEntry
                    {
                        GemId = gem.id,
                        RollValue = WeaponGemManager.Instance.GetRolledGemValue(wt, s)
                    };
                }
            }
        }
    }

    public static void ImportArraysToLocal(
        NetworkArray<NetInvEntry> inv,
        NetworkArray<NetEquipEntry> equip,
        NetworkArray<NetWeaponGemEntry> weaponGems,
        bool saveToDisk)
    {
        var invList = new List<NetInvEntry>(InvCapacity);
        for (int i = 0; i < inv.Length; i++)
        {
            var e = inv.Get(i);
            if (!e.IsEmpty)
                invList.Add(e);
        }

        var equipList = new NetEquipEntry[EquipSlots];
        for (int i = 0; i < equip.Length && i < EquipSlots; i++)
            equipList[i] = equip.Get(i);

        var gemList = new NetWeaponGemEntry[WeaponGemCapacity];
        for (int i = 0; i < weaponGems.Length && i < WeaponGemCapacity; i++)
            gemList[i] = weaponGems.Get(i);

        InventoryManager.Instance?.ReplaceAllForNetwork(invList, saveToDisk);
        EquipmentManager.Instance?.ReplaceAllForNetwork(equipList, saveToDisk);
        WeaponGemManager.Instance?.ReplaceAllForNetwork(gemList, saveToDisk);
    }
}
