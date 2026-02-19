CREATE DATABASE flottakezelo_db
	CHARACTER SET utf8mb4
	COLLATE utf8mb4_general_ci;

USE flottakezelo_db
-- -------------------------
-- 1) USERS
-- -------------------------
CREATE TABLE IF NOT EXISTS users (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  email VARCHAR(255) NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  role ENUM('ADMIN','DRIVER') NOT NULL,
  full_name VARCHAR(255) NOT NULL,
  phone VARCHAR(50) NULL,
  profile_img_file_id BIGINT UNSIGNED NULL,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_users_email (email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -------------------------
-- 2) DRIVERS (1-1 with users)
-- -------------------------
CREATE TABLE IF NOT EXISTS drivers (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  user_id BIGINT UNSIGNED NOT NULL,
  license_number VARCHAR(100) NOT NULL,
  license_expiry_date DATE NULL,
  notes TEXT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_drivers_user_id (user_id),
  CONSTRAINT fk_drivers_user
    FOREIGN KEY (user_id) REFERENCES users(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -------------------------
-- 3) VEHICLES
-- -------------------------
CREATE TABLE IF NOT EXISTS vehicles (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  license_plate VARCHAR(32) NOT NULL,
  brand VARCHAR(80) NOT NULL,
  model VARCHAR(80) NOT NULL,
  year INT NULL,
  vin VARCHAR(64) NULL,
  current_mileage_km INT NOT NULL DEFAULT 0,
  status ENUM('ACTIVE','MAINTENANCE','RETIRED') NOT NULL DEFAULT 'ACTIVE',
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_vehicles_license_plate (license_plate),
  UNIQUE KEY uq_vehicles_vin (vin)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -------------------------
-- 4) VEHICLE_ASSIGNMENTS (history of who used which car)
-- -------------------------
CREATE TABLE IF NOT EXISTS vehicle_assignments (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  vehicle_id BIGINT UNSIGNED NOT NULL,
  driver_id BIGINT UNSIGNED NOT NULL,
  assigned_from DATETIME NOT NULL,
  assigned_to DATETIME NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY ix_va_vehicle_assigned_to (vehicle_id, assigned_to),
  KEY ix_va_driver_assigned_to (driver_id, assigned_to),
  CONSTRAINT fk_va_vehicle
    FOREIGN KEY (vehicle_id) REFERENCES vehicles(id)
    ON DELETE RESTRICT
    ON UPDATE CASCADE,
  CONSTRAINT fk_va_driver
    FOREIGN KEY (driver_id) REFERENCES drivers(id)
    ON DELETE RESTRICT
    ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -------------------------
-- 5) FILES (receipts, invoices, photos)
-- -------------------------
CREATE TABLE IF NOT EXISTS files (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  uploaded_by_user_id BIGINT UNSIGNED NOT NULL,
  original_name VARCHAR(255) NOT NULL,
  stored_name VARCHAR(255) NOT NULL,
  mime_type VARCHAR(120) NOT NULL,
  size_bytes BIGINT UNSIGNED NOT NULL,
  storage_provider ENUM('LOCAL','S3') NOT NULL DEFAULT 'LOCAL',
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY ix_files_uploaded_by (uploaded_by_user_id),
  CONSTRAINT fk_files_uploaded_by
    FOREIGN KEY (uploaded_by_user_id) REFERENCES users(id)
    ON DELETE RESTRICT
    ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -------------------------
-- 6) FUEL_LOGS (driver adds fuel entries)
-- -------------------------
CREATE TABLE IF NOT EXISTS fuel_logs (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  vehicle_id BIGINT UNSIGNED NOT NULL,
  driver_id BIGINT UNSIGNED NOT NULL,
  date DATETIME NOT NULL,
  odometer_km INT NULL,
  liters DECIMAL(10,2) NOT NULL,
  total_cost DECIMAL(10,2) NOT NULL,
  currency CHAR(3) NOT NULL DEFAULT 'HUF',
  station_name VARCHAR(255) NULL,
  location_text VARCHAR(255) NULL,
  receipt_file_id BIGINT UNSIGNED NULL,
  is_deleted TINYINT(1) NOT NULL DEFAULT 0,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY ix_fuel_vehicle_date (vehicle_id, date),
  KEY ix_fuel_driver_date (driver_id, date),
  KEY ix_fuel_receipt (receipt_file_id),
  CONSTRAINT fk_fuel_vehicle
    FOREIGN KEY (vehicle_id) REFERENCES vehicles(id)
    ON DELETE RESTRICT
    ON UPDATE CASCADE,
  CONSTRAINT fk_fuel_driver
    FOREIGN KEY (driver_id) REFERENCES drivers(id)
    ON DELETE RESTRICT
    ON UPDATE CASCADE,
  CONSTRAINT fk_fuel_receipt
    FOREIGN KEY (receipt_file_id) REFERENCES files(id)
    ON DELETE SET NULL
    ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -------------------------
-- 7) TRIPS (driver adds trips)
-- -------------------------
CREATE TABLE IF NOT EXISTS trips (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  vehicle_id BIGINT UNSIGNED NOT NULL,
  driver_id BIGINT UNSIGNED NOT NULL,
  start_time DATETIME NOT NULL,
  end_time DATETIME NULL,
  start_location VARCHAR(255) NULL,
  end_location VARCHAR(255) NULL,
  distance_km DECIMAL(10,2) NULL,
  start_odometer_km INT NULL,
  end_odometer_km INT NULL,
  notes TEXT NULL,
  is_deleted TINYINT(1) NOT NULL DEFAULT 0,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY ix_trips_vehicle_start (vehicle_id, start_time),
  KEY ix_trips_driver_start (driver_id, start_time),
  CONSTRAINT fk_trips_vehicle
    FOREIGN KEY (vehicle_id) REFERENCES vehicles(id)
    ON DELETE RESTRICT
    ON UPDATE CASCADE,
  CONSTRAINT fk_trips_driver
    FOREIGN KEY (driver_id) REFERENCES drivers(id)
    ON DELETE RESTRICT
    ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -------------------------
-- 8) SERVICE_REQUESTS (full lifecycle)
-- -------------------------
CREATE TABLE IF NOT EXISTS service_requests (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  vehicle_id BIGINT UNSIGNED NOT NULL,
  driver_id BIGINT UNSIGNED NOT NULL,

  created_by_driver_user_id BIGINT UNSIGNED NOT NULL,

  title VARCHAR(120) NOT NULL,
  description TEXT NULL,
  priority ENUM('LOW','MEDIUM','HIGH') NOT NULL DEFAULT 'MEDIUM',

  status ENUM(
    'REQUESTED',
    'REJECTED',
    'APPROVED',
    'SCHEDULED',
    'WAITING_DRIVER_COST',
    'CLOSED'
  ) NOT NULL DEFAULT 'REQUESTED',

  admin_user_id BIGINT UNSIGNED NULL,
  admin_decision_note TEXT NULL,

  scheduled_start DATETIME NULL,
  scheduled_end DATETIME NULL,
  service_location VARCHAR(255) NULL,

  completed_at DATETIME NULL,

  driver_report_cost DECIMAL(10,2) NULL,
  driver_report_currency CHAR(3) NOT NULL DEFAULT 'HUF',
  invoice_file_id BIGINT UNSIGNED NULL,
  driver_close_note TEXT NULL,

  closed_at DATETIME NULL,

  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

  PRIMARY KEY (id),
  KEY ix_sr_vehicle_status (vehicle_id, status),
  KEY ix_sr_driver_status (driver_id, status),
  KEY ix_sr_admin (admin_user_id),
  KEY ix_sr_invoice (invoice_file_id),
  CONSTRAINT fk_sr_vehicle
    FOREIGN KEY (vehicle_id) REFERENCES vehicles(id)
    ON DELETE RESTRICT
    ON UPDATE CASCADE,
  CONSTRAINT fk_sr_driver
    FOREIGN KEY (driver_id) REFERENCES drivers(id)
    ON DELETE RESTRICT
    ON UPDATE CASCADE,
  CONSTRAINT fk_sr_created_by_user
    FOREIGN KEY (created_by_driver_user_id) REFERENCES users(id)
    ON DELETE RESTRICT
    ON UPDATE CASCADE,
  CONSTRAINT fk_sr_admin_user
    FOREIGN KEY (admin_user_id) REFERENCES users(id)
    ON DELETE SET NULL
    ON UPDATE CASCADE,
  CONSTRAINT fk_sr_invoice_file
    FOREIGN KEY (invoice_file_id) REFERENCES files(id)
    ON DELETE SET NULL
    ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -------------------------
-- 9) CALENDAR_EVENTS (driver/admin calendars, includes service appointments)
-- -------------------------
CREATE TABLE IF NOT EXISTS calendar_events (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  owner_user_id BIGINT UNSIGNED NOT NULL,
  created_by_user_id BIGINT UNSIGNED NOT NULL,

  event_type ENUM('PERSONAL_TASK','ADMIN_TASK','SERVICE_APPOINTMENT') NOT NULL,

  title VARCHAR(160) NOT NULL,
  description TEXT NULL,

  start_at DATETIME NOT NULL,
  end_at DATETIME NULL,

  status ENUM('PLANNED','DONE','CANCELLED') NOT NULL DEFAULT 'PLANNED',

  related_service_request_id BIGINT UNSIGNED NULL,

  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

  PRIMARY KEY (id),
  KEY ix_ce_owner_start (owner_user_id, start_at),
  KEY ix_ce_related_service (related_service_request_id),
  CONSTRAINT fk_ce_owner
    FOREIGN KEY (owner_user_id) REFERENCES users(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT fk_ce_created_by
    FOREIGN KEY (created_by_user_id) REFERENCES users(id)
    ON DELETE RESTRICT
    ON UPDATE CASCADE,
  CONSTRAINT fk_ce_related_service
    FOREIGN KEY (related_service_request_id) REFERENCES service_requests(id)
    ON DELETE SET NULL
    ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -------------------------
-- 10) NOTIFICATIONS
-- -------------------------
CREATE TABLE IF NOT EXISTS notifications (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  user_id BIGINT UNSIGNED NOT NULL,

  type VARCHAR(50) NOT NULL,
  title VARCHAR(160) NOT NULL,
  message TEXT NOT NULL,

  is_read TINYINT(1) NOT NULL DEFAULT 0,

  related_service_request_id BIGINT UNSIGNED NULL,

  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

  PRIMARY KEY (id),
  KEY ix_notif_user_read (user_id, is_read, created_at),
  KEY ix_notif_related_service (related_service_request_id),
  CONSTRAINT fk_notif_user
    FOREIGN KEY (user_id) REFERENCES users(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT fk_notif_related_service
    FOREIGN KEY (related_service_request_id) REFERENCES service_requests(id)
    ON DELETE SET NULL
    ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

