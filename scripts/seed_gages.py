#!/usr/bin/env python3
"""Seed gage data from MySQL legacy to PostgreSQL via Docker."""
import subprocess, os, sys

PG_CONTAINER = "shopfloor-manager-dev-postgres-1"
PG_USER      = "shopfloor"
PG_DB        = "shopfloor_dev"

def mysql(sql):
    r = subprocess.run(
        ["mysql", "-h127.0.0.1", "-uadmin", "-p1qazxsw2", "longnv",
         "--batch", "--skip-column-names", "-e", sql],
        capture_output=True, text=True, errors="replace"
    )
    if r.returncode != 0:
        print("MySQL error:", r.stderr[:200])
        return []
    return [line.split("\t") for line in r.stdout.strip().split("\n") if line.strip()]

def pg(sql):
    r = subprocess.run(
        ["docker", "exec", "-i", PG_CONTAINER,
         "psql", f"-U{PG_USER}", f"-d{PG_DB}", "-c", sql],
        capture_output=True, text=True
    )
    if r.returncode != 0 and "ERROR" in r.stderr:
        print("  PG error:", r.stderr.strip()[:200])
    return r.returncode == 0

def pg_count(table):
    r = subprocess.run(
        ["docker", "exec", "-i", PG_CONTAINER,
         "psql", f"-U{PG_USER}", f"-d{PG_DB}", "-t", "-c", f"SELECT COUNT(*) FROM {table}"],
        capture_output=True, text=True
    )
    return r.stdout.strip()

def q(s):
    if not s or s.strip() in ("NULL", ""):
        return "NULL"
    return "'" + s.strip().replace("'", "''") + "'"

def qd(s):
    if not s or s.strip() in ("NULL", "", "0000-00-00"):
        return "NULL"
    return f"'{s.strip()}'::date"

STATUS_MAP = {"VL": "VALID", "IVL": "EXPIRED", "CL": "CALIB",
              "NRP": "DAMAGED", "DNU": "DAMAGED", "LST": "DAMAGED", "RP": "DAMAGED"}
CAT_MAP = {
    "CAL": "LIN", "BOR": "LIN", "DPG": "LIN", "HEG": "LIN", "MIC": "LIN",
    "LHG": "LIN", "EDC": "LIN", "GRO": "LIN", "CTG": "LIN",
    "ANG": "ANG",
    "PLG": "THD", "RIG": "THD", "RGA": "THD", "IPG": "THD",
    "ETG": "THD", "IGT": "THD", "THG": "THD", "PDG": "THD",
    "PGA": "THD", "GMT": "THD",
    "CMM": "GEO", "IND": "GEO", "PPM": "GEO", "RAD": "GEO",
    "VIS": "GEO", "GBS": "GEO",
    "SRM": "SFC", "SRT": "SFC",
}

print("=== Seed Gage data: MySQL to PostgreSQL ===")

# ── 1. GageLocations ──────────────────────────────────────────────────────
print("\n1. Seeding gage_locations...")
rows = mysql("SELECT LocationCode, Description FROM gagelocation "
             "WHERE LocationCode IS NOT NULL ORDER BY LocationCode")
for i, row in enumerate(rows, 1):
    code = row[0]
    desc = row[1] if len(row) > 1 else row[0]
    pg(f"INSERT INTO gage_locations(id, code, description, created_at, updated_at) "
       f"OVERRIDING SYSTEM VALUE VALUES ({i}, {q(code)}, {q(desc)}, NOW(), NOW()) "
       f"ON CONFLICT (code) DO NOTHING")
loc_id = {row[0]: i for i, row in enumerate(rows, 1)}
print(f"   Seeded {len(rows)} locations. DB: {pg_count('gage_locations')}")

# ── 2. GageTypes ──────────────────────────────────────────────────────────
print("\n2. Seeding gage_types...")
rows_t = mysql("SELECT TypeCode, MIN(TypeName), MAX(CategoryCode) FROM gagetype "
               "WHERE TypeCode IS NOT NULL GROUP BY TypeCode ORDER BY TypeCode")
seen, type_rows, type_id = set(), [], {}
idx = 1
for row in rows_t:
    if len(row) < 2: continue
    code = row[0].strip()
    name = row[1].strip() if len(row) > 1 else code
    cat  = row[2].strip() if len(row) > 2 else ""
    if not code or code in seen: continue
    seen.add(code)
    type_rows.append((idx, code, name, cat))
    type_id[code] = idx
    idx += 1

for i, code, name, cat in type_rows:
    pg(f"INSERT INTO gage_types(id, code, name, description, created_at, updated_at) "
       f"OVERRIDING SYSTEM VALUE VALUES ({i}, {q(code)}, {q(name)}, {q(cat)}, NOW(), NOW()) "
       f"ON CONFLICT (code) DO NOTHING")

for i, code, name, cat in type_rows:
    new_cat = CAT_MAP.get(cat)
    if new_cat:
        pg(f"UPDATE gage_types SET category_id = "
           f"(SELECT id FROM dimension_categories WHERE code = '{new_cat}') "
           f"WHERE code = {q(code)}")
