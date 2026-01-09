-- EMS Core Database Initialization Script
-- This script sets up the initial database structure and sample data

-- Enable TimescaleDB extension
CREATE EXTENSION IF NOT EXISTS timescaledb;

-- Create initial site
INSERT INTO sites (id, name, location, latitude, longitude, time_zone, description, is_active, created_at, updated_at)
VALUES 
    ('docker-site-001', 'Docker Development Site', 'Local Development Environment', 52.5200, 13.4050, 'Europe/Berlin', 'Default site for Docker development', true, NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

-- Create sample devices
INSERT INTO devices (id, name, type, site_id, manufacturer, model, description, is_active, is_online, created_at, updated_at)
VALUES 
    ('solar-panel-001', 'Solar Panel Array 1', 'Solar Panel', 'docker-site-001', 'SolarTech', 'ST-500W', 'Main solar panel array', true, false, NOW(), NOW()),
    ('battery-001', 'Lithium Battery Bank 1', 'Battery', 'docker-site-001', 'PowerStore', 'PS-10kWh', 'Primary energy storage', true, false, NOW(), NOW()),
    ('inverter-001', 'Grid Tie Inverter 1', 'Inverter', 'docker-site-001', 'InverTech', 'IT-5000W', 'Main grid connection inverter', true, false, NOW(), NOW()),
    ('meter-001', 'Smart Energy Meter 1', 'Energy Meter', 'docker-site-001', 'MeterCorp', 'MC-Smart100', 'Grid connection meter', true, false, NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

-- Insert sample energy measurements (last 24 hours)
DO $$
DECLARE
    device_ids TEXT[] := ARRAY['solar-panel-001', 'battery-001', 'inverter-001', 'meter-001'];
    device_id TEXT;
    measurement_time TIMESTAMP;
    i INTEGER;
    power_value DOUBLE PRECISION;
    voltage_value DOUBLE PRECISION;
    current_value DOUBLE PRECISION;
BEGIN
    -- Generate sample data for the last 24 hours
    FOR i IN 0..1439 LOOP -- 1440 minutes in 24 hours
        measurement_time := NOW() - INTERVAL '24 hours' + (i * INTERVAL '1 minute');
        
        FOREACH device_id IN ARRAY device_ids LOOP
            -- Generate realistic values based on device type and time of day
            CASE 
                WHEN device_id = 'solar-panel-001' THEN
                    -- Solar panel: power varies with time of day (simulated sun)
                    power_value := GREATEST(0, 3000 * SIN(RADIANS((EXTRACT(HOUR FROM measurement_time) - 6) * 15))) + RANDOM() * 200 - 100;
                    voltage_value := 400 + RANDOM() * 50 - 25;
                    current_value := CASE WHEN power_value > 0 THEN power_value / voltage_value ELSE 0 END;
                    
                WHEN device_id = 'battery-001' THEN
                    -- Battery: charging during day, discharging at night
                    power_value := (CASE 
                        WHEN EXTRACT(HOUR FROM measurement_time) BETWEEN 8 AND 16 THEN -(1000 + RANDOM() * 500) -- Charging
                        ELSE 800 + RANDOM() * 400 -- Discharging
                    END);
                    voltage_value := 48 + RANDOM() * 4 - 2;
                    current_value := ABS(power_value) / voltage_value;
                    
                WHEN device_id = 'inverter-001' THEN
                    -- Inverter: converts DC to AC
                    power_value := 1500 + RANDOM() * 1000 - 500;
                    voltage_value := 230 + RANDOM() * 20 - 10;
                    current_value := ABS(power_value) / voltage_value;
                    
                WHEN device_id = 'meter-001' THEN
                    -- Grid meter: measures grid consumption/feed-in
                    power_value := (CASE 
                        WHEN EXTRACT(HOUR FROM measurement_time) BETWEEN 9 AND 15 THEN -(500 + RANDOM() * 1000) -- Feed-in
                        ELSE 1200 + RANDOM() * 800 -- Consumption
                    END);
                    voltage_value := 230 + RANDOM() * 10 - 5;
                    current_value := ABS(power_value) / voltage_value;
            END CASE;
            
            -- Insert power measurement
            INSERT INTO energy_measurements (
                timestamp, device_id, site_id, measurement_type, value, unit, 
                quality_flag, aggregation_level, created_at
            ) VALUES (
                measurement_time, device_id, 'docker-site-001', 2, -- Power
                power_value, 'W', 0, 'raw', NOW()
            );
            
            -- Insert voltage measurement
            INSERT INTO energy_measurements (
                timestamp, device_id, site_id, measurement_type, value, unit, 
                quality_flag, aggregation_level, created_at
            ) VALUES (
                measurement_time, device_id, 'docker-site-001', 0, -- Voltage
                voltage_value, 'V', 0, 'raw', NOW()
            );
            
            -- Insert current measurement
            INSERT INTO energy_measurements (
                timestamp, device_id, site_id, measurement_type, value, unit, 
                quality_flag, aggregation_level, created_at
            ) VALUES (
                measurement_time, device_id, 'docker-site-001', 1, -- Current
                current_value, 'A', 0, 'raw', NOW()
            );
            
            -- Add battery SOC for battery device
            IF device_id = 'battery-001' THEN
                INSERT INTO energy_measurements (
                    timestamp, device_id, site_id, measurement_type, value, unit, 
                    quality_flag, aggregation_level, created_at
                ) VALUES (
                    measurement_time, device_id, 'docker-site-001', 5, -- BatterySOC
                    50 + 30 * SIN(RADIANS(i * 0.25)) + RANDOM() * 10 - 5, '%', 0, 'raw', NOW()
                );
            END IF;
        END LOOP;
    END LOOP;
    
    RAISE NOTICE 'Inserted sample measurements for % devices over 24 hours', array_length(device_ids, 1);
END $$;

-- Create TimescaleDB hypertable (this will be done by the application, but we can try here too)
DO $$
BEGIN
    -- Try to create hypertable, ignore if already exists
    PERFORM create_hypertable('energy_measurements', 'timestamp', if_not_exists => TRUE);
    RAISE NOTICE 'TimescaleDB hypertable created successfully';
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE 'TimescaleDB hypertable creation skipped: %', SQLERRM;
END $$;

-- Create indexes for better query performance
CREATE INDEX IF NOT EXISTS idx_energy_measurements_device_time 
    ON energy_measurements (device_id, timestamp DESC);

CREATE INDEX IF NOT EXISTS idx_energy_measurements_site_time 
    ON energy_measurements (site_id, timestamp DESC);

CREATE INDEX IF NOT EXISTS idx_energy_measurements_type_time 
    ON energy_measurements (measurement_type, timestamp DESC);

-- Update device online status
UPDATE devices SET is_online = true, last_seen_at = NOW() 
WHERE id IN ('solar-panel-001', 'battery-001', 'inverter-001', 'meter-001');

RAISE NOTICE 'EMS Core database initialization completed successfully';