namespace CessnaCrew.WebApi.Models;

// Alle Enums werden in der DB als Text gespeichert (z. B. "Owner"), siehe CessnaCrewDbContext.

public enum UserRole
{
    Owner,
    RegularGuest
}

public enum TurnType
{
    Private,
    Maintenance
}

public enum RoleOnBoard
{
    Skipper,
    Navigator,
    Deckhand,
    Guest
}

public enum SwapRequestStatus
{
    Open,
    Accepted,
    Rejected
}