print(f"   Seeded {len(type_rows)} types. DB: {pg_count('gage_types')}")

# ── 3. Gages ──────────────────────────────────────────────────────────────
print("\n3. Seeding gages (80 records)...")
# Get representative sample via multiple queries
def get_gages_by_status(status_filter, borrowed_filter, limit):
    where = f"g.GageStatus='{status_filter}' AND g.BrStatus={borrowed_filter}" if status_filter else f"g.BrStatus={borrowed_filter}"
    return mysql(
        f"SELECT g.GageNo, COALESCE(g.GageSN,''),"
        f" REPLACE(COALESCE(g.GageDescription,''),'\\t',' '),"
        f" COALESCE(g.MeasuringRange,''), COALESCE(g.Accuracy,''),"
        f" COALESCE(NULLIF(g.Unit,''),'mm'), COALESCE(g.CalibrationFrequency,12),"
        f" COALESCE(DATE_FORMAT(g.LastCalibration,'%Y-%m-%d'),''),"
        f" COALESCE(DATE_FORMAT(g.InServiceDate,'%Y-%m-%d'),''),"
        f" COALESCE(g.GageStatus,'VL'), COALESCE(g.BrStatus,0),"
        f" COALESCE(gt.TypeCode,''), COALESCE(gl.LocationCode,'')"
        f" FROM gage g LEFT JOIN gagetype gt ON gt.TypeID=g.TypeID"
        f" LEFT JOIN gagelocation gl ON gl.LocationID=g.LocationIDUse"
        f" WHERE g.GageNo IS NOT NULL AND g.GageDescription IS NOT NULL"
        f" AND LENGTH(g.GageNo)<=30 AND {where} LIMIT {limit}"
    )

seen_gno = set()
gages = []
for rows in [
    get_gages_by_status("VL", 0, 50),   # 50 valid
    get_gages_by_status("IVL", 0, 10),  # 10 expired
    get_gages_by_status("CL", 0, 8),    # 8 calib
    get_gages_by_status("NRP", 0, 8),   # 8 damaged
    get_gages_by_status("", 1, 10),     # 10 borrowed
]:
    for r in rows:
        gno = r[0].strip() if r else ""
        if gno and gno not in seen_gno:
            seen_gno.add(gno)
            gages.append(r)

# dummy line to keep the original variable assignment pattern
if False: gages = mysql("""
    (SELECT g.GageNo, COALESCE(g.GageSN,''),
            REPLACE(COALESCE(g.GageDescription,''),'\t',' ') AS Desc,
            COALESCE(g.MeasuringRange,''), COALESCE(g.Accuracy,''),
            COALESCE(NULLIF(g.Unit,''),'mm'), COALESCE(g.CalibrationFrequency,12),
            COALESCE(DATE_FORMAT(g.LastCalibration,'%Y-%m-%d'),''),
            COALESCE(DATE_FORMAT(g.InServiceDate,'%Y-%m-%d'),''),
            'VL', 0, COALESCE(gt.TypeCode,''), COALESCE(gl.LocationCode,'')
     FROM gage g LEFT JOIN gagetype gt ON gt.TypeID=g.TypeID
     LEFT JOIN gagelocation gl ON gl.LocationID=g.LocationIDUse
     WHERE g.GageNo IS NOT NULL AND g.GageDescription IS NOT NULL
       AND LENGTH(g.GageNo)<=30 AND g.GageStatus='VL' AND g.BrStatus=0
     LIMIT 50)
    UNION ALL
    (SELECT g.GageNo, COALESCE(g.GageSN,''),
            REPLACE(COALESCE(g.GageDescription,''),'\t',' '),
            COALESCE(g.MeasuringRange,''), COALESCE(g.Accuracy,''),
            COALESCE(NULLIF(g.Unit,''),'mm'), COALESCE(g.CalibrationFrequency,12),
            COALESCE(DATE_FORMAT(g.LastCalibration,'%Y-%m-%d'),''),
            COALESCE(DATE_FORMAT(g.InServiceDate,'%Y-%m-%d'),''),
            'IVL', 0, COALESCE(gt.TypeCode,''), COALESCE(gl.LocationCode,'')
     FROM gage g LEFT JOIN gagetype gt ON gt.TypeID=g.TypeID
     LEFT JOIN gagelocation gl ON gl.LocationID=g.LocationIDUse
     WHERE g.GageNo IS NOT NULL AND g.GageDescription IS NOT NULL
       AND LENGTH(g.GageNo)<=30 AND g.GageStatus='IVL'
     LIMIT 10)
    UNION ALL
    (SELECT g.GageNo, COALESCE(g.GageSN,''),
            REPLACE(COALESCE(g.GageDescription,''),'\t',' '),
            COALESCE(g.MeasuringRange,''), COALESCE(g.Accuracy,''),
            COALESCE(NULLIF(g.Unit,''),'mm'), COALESCE(g.CalibrationFrequency,12),
            COALESCE(DATE_FORMAT(g.LastCalibration,'%Y-%m-%d'),''),
            COALESCE(DATE_FORMAT(g.InServiceDate,'%Y-%m-%d'),''),
            'CL', 0, COALESCE(gt.TypeCode,''), COALESCE(gl.LocationCode,'')
     FROM gage g LEFT JOIN gagetype gt ON gt.TypeID=g.TypeID
     LEFT JOIN gagelocation gl ON gl.LocationID=g.LocationIDUse
     WHERE g.GageNo IS NOT NULL AND g.GageDescription IS NOT NULL
       AND LENGTH(g.GageNo)<=30 AND g.GageStatus='CL'
     LIMIT 8)
    UNION ALL
    (SELECT g.GageNo, COALESCE(g.GageSN,''),
            REPLACE(COALESCE(g.GageDescription,''),'\t',' '),
            COALESCE(g.MeasuringRange,''), COALESCE(g.Accuracy,''),
            COALESCE(NULLIF(g.Unit,''),'mm'), COALESCE(g.CalibrationFrequency,12),
            COALESCE(DATE_FORMAT(g.LastCalibration,'%Y-%m-%d'),''),
            COALESCE(DATE_FORMAT(g.InServiceDate,'%Y-%m-%d'),''),
            g.GageStatus, COALESCE(g.BrStatus,0),
            COALESCE(gt.TypeCode,''), COALESCE(gl.LocationCode,'')
     FROM gage g LEFT JOIN gagetype gt ON gt.TypeID=g.TypeID
     LEFT JOIN gagelocation gl ON gl.LocationID=g.LocationIDUse
     WHERE g.GageNo IS NOT NULL AND g.GageDescription IS NOT NULL
       AND LENGTH(g.GageNo)<=30 AND g.BrStatus=1
     LIMIT 10)
    UNION ALL
    (SELECT g.GageNo, COALESCE(g.GageSN,''),
            REPLACE(COALESCE(g.GageDescription,''),'\t',' '),
            COALESCE(g.MeasuringRange,''), COALESCE(g.Accuracy,''),
            COALESCE(NULLIF(g.Unit,''),'mm'), COALESCE(g.CalibrationFrequency,12),
            COALESCE(DATE_FORMAT(g.LastCalibration,'%Y-%m-%d'),''),
            COALESCE(DATE_FORMAT(g.InServiceDate,'%Y-%m-%d'),''),
            'NRP', 0, COALESCE(gt.TypeCode,''), COALESCE(gl.LocationCode,'')
     FROM gage g LEFT JOIN gagetype gt ON gt.TypeID=g.TypeID
     LEFT JOIN gagelocation gl ON gl.LocationID=g.LocationIDUse
     WHERE g.GageNo IS NOT NULL AND g.GageDescription IS NOT NULL
       AND LENGTH(g.GageNo)<=30 AND g.GageStatus='NRP'
     LIMIT 8)
""")

