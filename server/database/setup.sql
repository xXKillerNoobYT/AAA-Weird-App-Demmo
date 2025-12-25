-- WeirdToo Parts System - PostgreSQL Database Setup
-- This script initializes the database and runs all migrations
-- Version: 1.0.0

-- Create database (if running standalone; adjust as needed for existing DB)
-- CREATE DATABASE weirdtoo_parts;

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "citext";

-- Create schema
CREATE SCHEMA IF NOT EXISTS app;

-- Set default schema for session
SET search_path TO app, public;

-- Core Tables

-- Users table
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email CITEXT UNIQUE NOT NULL,
    name VARCHAR(255) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_login TIMESTAMP
);

-- Roles table (hierarchical with level)
CREATE TABLE IF NOT EXISTS roles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) UNIQUE NOT NULL,
    description TEXT,
    level INTEGER NOT NULL DEFAULT 0,
    parent_role_id UUID REFERENCES roles(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Departments table
CREATE TABLE IF NOT EXISTS departments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    type VARCHAR(50) NOT NULL, -- 'office', 'warehouse', 'truck', 'job_site'
    location VARCHAR(255),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- User assignments to roles and departments
CREATE TABLE IF NOT EXISTS user_assignments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id),
    role_id UUID NOT NULL REFERENCES roles(id),
    department_id UUID NOT NULL REFERENCES departments(id),
    is_primary BOOLEAN DEFAULT false,
    assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, role_id, department_id)
);

