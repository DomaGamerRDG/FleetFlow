using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class ServiceRequest
{
    public ulong Id { get; set; }

    public ulong VehicleId { get; set; }

    public ulong DriverId { get; set; }

    public ulong CreatedByDriverUserId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public ulong? AdminUserId { get; set; }

    public string? AdminDecisionNote { get; set; }

    public DateTime? ScheduledStart { get; set; }

    public DateTime? ScheduledEnd { get; set; }

    public string? ServiceLocation { get; set; }

    public decimal? DriverReportCost { get; set; }

    public ulong? InvoiceFileId { get; set; }

    public string? DriverCloseNote { get; set; }

    public DateTime? ClosedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User? AdminUser { get; set; }

    public virtual ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();

    public virtual User CreatedByDriverUser { get; set; } = null!;

    public virtual Driver Driver { get; set; } = null!;

    public virtual File? InvoiceFile { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Vehicle Vehicle { get; set; } = null!;
}
