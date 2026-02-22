using System;
using System.Collections.Generic;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend;

public partial class FlottakezeloDbContext : DbContext
{
    public FlottakezeloDbContext()
    {
    }

    public FlottakezeloDbContext(DbContextOptions<FlottakezeloDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CalendarEvent> CalendarEvents { get; set; }

    public virtual DbSet<Driver> Drivers { get; set; }

    public virtual DbSet<Models.File> Files { get; set; }

    public virtual DbSet<FuelLog> FuelLogs { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<ServiceRequest> ServiceRequests { get; set; }

    public virtual DbSet<Trip> Trips { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

    public virtual DbSet<VehicleAssignment> VehicleAssignments { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySQL("Server=localhost;Database=flottakezelo_db;Uid=root;Pwd=;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CalendarEvent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("calendar_events");

            entity.HasIndex(e => e.CreatedByUserId, "fk_ce_created_by");

            entity.HasIndex(e => new { e.OwnerUserId, e.StartAt }, "ix_ce_owner_start");

            entity.HasIndex(e => e.RelatedServiceRequestId, "ix_ce_related_service");

            entity.Property(e => e.Id)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("'current_timestamp()'")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("created_by_user_id");
            entity.Property(e => e.Description)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.EndAt)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("datetime")
                .HasColumnName("end_at");
            entity.Property(e => e.EventType)
                .HasColumnType("enum('PERSONAL_TASK','SERVICE_APPOINTMENT')")
                .HasColumnName("event_type");
            entity.Property(e => e.OwnerUserId)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("owner_user_id");
            entity.Property(e => e.RelatedServiceRequestId)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("related_service_request_id");
            entity.Property(e => e.StartAt)
                .HasColumnType("datetime")
                .HasColumnName("start_at");
            entity.Property(e => e.Title)
                .HasMaxLength(160)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("'current_timestamp()'")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.CalendarEventCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_ce_created_by");

            entity.HasOne(d => d.OwnerUser).WithMany(p => p.CalendarEventOwnerUsers)
                .HasForeignKey(d => d.OwnerUserId)
                .HasConstraintName("fk_ce_owner");

            entity.HasOne(d => d.RelatedServiceRequest).WithMany(p => p.CalendarEvents)
                .HasForeignKey(d => d.RelatedServiceRequestId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_ce_related_service");
        });

        modelBuilder.Entity<Driver>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("drivers");

            entity.HasIndex(e => e.UserId, "uq_drivers_user_id").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.LicenseExpiryDate)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("date")
                .HasColumnName("license_expiry_date");
            entity.Property(e => e.LicenseNumber)
                .HasMaxLength(100)
                .HasColumnName("license_number");
            entity.Property(e => e.Notes)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("text")
                .HasColumnName("notes");
            entity.Property(e => e.UserId)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Driver)
                .HasForeignKey<Driver>(d => d.UserId)
                .HasConstraintName("fk_drivers_user");
        });

        modelBuilder.Entity<Models.File>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("files");

            entity.HasIndex(e => e.UploadedByUserId, "ix_files_uploaded_by");

            entity.Property(e => e.Id)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("'current_timestamp()'")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.MimeType)
                .HasMaxLength(120)
                .HasColumnName("mime_type");
            entity.Property(e => e.OriginalName)
                .HasMaxLength(255)
                .HasColumnName("original_name");
            entity.Property(e => e.SizeBytes)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("size_bytes");
            entity.Property(e => e.StorageProvider)
                .HasDefaultValueSql("'''LOCAL'''")
                .HasColumnType("enum('LOCAL','S3')")
                .HasColumnName("storage_provider");
            entity.Property(e => e.StoredName)
                .HasMaxLength(255)
                .HasColumnName("stored_name");
            entity.Property(e => e.UploadedByUserId)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("uploaded_by_user_id");

            entity.HasOne(d => d.UploadedByUser).WithMany(p => p.Files)
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_files_uploaded_by");
        });

        modelBuilder.Entity<FuelLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("fuel_logs");

            entity.HasIndex(e => new { e.DriverId, e.Date }, "ix_fuel_driver_date");

            entity.HasIndex(e => e.ReceiptFileId, "ix_fuel_receipt");

            entity.HasIndex(e => new { e.VehicleId, e.Date }, "ix_fuel_vehicle_date");

            entity.Property(e => e.Id)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("'current_timestamp()'")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Date)
                .HasColumnType("datetime")
                .HasColumnName("date");
            entity.Property(e => e.DriverId)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("driver_id");
            entity.Property(e => e.Liters)
                .HasPrecision(10)
                .HasColumnName("liters");
            entity.Property(e => e.LocationText)
                .HasMaxLength(255)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("location_text");
            entity.Property(e => e.OdometerKm)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)")
                .HasColumnName("odometer_km");
            entity.Property(e => e.ReceiptFileId)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("receipt_file_id");
            entity.Property(e => e.StationName)
                .HasMaxLength(255)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("station_name");
            entity.Property(e => e.TotalCost)
                .HasPrecision(10)
                .HasColumnName("total_cost");
            entity.Property(e => e.VehicleId)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("vehicle_id");

            entity.HasOne(d => d.Driver).WithMany(p => p.FuelLogs)
                .HasForeignKey(d => d.DriverId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_fuel_driver");

            entity.HasOne(d => d.ReceiptFile).WithMany(p => p.FuelLogs)
                .HasForeignKey(d => d.ReceiptFileId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_fuel_receipt");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.FuelLogs)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_fuel_vehicle");
        });

        modelBuilder.Entity<ServiceRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("service_requests");

            entity.HasIndex(e => e.CreatedByDriverUserId, "fk_sr_created_by_user");

            entity.HasIndex(e => e.AdminUserId, "ix_sr_admin");

            entity.HasIndex(e => new { e.DriverId, e.Status }, "ix_sr_driver_status");

            entity.HasIndex(e => e.InvoiceFileId, "ix_sr_invoice");

            entity.HasIndex(e => new { e.VehicleId, e.Status }, "ix_sr_vehicle_status");

            entity.Property(e => e.Id)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.AdminDecisionNote)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("text")
                .HasColumnName("admin_decision_note");
            entity.Property(e => e.AdminUserId)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("admin_user_id");
            entity.Property(e => e.ClosedAt)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("datetime")
                .HasColumnName("closed_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("'current_timestamp()'")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByDriverUserId)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("created_by_driver_user_id");
            entity.Property(e => e.Description)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.DriverCloseNote)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("text")
                .HasColumnName("driver_close_note");
            entity.Property(e => e.DriverId)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("driver_id");
            entity.Property(e => e.DriverReportCost)
                .HasPrecision(10)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("driver_report_cost");
            entity.Property(e => e.InvoiceFileId)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("invoice_file_id");
            entity.Property(e => e.ScheduledEnd)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("datetime")
                .HasColumnName("scheduled_end");
            entity.Property(e => e.ScheduledStart)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("datetime")
                .HasColumnName("scheduled_start");
            entity.Property(e => e.ServiceLocation)
                .HasMaxLength(255)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("service_location");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'''REQUESTED'''")
                .HasColumnType("enum('REQUESTED','REJECTED','APPROVED','DRIVER_COST','CLOSED')")
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(120)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("'current_timestamp()'")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.VehicleId)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("vehicle_id");

            entity.HasOne(d => d.AdminUser).WithMany(p => p.ServiceRequestAdminUsers)
                .HasForeignKey(d => d.AdminUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_sr_admin_user");

            entity.HasOne(d => d.CreatedByDriverUser).WithMany(p => p.ServiceRequestCreatedByDriverUsers)
                .HasForeignKey(d => d.CreatedByDriverUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_sr_created_by_user");

            entity.HasOne(d => d.Driver).WithMany(p => p.ServiceRequests)
                .HasForeignKey(d => d.DriverId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_sr_driver");

            entity.HasOne(d => d.InvoiceFile).WithMany(p => p.ServiceRequests)
                .HasForeignKey(d => d.InvoiceFileId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_sr_invoice_file");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.ServiceRequests)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_sr_vehicle");
        });

        modelBuilder.Entity<Trip>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("trips");

            entity.HasIndex(e => new { e.DriverId, e.StartTime }, "ix_trips_driver_start");

            entity.HasIndex(e => new { e.VehicleId, e.StartTime }, "ix_trips_vehicle_start");

            entity.Property(e => e.Id)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("'current_timestamp()'")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DistanceKm)
                .HasPrecision(10)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("distance_km");
            entity.Property(e => e.DriverId)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("driver_id");
            entity.Property(e => e.EndLocation)
                .HasMaxLength(255)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("end_location");
            entity.Property(e => e.EndOdometerKm)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)")
                .HasColumnName("end_odometer_km");
            entity.Property(e => e.EndTime)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("datetime")
                .HasColumnName("end_time");
            entity.Property(e => e.Notes)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("text")
                .HasColumnName("notes");
            entity.Property(e => e.StartLocation)
                .HasMaxLength(255)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("start_location");
            entity.Property(e => e.StartOdometerKm)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)")
                .HasColumnName("start_odometer_km");
            entity.Property(e => e.StartTime)
                .HasColumnType("datetime")
                .HasColumnName("start_time");
            entity.Property(e => e.VehicleId)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("vehicle_id");

            entity.HasOne(d => d.Driver).WithMany(p => p.Trips)
                .HasForeignKey(d => d.DriverId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_trips_driver");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.Trips)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_trips_vehicle");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "uq_users_email").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("'current_timestamp()'")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.ProfileImgFileId)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("profile_img_file_id");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("phone");
            entity.Property(e => e.Role)
                .HasColumnType("enum('ADMIN','DRIVER')")
                .HasColumnName("role");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("'current_timestamp()'")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("vehicles");

            entity.HasIndex(e => e.LicensePlate, "uq_vehicles_license_plate").IsUnique();

            entity.HasIndex(e => e.Vin, "uq_vehicles_vin").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Brand)
                .HasMaxLength(80)
                .HasColumnName("brand");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("'current_timestamp()'")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CurrentMileageKm)
                .HasColumnType("int(11)")
                .HasColumnName("current_mileage_km");
            entity.Property(e => e.LicensePlate)
                .HasMaxLength(32)
                .HasColumnName("license_plate");
            entity.Property(e => e.Model)
                .HasMaxLength(80)
                .HasColumnName("model");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'''ACTIVE'''")
                .HasColumnType("enum('ACTIVE','MAINTENANCE','RETIRED')")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("'current_timestamp()'")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.Vin)
                .HasMaxLength(64)
                .HasDefaultValueSql("'NULL'")
                .HasColumnName("vin");
            entity.Property(e => e.Year)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("int(11)")
                .HasColumnName("year");
        });

        modelBuilder.Entity<VehicleAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("vehicle_assignments");

            entity.HasIndex(e => new { e.DriverId, e.AssignedTo }, "ix_va_driver_assigned_to");

            entity.HasIndex(e => new { e.VehicleId, e.AssignedTo }, "ix_va_vehicle_assigned_to");

            entity.Property(e => e.Id)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.AssignedFrom)
                .HasColumnType("datetime")
                .HasColumnName("assigned_from");
            entity.Property(e => e.AssignedTo)
                .HasDefaultValueSql("'NULL'")
                .HasColumnType("datetime")
                .HasColumnName("assigned_to");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("'current_timestamp()'")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DriverId)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("driver_id");
            entity.Property(e => e.VehicleId)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("vehicle_id");

            entity.HasOne(d => d.Driver).WithMany(p => p.VehicleAssignments)
                .HasForeignKey(d => d.DriverId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_va_driver");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.VehicleAssignments)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_va_vehicle");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