-- Parts Categories (hierarchical)
CREATE TABLE IF NOT EXISTS parts_categories (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    description TEXT,
    level INTEGER NOT NULL DEFAULT 1,
    parent_category_id UUID REFERENCES parts_categories(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(name, parent_category_id)
);

-- Measurement Units
CREATE TABLE IF NOT EXISTS measurement_units (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(50) NOT NULL UNIQUE,
    symbol VARCHAR(20),
    description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Parts (base part definition)
CREATE TABLE IF NOT EXISTS parts (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    sku VARCHAR(50) UNIQUE NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    category_id UUID NOT NULL REFERENCES parts_categories(id),
    measurement_unit_id UUID REFERENCES measurement_units(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Part Variants (specific configurations)
CREATE TABLE IF NOT EXISTS part_variants (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    part_id UUID NOT NULL REFERENCES parts(id),
    variant_name VARCHAR(255) NOT NULL,
    variant_code VARCHAR(50),
    specifications JSONB,
    pricing JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(part_id, variant_code)
);

-- Brands
CREATE TABLE IF NOT EXISTS brands (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL UNIQUE,
    website VARCHAR(255),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Suppliers
CREATE TABLE IF NOT EXISTS suppliers (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL UNIQUE,
    contact_email VARCHAR(255),
    contact_phone VARCHAR(20),
    website VARCHAR(255),
    address VARCHAR(255),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Supplier-Brand Relationships
CREATE TABLE IF NOT EXISTS supplier_brands (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    supplier_id UUID NOT NULL REFERENCES suppliers(id),
    brand_id UUID NOT NULL REFERENCES brands(id),
    lead_time_days INTEGER,
    minimum_order_quantity INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(supplier_id, brand_id)
);

-- Supplier Parts (parts offered by supplier)
CREATE TABLE IF NOT EXISTS supplier_parts (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    supplier_id UUID NOT NULL REFERENCES suppliers(id),
    part_variant_id UUID NOT NULL REFERENCES part_variants(id),
    supplier_sku VARCHAR(50),
    unit_price DECIMAL(10, 2),
    quantity_available INTEGER,
    last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(supplier_id, part_variant_id)
);

-- Warehouses
CREATE TABLE IF NOT EXISTS warehouses (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL UNIQUE,
    location VARCHAR(255),
    department_id UUID REFERENCES departments(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Warehouse Inventory
CREATE TABLE IF NOT EXISTS warehouse_inventory (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    warehouse_id UUID NOT NULL REFERENCES warehouses(id),
    part_variant_id UUID NOT NULL REFERENCES part_variants(id),
    quantity_on_hand INTEGER DEFAULT 0,
    quantity_reserved INTEGER DEFAULT 0,
    reorder_level INTEGER,
    target_level INTEGER,
    last_counted TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(warehouse_id, part_variant_id)
);

-- Trucks
CREATE TABLE IF NOT EXISTS trucks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL UNIQUE,
    truck_number VARCHAR(20),
    capacity_cubic_feet DECIMAL(10, 2),
    department_id UUID REFERENCES departments(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Truck Inventory
CREATE TABLE IF NOT EXISTS truck_inventory (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    truck_id UUID NOT NULL REFERENCES trucks(id),
    part_variant_id UUID NOT NULL REFERENCES part_variants(id),
    quantity INTEGER DEFAULT 0,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(truck_id, part_variant_id)
);

-- Job Sites
CREATE TABLE IF NOT EXISTS job_sites (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    address VARCHAR(255),
    location_lat DECIMAL(10, 8),
    location_lon DECIMAL(11, 8),
    department_id UUID REFERENCES departments(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Parts Lists
CREATE TABLE IF NOT EXISTS parts_lists (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    job_site_id UUID REFERENCES job_sites(id),
    created_by_user_id UUID REFERENCES users(id),
    status VARCHAR(50) DEFAULT 'draft', -- draft, submitted, approved, rejected
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    submitted_at TIMESTAMP,
    approved_at TIMESTAMP,
    rejected_at TIMESTAMP
);

-- Parts List Items
CREATE TABLE IF NOT EXISTS parts_list_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    parts_list_id UUID NOT NULL REFERENCES parts_lists(id),
    part_variant_id UUID NOT NULL REFERENCES part_variants(id),
    quantity_needed INTEGER NOT NULL,
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Supplier Orders
CREATE TABLE IF NOT EXISTS supplier_orders (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    supplier_id UUID NOT NULL REFERENCES suppliers(id),
    job_sites_id UUID REFERENCES job_sites(id),
    total_cost DECIMAL(12, 2),
    status VARCHAR(50) DEFAULT 'draft', -- draft, submitted, confirmed, shipped, received
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    submitted_at TIMESTAMP,
    confirmed_at TIMESTAMP
);

-- Supplier Order Lines
CREATE TABLE IF NOT EXISTS supplier_order_lines (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    supplier_order_id UUID NOT NULL REFERENCES supplier_orders(id),
    part_variant_id UUID NOT NULL REFERENCES part_variants(id),
    quantity INTEGER NOT NULL,
    unit_price DECIMAL(10, 2),
    line_total DECIMAL(12, 2),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Audit Log
CREATE TABLE IF NOT EXISTS audit_log (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES users(id),
    entity_type VARCHAR(50),
    entity_id UUID,
    action VARCHAR(50), -- 'create', 'update', 'delete', 'approve', 'reject'
    changes JSONB,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create Indexes
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_user_assignments_user_id ON user_assignments(user_id);
CREATE INDEX idx_user_assignments_role_id ON user_assignments(role_id);
CREATE INDEX idx_user_assignments_department_id ON user_assignments(department_id);
CREATE INDEX idx_parts_category_id ON parts(category_id);
CREATE INDEX idx_part_variants_part_id ON part_variants(part_id);
CREATE INDEX idx_supplier_brands_supplier_id ON supplier_brands(supplier_id);
CREATE INDEX idx_supplier_brands_brand_id ON supplier_brands(brand_id);
CREATE INDEX idx_supplier_parts_supplier_id ON supplier_parts(supplier_id);
CREATE INDEX idx_warehouse_inventory_warehouse_id ON warehouse_inventory(warehouse_id);
CREATE INDEX idx_warehouse_inventory_part_variant_id ON warehouse_inventory(part_variant_id);
CREATE INDEX idx_truck_inventory_truck_id ON truck_inventory(truck_id);
CREATE INDEX idx_truck_inventory_part_variant_id ON truck_inventory(part_variant_id);
CREATE INDEX idx_parts_lists_job_site_id ON parts_lists(job_site_id);
CREATE INDEX idx_parts_lists_created_by ON parts_lists(created_by_user_id);
CREATE INDEX idx_parts_lists_status ON parts_lists(status);
CREATE INDEX idx_parts_list_items_parts_list_id ON parts_list_items(parts_list_id);
CREATE INDEX idx_supplier_orders_supplier_id ON supplier_orders(supplier_id);
CREATE INDEX idx_supplier_orders_status ON supplier_orders(status);
CREATE INDEX idx_audit_log_user_id ON audit_log(user_id);
CREATE INDEX idx_audit_log_entity ON audit_log(entity_type, entity_id);
CREATE INDEX idx_audit_log_timestamp ON audit_log(timestamp);

-- JSONB indexes for full-text search and performance
CREATE INDEX idx_part_variants_specs ON part_variants USING GIN (specifications);
CREATE INDEX idx_supplier_parts_pricing ON supplier_parts USING GIN (unit_price::text::jsonb);
CREATE INDEX idx_audit_log_changes ON audit_log USING GIN (changes);

-- Create updated_at trigger function
CREATE OR REPLACE FUNCTION update_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply triggers to timestamp columns
CREATE TRIGGER trg_users_updated_at BEFORE UPDATE ON users FOR EACH ROW EXECUTE FUNCTION update_timestamp();
CREATE TRIGGER trg_roles_updated_at BEFORE UPDATE ON roles FOR EACH ROW EXECUTE FUNCTION update_timestamp();
CREATE TRIGGER trg_departments_updated_at BEFORE UPDATE ON departments FOR EACH ROW EXECUTE FUNCTION update_timestamp();
CREATE TRIGGER trg_parts_updated_at BEFORE UPDATE ON parts FOR EACH ROW EXECUTE FUNCTION update_timestamp();
CREATE TRIGGER trg_part_variants_updated_at BEFORE UPDATE ON part_variants FOR EACH ROW EXECUTE FUNCTION update_timestamp();
CREATE TRIGGER trg_warehouses_updated_at BEFORE UPDATE ON warehouses FOR EACH ROW EXECUTE FUNCTION update_timestamp();
CREATE TRIGGER trg_warehouse_inventory_updated_at BEFORE UPDATE ON warehouse_inventory FOR EACH ROW EXECUTE FUNCTION update_timestamp();
CREATE TRIGGER trg_trucks_updated_at BEFORE UPDATE ON trucks FOR EACH ROW EXECUTE FUNCTION update_timestamp();
CREATE TRIGGER trg_job_sites_updated_at BEFORE UPDATE ON job_sites FOR EACH ROW EXECUTE FUNCTION update_timestamp();
CREATE TRIGGER trg_supplier_parts_updated_at BEFORE UPDATE ON supplier_parts FOR EACH ROW EXECUTE FUNCTION update_timestamp();

-- Migration versioning table
CREATE TABLE IF NOT EXISTS schema_versions (
    version_id INTEGER PRIMARY KEY,
    description VARCHAR(255),
    installed_on TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    execution_time_ms INTEGER
);

-- Record this migration as v1
INSERT INTO schema_versions (version_id, description, execution_time_ms) 
VALUES (1, 'Initial schema with all core tables', 0)
ON CONFLICT DO NOTHING;
