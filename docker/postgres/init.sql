-- ============================================================
-- Shopfloor Manager — PostgreSQL Schema
-- Version: 1.0.0
-- ============================================================

-- ============================================================
-- ENUMs
-- ============================================================
CREATE TYPE file_status      AS ENUM ('pending', 'approved', 'rejected');
CREATE TYPE ncr_action       AS ENUM ('pending', 'approve', 'rework', 'reject');
CREATE TYPE ncr_status       AS ENUM ('open', 'closed');
CREATE TYPE measure_result   AS ENUM ('pass', 'fail');
CREATE TYPE borrow_status    AS ENUM ('active', 'returned', 'cancelled');
CREATE TYPE calib_req_status AS ENUM ('pending', 'approved', 'completed', 'cancelled');

-- ============================================================
-- 1. ORGANIZATION
-- ============================================================

CREATE TABLE departments (
    id         SERIAL PRIMARY KEY,
    code       VARCHAR(20) UNIQUE NOT NULL,
    name       VARCHAR(100) NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE user_types (
    id              SERIAL PRIMARY KEY,
    type_name       VARCHAR(30) UNIQUE NOT NULL,
    description     VARCHAR(100),
    can_enter_value BOOLEAN DEFAULT FALSE,
    can_raise_ncr   BOOLEAN DEFAULT FALSE
);

CREATE TABLE positions (
    id          SERIAL PRIMARY KEY,
    code        VARCHAR(50) UNIQUE NOT NULL,
    description VARCHAR(100),
    is_active   BOOLEAN DEFAULT TRUE
);

CREATE TABLE work_statuses (
    id         SERIAL PRIMARY KEY,
    name       VARCHAR(50) NOT NULL,
    is_working BOOLEAN DEFAULT TRUE
);

-- ============================================================
-- 2. ROLES & PERMISSIONS
-- ============================================================

CREATE TABLE roles (
    id   SERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL
);

-- PDM menu (3 cấp: menu → submenu → endmenu)
CREATE TABLE menus (
    id         SERIAL PRIMARY KEY,
    code       VARCHAR(50) UNIQUE NOT NULL,
    name       VARCHAR(100) NOT NULL,
    parent_id  INT REFERENCES menus(id),
    level      SMALLINT NOT NULL CHECK (level BETWEEN 1 AND 3),
    sort_order INT DEFAULT 0
);

CREATE TABLE role_menus (
    id      SERIAL PRIMARY KEY,
    role_id INT NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    menu_id INT NOT NULL REFERENCES menus(id) ON DELETE CASCADE,
    UNIQUE(role_id, menu_id)
);

-- MES menu (desktop app tại máy CNC)
CREATE TABLE mes_menus (
    id          SERIAL PRIMARY KEY,
    code        VARCHAR(50) UNIQUE NOT NULL,
    description VARCHAR(100),
    image_path  VARCHAR(500),
    sort_order  INT DEFAULT 0,
    menu_type   VARCHAR(100),
    is_active   BOOLEAN DEFAULT TRUE
);

CREATE TABLE mes_role_menus (
    id        SERIAL PRIMARY KEY,
    role_name VARCHAR(200) NOT NULL,
    menu_ids  INT[]
);

-- ============================================================
-- 3. USERS
-- ============================================================

CREATE TABLE users (
    id             SERIAL PRIMARY KEY,
    user_login     VARCHAR(50) UNIQUE NOT NULL,
    password_hash  VARCHAR(255) NOT NULL,
    name           VARCHAR(100) NOT NULL,
    sex            VARCHAR(10),
    email          VARCHAR(100),
    user_type_id   INT REFERENCES user_types(id),
    position_id    INT REFERENCES positions(id),
    work_status_id INT REFERENCES work_statuses(id),
    role_id        INT REFERENCES roles(id),
    mes_role_id    INT REFERENCES mes_role_menus(id),
    first_login    BOOLEAN DEFAULT TRUE,
    reset_code     VARCHAR(10),
    is_active      BOOLEAN DEFAULT TRUE,
    created_at     TIMESTAMPTZ DEFAULT NOW(),
    updated_at     TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE audit_logs (
    id             BIGSERIAL PRIMARY KEY,
    user_id        INT REFERENCES users(id),
    machine_id     INT,
    ip_address     VARCHAR(45),
    logged_in_at   TIMESTAMPTZ,
    logged_out_at  TIMESTAMPTZ,
    log_date       DATE GENERATED ALWAYS AS (logged_in_at::DATE) STORED
);

-- ============================================================
-- 4. FACTORY & MACHINES
-- ============================================================

CREATE TABLE factories (
    id           SERIAL PRIMARY KEY,
    name         VARCHAR(200) NOT NULL,
    address      VARCHAR(500),
    phone_number VARCHAR(50),
    created_at   TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE machine_groups (
    id   SERIAL PRIMARY KEY,
    code VARCHAR(45) UNIQUE,
    name VARCHAR(100) NOT NULL
);

CREATE TABLE machines (
    id                       SERIAL PRIMARY KEY,
    code                     VARCHAR(50) UNIQUE NOT NULL,
    name                     VARCHAR(100),
    machine_type             VARCHAR(50),
    serial_number            VARCHAR(200),
    power_source             VARCHAR(50),
    rate_capacity            VARCHAR(50),
    full_load_current        VARCHAR(50),
    electric_drawing_edition VARCHAR(50),
    net_weight               VARCHAR(50),
    date_of_manufacture      DATE,
    max_od   VARCHAR(50),
    length   VARCHAR(50),
    dia      VARCHAR(50),
    travel_x VARCHAR(50),
    travel_y VARCHAR(50),
    travel_z VARCHAR(50),
    travel_ab VARCHAR(50),
    travel_c  VARCHAR(50),
    factory_id       INT REFERENCES factories(id),
    machine_group_id INT REFERENCES machine_groups(id),
    is_cnc           BOOLEAN DEFAULT FALSE,
    is_active        BOOLEAN DEFAULT TRUE,
    created_at       TIMESTAMPTZ DEFAULT NOW()
);

-- Cấu hình serial port per-PC (cho Desktop MES)
CREATE TABLE machine_configs (
    id           SERIAL PRIMARY KEY,
    pc_name      VARCHAR(200),
    machine_id   INT NOT NULL REFERENCES machines(id),
    comm_port    VARCHAR(20),
    baud_rate    INT,
    data_bits    SMALLINT,
    parity       VARCHAR(10),
    stop_bits    VARCHAR(10),
    flow_control VARCHAR(20),
    updated_by   INT REFERENCES users(id),
    updated_at   TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(machine_id)
);

CREATE TABLE machine_events (
    id            BIGSERIAL PRIMARY KEY,
    machine_id    INT NOT NULL REFERENCES machines(id),
    created_by    INT REFERENCES users(id),
    created_at    TIMESTAMPTZ DEFAULT NOW(),
    tm_mode       TEXT,
    at_mode       TEXT,
    run_mode      TEXT,
    alarm         TEXT,
    alarm_message TEXT
);

-- ============================================================
-- 5. PRODUCTION CORE
-- ============================================================

CREATE TABLE op_types (
    id          SERIAL PRIMARY KEY,
    code        VARCHAR(50) UNIQUE NOT NULL,
    name        VARCHAR(100),
    description VARCHAR(200)
);

-- Link MES menu → op_type
CREATE TABLE mes_menu_op_types (
    id           SERIAL PRIMARY KEY,
    mes_menu_id  INT NOT NULL REFERENCES mes_menus(id),
    op_type_id   INT NOT NULL REFERENCES op_types(id),
    file_type_id INT,
    UNIQUE(mes_menu_id, op_type_id)
);

CREATE TABLE po_lines (
    id             SERIAL PRIMARY KEY,
    po_number      VARCHAR(30),
    po_line_number VARCHAR(20),
    customer_id    INT
);

CREATE TABLE parts (
    id               SERIAL PRIMARY KEY,
    part_number      VARCHAR(20) NOT NULL,
    description      VARCHAR(300) NOT NULL,
    revision         VARCHAR(10),
    routing_revision VARCHAR(100),
    status           SMALLINT DEFAULT 0,
    is_active        BOOLEAN DEFAULT TRUE,
    is_complete      BOOLEAN DEFAULT FALSE,
    created_at       TIMESTAMPTZ DEFAULT NOW(),
    updated_at       TIMESTAMPTZ DEFAULT NOW(),
    created_by       INT REFERENCES users(id),
    updated_by       INT REFERENCES users(id),
    confirmed_by     INT REFERENCES users(id),
    confirmed_at     TIMESTAMPTZ,
    completed_by     INT REFERENCES users(id),
    UNIQUE(part_number, revision)
);

CREATE TABLE jobs (
    id          SERIAL PRIMARY KEY,
    job_number  VARCHAR(20) UNIQUE NOT NULL,
    run_qty     INT,
    ship_by     DATE,
    part_id     INT NOT NULL REFERENCES parts(id),
    po_line_id  INT REFERENCES po_lines(id),
    created_at  TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE part_ops (
    id               SERIAL PRIMARY KEY,
    op_number        VARCHAR(10) NOT NULL,
    op_number_sort   DECIMAL(8,2),
    part_id          INT REFERENCES parts(id),
    op_type_id       INT REFERENCES op_types(id),
    job_id           INT REFERENCES jobs(id),
    is_for_job_only  BOOLEAN DEFAULT FALSE,
    description      TEXT,
    note             TEXT,
    setup_time       DECIMAL(8,2),
    prod_time        DECIMAL(8,2),
    is_visible       BOOLEAN DEFAULT TRUE,
    is_complete      BOOLEAN DEFAULT FALSE,
    completed_by     INT REFERENCES users(id),
    created_by       INT REFERENCES users(id),
    created_at       TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE part_op_logs (
    id               BIGSERIAL PRIMARY KEY,
    part_op_id       INT NOT NULL REFERENCES part_ops(id),
    action           TEXT,
    user_id          INT REFERENCES users(id),
    tech_document_id BIGINT,
    part_id          INT REFERENCES parts(id),
    created_at       TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE products (
    id            SERIAL PRIMARY KEY,
    serial_number VARCHAR(10) NOT NULL,
    job_id        INT NOT NULL REFERENCES jobs(id),
    is_complete   BOOLEAN DEFAULT FALSE,
    sort_order    INT,
    created_at    TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(serial_number, job_id)
);

CREATE TABLE production_events (
    id          BIGSERIAL PRIMARY KEY,
    job_id      INT REFERENCES jobs(id),
    part_op_id  INT REFERENCES part_ops(id),
    product_id  INT REFERENCES products(id),
    machine_id  INT REFERENCES machines(id),
    created_by  INT REFERENCES users(id),
    started_at  TIMESTAMPTZ,
    ended_at    TIMESTAMPTZ,
    duration    INTERVAL GENERATED ALWAYS AS (ended_at - started_at) STORED,
    action      VARCHAR(500),
    finished_by INT REFERENCES users(id)
);

-- ============================================================
-- 6. FILE TYPES & TECHNICAL DOCUMENTS
-- ============================================================

CREATE TABLE file_types (
    id                   SERIAL PRIMARY KEY,
    name                 VARCHAR(200) NOT NULL,
    code                 VARCHAR(50) UNIQUE NOT NULL,
    folder               VARCHAR(100),
    is_gcode             BOOLEAN DEFAULT FALSE,
    is_segment           BOOLEAN DEFAULT FALSE,
    requires_job_number  BOOLEAN DEFAULT TRUE,
    requires_part_number BOOLEAN DEFAULT TRUE,
    requires_op_number   BOOLEAN DEFAULT FALSE,
    requires_revision    BOOLEAN DEFAULT FALSE,
    sort_order           INT DEFAULT 0
);

CREATE TABLE tech_documents (
    id           BIGSERIAL PRIMARY KEY,
    file_type_id INT NOT NULL REFERENCES file_types(id),
    job_id       INT REFERENCES jobs(id),
    part_id      INT REFERENCES parts(id),
    part_op_id   INT REFERENCES part_ops(id),
    storage_path VARCHAR(500) NOT NULL,
    description  VARCHAR(500),
    revision     VARCHAR(50),
    code         VARCHAR(100),
    segment      VARCHAR(100),
    status       file_status DEFAULT 'pending',
    inspector_id INT REFERENCES users(id),
    inspected_at TIMESTAMPTZ,
    inspect_note VARCHAR(500),
    created_by   INT NOT NULL REFERENCES users(id),
    created_at   TIMESTAMPTZ DEFAULT NOW(),
    deleted_at   TIMESTAMPTZ
);

CREATE INDEX idx_td_job    ON tech_documents(job_id)     WHERE deleted_at IS NULL;
CREATE INDEX idx_td_op     ON tech_documents(part_op_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_td_status ON tech_documents(status)     WHERE deleted_at IS NULL;

CREATE TABLE file_logs (
    id               BIGSERIAL PRIMARY KEY,
    tech_document_id BIGINT REFERENCES tech_documents(id),
    action           VARCHAR(100) NOT NULL,
    user_id          INT REFERENCES users(id),
    note             TEXT,
    created_at       TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE gcode_library (
    id                  SERIAL PRIMARY KEY,
    name                VARCHAR(500) NOT NULL,
    file_name           VARCHAR(200),
    revision            VARCHAR(50),
    description         TEXT,
    storage_path        VARCHAR(500),
    fixture_category_id INT,
    status              file_status DEFAULT 'pending',
    inspector_id        INT REFERENCES users(id),
    inspected_at        TIMESTAMPTZ,
    created_by          INT REFERENCES users(id),
    created_at          TIMESTAMPTZ DEFAULT NOW(),
    updated_by          INT REFERENCES users(id),
    updated_at          TIMESTAMPTZ DEFAULT NOW(),
    deleted_at          TIMESTAMPTZ
);

CREATE TABLE gcode_library_logs (
    id          SERIAL PRIMARY KEY,
    gcode_id    INT REFERENCES gcode_library(id),
    machine_id  INT REFERENCES machines(id),
    action      VARCHAR(500),
    created_by  INT REFERENCES users(id),
    created_at  TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE gcode_receive_logs (
    id           SERIAL PRIMARY KEY,
    job_id       INT REFERENCES jobs(id),
    part_op_id   INT REFERENCES part_ops(id),
    machine_id   INT REFERENCES machines(id),
    user_id      INT REFERENCES users(id),
    storage_path VARCHAR(500),
    created_at   TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE document_types (
    id   SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL
);

CREATE TABLE documents (
    id               SERIAL PRIMARY KEY,
    name_vn          VARCHAR(500),
    name_en          VARCHAR(500),
    document_type_id INT REFERENCES document_types(id),
    code_no          VARCHAR(100),
    revision         VARCHAR(50),
    department_id    INT REFERENCES departments(id),
    effective_date   DATE,
    storage_path     VARCHAR(500),
    status           file_status DEFAULT 'pending',
    inspector_id     INT REFERENCES users(id),
    inspected_at     TIMESTAMPTZ,
    created_by       INT REFERENCES users(id),
    created_at       TIMESTAMPTZ DEFAULT NOW(),
    deleted_at       TIMESTAMPTZ
);

-- ============================================================
-- 7. DIMENSIONS & MEASUREMENT
-- ============================================================

CREATE TABLE dimension_categories (
    id          SERIAL PRIMARY KEY,
    code        VARCHAR(20) UNIQUE NOT NULL,
    name        VARCHAR(100) NOT NULL,
    description VARCHAR(200)
);

CREATE TABLE dimensions (
    id              BIGSERIAL PRIMARY KEY,
    part_op_id      INT NOT NULL REFERENCES part_ops(id),
    balloon_number  VARCHAR(20) NOT NULL,
    balloon_sort    DECIMAL(8,2),
    -- Kích thước số
    nominal_value   DECIMAL(14,4),
    max_value       DECIMAL(14,4),
    min_value       DECIMAL(14,4),
    tolerance_plus  DECIMAL(14,4),
    tolerance_minus DECIMAL(14,4),
    -- Kích thước text (ren, ký hiệu đặc biệt)
    nominal_text    VARCHAR(100),
    is_text_type    BOOLEAN DEFAULT FALSE,
    category_id     INT REFERENCES dimension_categories(id),
    is_final        BOOLEAN DEFAULT FALSE,
    created_by      INT REFERENCES users(id),
    created_at      TIMESTAMPTZ DEFAULT NOW(),
    updated_by      INT REFERENCES users(id),
    updated_at      TIMESTAMPTZ DEFAULT NOW(),
    deleted_at      TIMESTAMPTZ,
    UNIQUE(part_op_id, balloon_number)
);

CREATE INDEX idx_dim_part_op ON dimensions(part_op_id) WHERE deleted_at IS NULL;

CREATE TABLE dimension_history (
    id              BIGSERIAL PRIMARY KEY,
    dimension_id    BIGINT NOT NULL REFERENCES dimensions(id),
    nominal_value   DECIMAL(14,4),
    max_value       DECIMAL(14,4),
    min_value       DECIMAL(14,4),
    tolerance_plus  DECIMAL(14,4),
    tolerance_minus DECIMAL(14,4),
    nominal_text    VARCHAR(100),
    category_id     INT,
    is_final        BOOLEAN,
    changed_by      INT REFERENCES users(id),
    changed_at      TIMESTAMPTZ DEFAULT NOW(),
    change_reason   VARCHAR(500)
);

CREATE TABLE measure_values (
    id            BIGSERIAL PRIMARY KEY,
    dimension_id  BIGINT NOT NULL REFERENCES dimensions(id),
    product_id    INT NOT NULL REFERENCES products(id),
    part_op_id    INT NOT NULL REFERENCES part_ops(id),
    value         DECIMAL(14,4),
    result        measure_result NOT NULL,
    gage_id       INT,
    machine_id    INT REFERENCES machines(id),
    measured_by   INT REFERENCES users(id),
    user_type     VARCHAR(30),
    measured_at   TIMESTAMPTZ DEFAULT NOW(),
    note          VARCHAR(200),
    is_final      BOOLEAN DEFAULT FALSE,
    final_op_id   INT REFERENCES part_ops(id),
    has_ncr       BOOLEAN DEFAULT FALSE,
    ncr_code      VARCHAR(50),
    updated_by    INT REFERENCES users(id),
    updated_at    TIMESTAMPTZ
);

CREATE INDEX idx_mv_dimension ON measure_values(dimension_id);
CREATE INDEX idx_mv_product   ON measure_values(product_id);
CREATE INDEX idx_mv_part_op   ON measure_values(part_op_id);

CREATE TABLE measure_value_logs (
    id          BIGSERIAL PRIMARY KEY,
    measure_id  BIGINT NOT NULL REFERENCES measure_values(id),
    old_value   DECIMAL(14,4),
    new_value   DECIMAL(14,4),
    old_result  measure_result,
    new_result  measure_result,
    gage_id     INT,
    note        VARCHAR(500),
    changed_by  INT REFERENCES users(id),
    changed_at  TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================================
-- 8. NCR (Non-Conformance Report)
-- ============================================================

CREATE TABLE ncr_reasons (
    id            SERIAL PRIMARY KEY,
    name          VARCHAR(500) NOT NULL,
    tag           VARCHAR(100),
    title         VARCHAR(100),
    sort_order    INT DEFAULT 0,
    department_id INT REFERENCES departments(id),
    created_by    INT REFERENCES users(id),
    created_at    TIMESTAMPTZ DEFAULT NOW(),
    updated_by    INT REFERENCES users(id),
    updated_at    TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE ncr_codes (
    id           SERIAL PRIMARY KEY,
    ncr_code     VARCHAR(50) UNIQUE NOT NULL,
    year_code    INT,
    sequence     INT,
    machine_id   INT REFERENCES machines(id),
    action       ncr_action DEFAULT 'pending',
    status       ncr_status DEFAULT 'open',
    inspector_id INT REFERENCES users(id),
    inspected_at TIMESTAMPTZ,
    note         VARCHAR(500),
    created_by   INT REFERENCES users(id),
    created_at   TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE ncrs (
    id            SERIAL PRIMARY KEY,
    ncr_code_id   INT NOT NULL REFERENCES ncr_codes(id),
    measure_id    BIGINT NOT NULL REFERENCES measure_values(id),
    department_id INT REFERENCES departments(id),
    reason_id     INT REFERENCES ncr_reasons(id),
    created_by    INT NOT NULL REFERENCES users(id),
    created_at    TIMESTAMPTZ DEFAULT NOW(),
    note          VARCHAR(500)
);

CREATE TABLE cpars (
    id             SERIAL PRIMARY KEY,
    ncr_code_id    INT NOT NULL REFERENCES ncr_codes(id),
    assigned_to    INT REFERENCES users(id),
    department_id  INT REFERENCES departments(id),
    status         file_status DEFAULT 'pending',
    storage_path   VARCHAR(500),
    implement_date DATE,
    done_date      DATE,
    note           VARCHAR(500),
    completed_by   INT REFERENCES users(id),
    completed_at   TIMESTAMPTZ,
    inspector_id   INT REFERENCES users(id),
    inspected_at   TIMESTAMPTZ,
    created_by     INT REFERENCES users(id),
    created_at     TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE reworks (
    id             SERIAL PRIMARY KEY,
    ncr_code_id    INT NOT NULL REFERENCES ncr_codes(id),
    assigned_to    INT REFERENCES users(id),
    department_id  INT REFERENCES departments(id),
    status         file_status DEFAULT 'pending',
    storage_path   VARCHAR(500),
    implement_date DATE,
    done_date      DATE,
    note           VARCHAR(500),
    completed_by   INT REFERENCES users(id),
    completed_at   TIMESTAMPTZ,
    inspector_id   INT REFERENCES users(id),
    inspected_at   TIMESTAMPTZ,
    created_by     INT REFERENCES users(id),
    created_at     TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================================
-- 9. GAGE & CALIBRATION
-- ============================================================

CREATE TABLE gage_statuses (
    id          SERIAL PRIMARY KEY,
    code        VARCHAR(30) UNIQUE NOT NULL,
    description VARCHAR(100),
    is_valid    BOOLEAN DEFAULT TRUE,
    group_code  VARCHAR(50)
);

CREATE TABLE gage_types (
    id            SERIAL PRIMARY KEY,
    code          VARCHAR(20) UNIQUE,
    name          VARCHAR(150) NOT NULL,
    description   VARCHAR(200),
    procedure_id  INT,
    category_id   INT REFERENCES dimension_categories(id)
);

CREATE TABLE gage_locations (
    id          SERIAL PRIMARY KEY,
    code        VARCHAR(50) UNIQUE NOT NULL,
    description VARCHAR(255),
    factory_id  INT REFERENCES factories(id)
);

CREATE TABLE gage_slots (
    id          SERIAL PRIMARY KEY,
    code        VARCHAR(50),
    description VARCHAR(100),
    location_id INT NOT NULL REFERENCES gage_locations(id)
);

CREATE TABLE gages (
    id                   SERIAL PRIMARY KEY,
    gage_no              VARCHAR(30) UNIQUE NOT NULL,
    serial_no            VARCHAR(50),
    description          VARCHAR(100),
    measuring_range      VARCHAR(100),
    accuracy             VARCHAR(100),
    unit                 VARCHAR(50),
    manufacturer         VARCHAR(100),
    calib_frequency_days INT,
    last_calibration     DATE,
    in_service_date      DATE,
    gage_type_id         INT REFERENCES gage_types(id),
    status_id            INT NOT NULL REFERENCES gage_statuses(id),
    default_location_id  INT REFERENCES gage_locations(id),
    default_slot_id      INT REFERENCES gage_slots(id),
    current_location_id  INT REFERENCES gage_locations(id),
    current_slot_id      INT REFERENCES gage_slots(id),
    is_borrowed          BOOLEAN DEFAULT FALSE,
    has_pending_calib    BOOLEAN DEFAULT FALSE,
    vendor_id            INT,
    note                 VARCHAR(200),
    factory_id           INT REFERENCES factories(id),
    created_at           TIMESTAMPTZ DEFAULT NOW(),
    deleted_at           TIMESTAMPTZ
);

-- FK từ measure_values.gage_id → gages.id (thêm sau khi tạo gages)
ALTER TABLE measure_values ADD CONSTRAINT fk_mv_gage
    FOREIGN KEY (gage_id) REFERENCES gages(id);

ALTER TABLE measure_value_logs ADD CONSTRAINT fk_mvl_gage
    FOREIGN KEY (gage_id) REFERENCES gages(id);

ALTER TABLE gage_types ADD CONSTRAINT fk_gt_procedure
    FOREIGN KEY (procedure_id) REFERENCES calib_procedures(id)
    DEFERRABLE INITIALLY DEFERRED;

CREATE TABLE calib_vendors (
    id      SERIAL PRIMARY KEY,
    name    VARCHAR(100) NOT NULL,
    contact VARCHAR(100),
    address VARCHAR(200),
    phone   VARCHAR(50),
    email   VARCHAR(100)
);

CREATE TABLE calib_procedures (
    id          SERIAL PRIMARY KEY,
    name        VARCHAR(100) NOT NULL,
    description VARCHAR(200),
    revision    VARCHAR(20),
    rev_date    DATE,
    doc_link    VARCHAR(200),
    is_latest   BOOLEAN DEFAULT TRUE
);

-- Tạo lại constraint sau khi calib_procedures đã tồn tại
ALTER TABLE gage_types DROP CONSTRAINT IF EXISTS fk_gt_procedure;
ALTER TABLE gage_types ADD CONSTRAINT fk_gt_procedure
    FOREIGN KEY (procedure_id) REFERENCES calib_procedures(id);

ALTER TABLE gages ADD CONSTRAINT fk_gage_vendor
    FOREIGN KEY (vendor_id) REFERENCES calib_vendors(id);

CREATE TABLE calib_requests (
    id           SERIAL PRIMARY KEY,
    gage_id      INT NOT NULL REFERENCES gages(id),
    vendor_id    INT REFERENCES calib_vendors(id),
    request_date DATE DEFAULT CURRENT_DATE,
    status       calib_req_status DEFAULT 'pending',
    created_by   INT REFERENCES users(id),
    created_at   TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE calib_records (
    id                  SERIAL PRIMARY KEY,
    calib_request_id    INT NOT NULL REFERENCES calib_requests(id),
    procedure_id        INT REFERENCES calib_procedures(id),
    calibrated_by       VARCHAR(100),
    calibration_date    DATE NOT NULL,
    as_found_conditions VARCHAR(100),
    adjustment_made     DECIMAL(8,4),
    temperature         DECIMAL(6,2),
    humidity            DECIMAL(6,2),
    storage_path        VARCHAR(200),
    created_by          INT REFERENCES users(id),
    created_at          TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE borrow_transactions (
    id                   SERIAL PRIMARY KEY,
    gage_id              INT NOT NULL REFERENCES gages(id),
    borrower_id          INT NOT NULL REFERENCES users(id),
    manager_id           INT NOT NULL REFERENCES users(id),
    borrow_date          DATE DEFAULT CURRENT_DATE,
    expected_return_date DATE,
    return_date          DATE,
    from_location_id     INT REFERENCES gage_locations(id),
    from_slot_id         INT REFERENCES gage_slots(id),
    use_location_id      INT REFERENCES gage_locations(id),
    status               borrow_status DEFAULT 'active',
    note                 VARCHAR(500),
    created_at           TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE reminders (
    id           SERIAL PRIMARY KEY,
    remind_type  INT,
    remind_date  DATE,
    is_sent      BOOLEAN DEFAULT FALSE,
    content      VARCHAR(200),
    created_at   TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================================
-- 10. FIXTURE MANAGEMENT
-- ============================================================

CREATE TABLE fixture_types (
    id          SERIAL PRIMARY KEY,
    code        VARCHAR(50) UNIQUE,
    name        VARCHAR(200) NOT NULL,
    description VARCHAR(500),
    created_by  INT REFERENCES users(id),
    created_at  TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE fixture_locations (
    id          SERIAL PRIMARY KEY,
    code        VARCHAR(50) UNIQUE,
    name        VARCHAR(200) NOT NULL,
    description VARCHAR(500),
    created_by  INT REFERENCES users(id),
    created_at  TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE fixture_slots (
    id                  SERIAL PRIMARY KEY,
    code                VARCHAR(50),
    name                VARCHAR(200) NOT NULL,
    fixture_location_id INT NOT NULL REFERENCES fixture_locations(id),
    description         VARCHAR(500),
    created_by          INT REFERENCES users(id),
    created_at          TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE fixture_categories (
    id                  SERIAL PRIMARY KEY,
    code                VARCHAR(50),
    name                VARCHAR(200) NOT NULL,
    description         VARCHAR(500),
    max_range           VARCHAR(200),
    min_range           VARCHAR(200),
    fixture_type_id     INT REFERENCES fixture_types(id),
    fixture_location_id INT REFERENCES fixture_locations(id),
    fixture_slot_id     INT REFERENCES fixture_slots(id),
    created_by          INT REFERENCES users(id),
    created_at          TIMESTAMPTZ DEFAULT NOW(),
    updated_by          INT REFERENCES users(id)
);

-- FK gcode_library → fixture_categories (thêm sau khi fixture_categories tồn tại)
ALTER TABLE gcode_library ADD CONSTRAINT fk_gcode_fixture_cat
    FOREIGN KEY (fixture_category_id) REFERENCES fixture_categories(id);

-- ============================================================
-- 11. PLANNING & SHIFTS
-- ============================================================

CREATE TABLE shifts (
    id         SERIAL PRIMARY KEY,
    name       VARCHAR(200) NOT NULL,
    start_time TIME,
    end_time   TIME
);

CREATE TABLE break_times (
    id        SERIAL PRIMARY KEY,
    from_time TIME NOT NULL,
    to_time   TIME NOT NULL
);

CREATE TABLE planning_items (
    id          SERIAL PRIMARY KEY,
    job_id      INT NOT NULL REFERENCES jobs(id),
    part_op_id  INT NOT NULL REFERENCES part_ops(id),
    machine_id  INT NOT NULL REFERENCES machines(id),
    operator_id INT REFERENCES users(id),
    shift_id    INT REFERENCES shifts(id),
    start_time  TIMESTAMPTZ NOT NULL,
    end_time    TIMESTAMPTZ NOT NULL,
    note        VARCHAR(500),
    created_by  INT REFERENCES users(id),
    created_at  TIMESTAMPTZ DEFAULT NOW(),
    updated_by  INT REFERENCES users(id),
    updated_at  TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE shift_assignments (
    id            SERIAL PRIMARY KEY,
    user_id       INT NOT NULL REFERENCES users(id),
    machine_id    INT NOT NULL REFERENCES machines(id),
    shift_id      INT NOT NULL REFERENCES shifts(id),
    assigned_date DATE NOT NULL,
    created_at    TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================================
-- 12. MISC
-- ============================================================

CREATE TABLE confirm_logs (
    id          BIGSERIAL PRIMARY KEY,
    user_id     INT REFERENCES users(id),
    part_id     INT REFERENCES parts(id),
    action      TEXT,
    created_at  TIMESTAMPTZ DEFAULT NOW()
);

-- FK audit_logs → machines (thêm sau khi machines tồn tại)
ALTER TABLE audit_logs ADD CONSTRAINT fk_audit_machine
    FOREIGN KEY (machine_id) REFERENCES machines(id);

-- FK part_op_logs → tech_documents (thêm sau khi tech_documents tồn tại)
ALTER TABLE part_op_logs ADD CONSTRAINT fk_pol_td
    FOREIGN KEY (tech_document_id) REFERENCES tech_documents(id);

-- ============================================================
-- 13. VIEWS
-- ============================================================

CREATE VIEW v_product_completion AS
SELECT
    p.id                 AS product_id,
    p.serial_number,
    j.job_number,
    po.id                AS part_op_id,
    COUNT(d.id)          AS total_dims,
    COUNT(mv.id)         AS measured_dims,
    COUNT(mv.id) FILTER (WHERE mv.result = 'pass') AS pass_count,
    COUNT(mv.id) FILTER (WHERE mv.result = 'fail') AS fail_count,
    CASE WHEN COUNT(d.id) = 0 THEN 0
         ELSE ROUND(COUNT(mv.id)::DECIMAL / COUNT(d.id) * 100, 1)
    END AS completion_pct
FROM products p
JOIN jobs j         ON j.id = p.job_id
JOIN part_ops po    ON po.job_id = j.id
                    OR (po.job_id IS NULL AND po.part_id = j.part_id)
JOIN dimensions d   ON d.part_op_id = po.id AND d.deleted_at IS NULL
LEFT JOIN measure_values mv
                    ON mv.dimension_id = d.id AND mv.product_id = p.id
GROUP BY p.id, p.serial_number, j.job_number, po.id;

CREATE VIEW v_gage_calib_due AS
SELECT
    g.id,
    g.gage_no,
    g.description,
    g.last_calibration,
    (g.last_calibration + (g.calib_frequency_days || ' days')::INTERVAL)::DATE AS due_date,
    (g.last_calibration + (g.calib_frequency_days || ' days')::INTERVAL)::DATE
        - CURRENT_DATE AS days_remaining,
    gs.code        AS status,
    gl.description AS location
FROM gages g
JOIN gage_statuses gs      ON gs.id = g.status_id
LEFT JOIN gage_locations gl ON gl.id = g.current_location_id
WHERE g.deleted_at IS NULL
ORDER BY due_date;

-- ============================================================
-- 14. SEED DATA (master data tối thiểu)
-- ============================================================

INSERT INTO work_statuses (name, is_working) VALUES
    ('Working', TRUE),
    ('On Leave', FALSE),
    ('Resigned', FALSE);

INSERT INTO user_types (type_name, description, can_enter_value, can_raise_ncr) VALUES
    ('Admin',     'System administrator',          FALSE, FALSE),
    ('Manager',   'Production manager',            FALSE, FALSE),
    ('Engineer',  'Manufacturing engineer',        FALSE, FALSE),
    ('QC',        'Quality control inspector',     TRUE,  TRUE),
    ('Operator',  'CNC machine operator',          TRUE,  TRUE),
    ('Inspector', 'Dimensional inspector',         TRUE,  TRUE),
    ('Planning',  'Production planner',            FALSE, FALSE);

INSERT INTO gage_statuses (code, description, is_valid, group_code) VALUES
    ('VALID',    'Calibrated and valid',        TRUE,  'ACTIVE'),
    ('EXPIRED',  'Calibration expired',         FALSE, 'INACTIVE'),
    ('DAMAGED',  'Damaged, out of service',     FALSE, 'INACTIVE'),
    ('BORROWED', 'Currently borrowed',          TRUE,  'ACTIVE'),
    ('CALIB',    'Sent for calibration',        FALSE, 'INACTIVE');

INSERT INTO departments (code, name) VALUES
    ('MGMT',    'Management'),
    ('QC',      'Quality Control'),
    ('PROD',    'Production'),
    ('ME',      'Manufacturing Engineering'),
    ('PLAN',    'Planning'),
    ('MAINT',   'Maintenance');

INSERT INTO roles (name) VALUES
    ('Administrator'),
    ('Manager'),
    ('Engineer'),
    ('QC Inspector'),
    ('Operator'),
    ('Planner');

INSERT INTO file_types (name, code, folder, is_gcode, requires_op_number, requires_revision) VALUES
    ('Drawing',          'DRW',  'drawings',    FALSE, FALSE, TRUE),
    ('G-Code',           'GCD',  'gcodes',      TRUE,  TRUE,  TRUE),
    ('Route Card',       'RTC',  'routecards',  FALSE, TRUE,  FALSE),
    ('Fixture Drawing',  'FXT',  'fixtures',    FALSE, TRUE,  FALSE),
    ('Thread Drawing',   'THD',  'threads',     FALSE, TRUE,  FALSE),
    ('Tool List',        'TLS',  'tools',       FALSE, TRUE,  FALSE),
    ('CAM File',         'CAM',  'cam',         FALSE, TRUE,  FALSE),
    ('CAD Drawing',      'CAD',  'cad',         FALSE, FALSE, TRUE);