ok = 0
for g in gages:
    if len(g) < 13: continue
    gno, sn, desc, mrange, acc, unit, freq, last_cal, in_svc, \
        status, borrowed, type_code, loc_code = [x.strip() for x in g[:13]]

    if not gno: continue
    status_code = STATUS_MAP.get(status, "VALID")
    if borrowed == "1": status_code = "BORROWED"

    type_val = str(type_id.get(type_code)) if type_code in type_id else "NULL"
    loc_val  = str(loc_id.get(loc_code))   if loc_code  in loc_id  else "NULL"
    freq_val = freq if freq.isdigit() else "365"

    if pg(
        f"INSERT INTO gages "
        f"(gage_no, serial_no, description, measuring_range, accuracy, unit, "
        f"calib_frequency_days, last_calibration, in_service_date, "
        f"status_code, is_borrowed, has_pending_calib, "
        f"gage_type_id, default_location_id, current_location_id, "
        f"created_at, updated_at) VALUES ("
        f"{q(gno)}, {q(sn)}, {q(desc)}, {q(mrange)}, {q(acc)}, {q(unit)}, "
        f"{freq_val}, {qd(last_cal)}, {qd(in_svc)}, "
        f"'{status_code}', {'true' if borrowed=='1' else 'false'}, false, "
        f"{type_val}, {loc_val}, {loc_val}, "
        f"NOW(), NOW()) ON CONFLICT (gage_no) DO NOTHING"
    ):
        ok += 1

print(f"   Inserted: {ok} / {len(gages)}")
print(f"   DB total: {pg_count('gages')}")

# ── Summary ────────────────────────────────────────────────────────────────
print("\n--- Summary by status ---")
r = subprocess.run(
    ["docker", "exec", "-i", PG_CONTAINER,
     "psql", f"-U{PG_USER}", f"-d{PG_DB}",
     "-c", "SELECT status_code, COUNT(*) FROM gages GROUP BY status_code ORDER BY status_code"],
    capture_output=True, text=True
)
print(r.stdout)

print("--- Sample gages ---")
r = subprocess.run(
    ["docker", "exec", "-i", PG_CONTAINER,
     "psql", f"-U{PG_USER}", f"-d{PG_DB}",
     "-c", "SELECT gage_no, LEFT(description,35), status_code, last_calibration FROM gages ORDER BY gage_no LIMIT 12"],
    capture_output=True, text=True
)
print(r.stdout)
