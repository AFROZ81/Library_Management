namespace LibraryPro.Web.Models.Api;

public class ApiMemberDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime MembershipDate { get; set; }
    public bool ReceiveDueDateReminders { get; set; }
    public bool ReceiveOverdueNotices { get; set; }
    public bool ReceiveReservationAlerts { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateMemberDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool ReceiveDueDateReminders { get; set; } = true;
    public bool ReceiveOverdueNotices { get; set; } = true;
    public bool ReceiveReservationAlerts { get; set; } = true;
}

public class UpdateMemberDto
{
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool ReceiveDueDateReminders { get; set; }
    public bool ReceiveOverdueNotices { get; set; }
    public bool ReceiveReservationAlerts { get; set; }
}
