using Fusion;

public enum BlacksmithSocketNetResult
{
    Success = 0,
    Fail = 1,
    NoCrystal = 2,
    NoGem = 3,
    NoTarget = 4,
    InvalidSlot = 5,
    SystemError = 6
}

public enum BlacksmithSimpleNetResult
{
    Success = 0,
    Fail = 1,
    InvalidRequest = 2,
    SystemError = 3
}

public struct NetInvEntry : INetworkStruct
{
    public int ItemId;
    public int Amount;
    public int Rarity;
    public float RollValue;

    public bool IsEmpty => ItemId < 0 || Amount <= 0;

    public static NetInvEntry Empty => new NetInvEntry { ItemId = -1, Amount = 0, Rarity = 0, RollValue = -1f };
}

public struct NetEquipEntry : INetworkStruct
{
    public int ItemId;
    public int Rarity;
    public float StatRoll;
    public int EnhancementLevel;
    public int GemId0;
    public int GemId1;
    public int GemId2;
    public int GemId3;
    public float GemRoll0;
    public float GemRoll1;
    public float GemRoll2;
    public float GemRoll3;

    public bool IsEmpty => ItemId < 0;

    public static NetEquipEntry Empty => new NetEquipEntry
    {
        ItemId = -1,
        Rarity = 0,
        StatRoll = 1f,
        EnhancementLevel = 0,
        GemId0 = -1,
        GemId1 = -1,
        GemId2 = -1,
        GemId3 = -1,
        GemRoll0 = 0f,
        GemRoll1 = 0f,
        GemRoll2 = 0f,
        GemRoll3 = 0f
    };
}

public struct NetWeaponGemEntry : INetworkStruct
{
    public int GemId;
    public float RollValue;

    public bool IsEmpty => GemId < 0;

    public static NetWeaponGemEntry Empty => new NetWeaponGemEntry { GemId = -1, RollValue = 0f };
}
