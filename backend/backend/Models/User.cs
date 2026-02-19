using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class User
{
    public ulong Id { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public ulong? ProfileImgFileId { get; set; }

    public bool? IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<CalendarEvent> CalendarEventCreatedByUsers { get; set; } = new List<CalendarEvent>();

    public virtual ICollection<CalendarEvent> CalendarEventOwnerUsers { get; set; } = new List<CalendarEvent>();

    public virtual Driver? Driver { get; set; }

    public virtual ICollection<File> Files { get; set; } = new List<File>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<ServiceRequest> ServiceRequestAdminUsers { get; set; } = new List<ServiceRequest>();

    public virtual ICollection<ServiceRequest> ServiceRequestCreatedByDriverUsers { get; set; } = new List<ServiceRequest>();
}
