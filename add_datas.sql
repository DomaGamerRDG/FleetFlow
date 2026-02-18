USE flottakezelo_db

INSERT INTO users (id, email, password_hash, role, full_name, phone)
VALUES
(1, 'admin@flotta.hu', '$2a$12$u2fVJvzVrWmp3Z/mwP/76O1VlXC3y7pMPPtFjzzzwwb3WKXE/cT3K', 'ADMIN', 'Kovács Admin', '0612345678'),
(2, 'sofor1@flotta.hu', '$2a$12$2xCTUlvihASTB/P98Qg9i.WPxWxiMEfukTU0V.7V7AHVoCxaQZvEe', 'DRIVER', 'Nagy Péter', '0620123456'),
(3, 'sofor2@flotta.hu', '$2a$12$zfhUsxKfx2V1zt4s59hjcerOhJ6QCseQdSGfX2Iio0eXih7Z4Rvcy', 'DRIVER', 'Kiss Gábor', '0630123456');

INSERT INTO drivers (id, user_id, license_number, license_expiry_date)
VALUES
(1, 2, 'B1234567', '2027-05-20'),
(2, 3, 'C9876543', '2026-11-10');

INSERT INTO vehicles (id, license_plate, brand, model, year, current_mileage_km, status)
VALUES
(1, 'ABC-123', 'Ford', 'Focus', 2019, 78500, 'ACTIVE'),
(2, 'XYZ-987', 'Volkswagen', 'Golf', 2021, 41200, 'ACTIVE');

INSERT INTO vehicle_assignments (vehicle_id, driver_id, assigned_from)
VALUES
(1, 1, '2025-01-01'),
(2, 2, '2025-01-05');

INSERT INTO files (id, uploaded_by_user_id, original_name, stored_name, mime_type, size_bytes)
VALUES
(1, 2, 'tankolas1.jpg', 'fuel_001.jpg', 'image/jpeg', 245000),
(2, 2, 'szerviz_szamla.pdf', 'service_001.pdf', 'application/pdf', 185000);

INSERT INTO fuel_logs 
(vehicle_id, driver_id, date, odometer_km, liters, total_cost, station_name, receipt_file_id)
VALUES
(1, 1, '2025-01-10 08:30:00', 78200, 42.5, 28500, 'OMV Győr', 1),
(2, 2, '2025-01-12 17:10:00', 41000, 38.2, 25800, 'MOL Budapest', NULL);

INSERT INTO trips
(vehicle_id, driver_id, start_time, end_time, start_location, end_location, distance_km, purpose)
VALUES
(1, 1, '2025-01-10 09:00:00', '2025-01-10 11:00:00', 'Győr', 'Budapest', 121.5, 'Ügyféllátogatás'),
(2, 2, '2025-01-12 18:00:00', '2025-01-12 20:00:00', 'Budapest', 'Székesfehérvár', 64.3, 'Áruszállítás');

INSERT INTO service_requests
(
  id, vehicle_id, driver_id, created_by_driver_user_id,
  title, description, priority, status,
  admin_user_id,
  scheduled_start, scheduled_end,
  service_location,
  driver_report_cost,
  invoice_file_id
)
VALUES
(
  1, 1, 1, 2,
  'Fékek ellenőrzése',
  'Fékezésnél furcsa hang hallható',
  'HIGH',
  'SCHEDULED',
  1,
  '2025-01-20 09:00:00',
  '2025-01-20 11:00:00',
  'Győr Autószerviz',
  38500,
  2
);

INSERT INTO calendar_events
(owner_user_id, created_by_user_id, event_type, title, start_at, end_at, related_service_request_id)
VALUES
(2, 1, 'SERVICE_APPOINTMENT', 'Szerviz - fék javítás', '2025-01-20 09:00:00', '2025-01-20 11:00:00', 1);

INSERT INTO notifications
(user_id, type, title, message, related_service_request_id)
VALUES
(2, 'SERVICE_APPROVED', 'Szerviz jóváhagyva', 'A szerviz időpontja rögzítve lett.', 1),
(1, 'SERVICE_CREATED', 'Új szerviz igény', 'Új szerviz igény érkezett egy járműhöz.', 1);
