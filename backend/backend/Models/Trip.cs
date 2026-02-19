using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class Trip
{
    public ulong Id { get; set; }

    public ulong VehicleId { get; set; }

    public ulong DriverId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string? StartLocation { get; set; }

    public string? EndLocation { get; set; }

    public decimal? DistanceKm { get; set; }

    public int? StartOdometerKm { get; set; }

    public int? EndOdometerKm { get; set; }

    public string? Notes { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Driver Driver { get; set; } = null!;

    public virtual Vehicle Vehicle { get; set; } = null!;
}
