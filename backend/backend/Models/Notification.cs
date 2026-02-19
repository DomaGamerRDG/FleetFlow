using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class Notification
{
    public ulong Id { get; set; }

    public ulong UserId { get; set; }

    public string Type { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public bool IsRead { get; set; }

    public ulong? RelatedServiceRequestId { get; set; }

    public DateTime CreatedAt { get; set; }
}
