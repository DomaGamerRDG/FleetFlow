using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class FuelLog
{
    public ulong Id { get; set; }

    public ulong VehicleId { get; set; }

    public ulong DriverId { get; set; }

    public DateTime Date { get; set; }

    public int? OdometerKm { get; set; }

    public decimal Liters { get; set; }

    public decimal TotalCost { get; set; }

    public string Currency { get; set; } = null!;

    public string? StationName { get; set; }

    public string? LocationText { get; set; }

    public ulong? ReceiptFileId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Driver Driver { get; set; } = null!;

    public virtual File? ReceiptFile { get; set; }

    public virtual Vehicle Vehicle { get; set; } = null!;
}
