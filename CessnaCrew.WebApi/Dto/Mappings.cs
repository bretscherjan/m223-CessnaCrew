using CessnaCrew.WebApi.Models;

namespace CessnaCrew.WebApi.Dto;

/// <summary>
/// Umwandlung Entität → DTO an einer zentralen Stelle.
/// Navigation Properties (z. B. Owner, Crew) müssen vorher mit .Include() geladen sein.
/// </summary>
public static class Mappings
{
    public static UserDto ToDto(this User u) =>
        new(u.Id, u.Username, u.Email, u.Role, u.CreatedAt);

    public static bool IsCompleted(this Turn t) => t.Logbook?.IsClosed == true;

    public static TurnDto ToDto(this Turn t) =>
        new(t.Id, t.Title, t.StartDateTime, t.EndDateTime, t.Type, t.OwnerId, t.Owner.Username, t.IsCompleted());

    public static TurnDetailDto ToDetailDto(this Turn t) =>
        new(t.Id, t.Title, t.StartDateTime, t.EndDateTime, t.Type, t.OwnerId, t.Owner.Username,
            t.IsCompleted(), t.IsTokenActive,
            t.CrewMembers.OrderBy(c => c.Id).Select(c => c.ToDto()).ToList());

    public static CrewMemberDto ToDto(this CrewMember c) =>
        new(c.Id, c.DisplayName, c.RoleOnBoard, c.UserId);

    public static SwapRequestDto ToDto(this SwapRequest s, int currentUserId) =>
        new(s.Id, s.RequestedTurn.ToDto(), s.OfferedTurn?.ToDto(), s.RequesterId, s.Requester.Username,
            s.Message, s.Status, s.CreatedAt, IsIncoming: s.RequesterId != currentUserId);

    public static LogbookDto ToDto(this Logbook l) =>
        new(l.Id, l.TurnId, l.IsHighSeaMode, l.EngineHoursStart, l.EngineHoursEnd, l.IsClosed,
            l.Rows.OrderBy(r => r.LoggedAt).Select(r => r.ToDto()).ToList());

    public static LogbookRowDto ToDto(this LogbookRow r) =>
        new(r.Id, r.LoggedAt, r.Latitude, r.Longitude, r.WindForceBft, r.WindDirection,
            r.AirPressureHpa, r.SailConfiguration, r.Remarks, r.CreatedByUserId);

    public static InvoiceDto ToDto(this Invoice i) =>
        new(i.Id, i.Title, i.TotalAmount, i.InvoiceDate, i.PayerId, i.Payer.Username,
            i.Splits.OrderBy(s => s.UserId).Select(s => s.ToDto()).ToList());

    public static InvoiceSplitDto ToDto(this InvoiceSplit s) =>
        new(s.Id, s.UserId, s.User.Username, s.ShareAmount, s.IsSettled);
}
