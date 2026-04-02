-- DROP SCHEMA log_data_proc;

CREATE SCHEMA log_data_proc AUTHORIZATION postgres;

-- DROP TYPE log_data_proc.process_type_enum;

CREATE TYPE log_data_proc.process_type_enum AS ENUM (
  'ML_AGGREGATE',
  'ML_INSERTDATA',
  'ML_WF_OR_DF');



-- log_data_proc.multilayer_aggregate_proc definition

-- Drop table

-- DROP TABLE log_data_proc.multilayer_aggregate_proc;

CREATE TABLE log_data_proc.multilayer_aggregate_proc (
  id bigserial NOT NULL,
  batch_no varchar NOT NULL,
  process_type log_data_proc.process_type_enum NOT NULL,
  process_code varchar NOT NULL,
  request json NULL,
  response json NULL,
  log text NULL,
  updated_at timestamptz DEFAULT CURRENT_TIMESTAMP NULL,
  CONSTRAINT multilayer_aggregate_proc_pk PRIMARY KEY (id),
  CONSTRAINT multilayer_aggregate_proc_unique UNIQUE (batch_no, process_code)
);





---------------------------------
-- DROP FUNCTION log_data_proc.acv_generate_batch_no(varchar);

CREATE OR REPLACE FUNCTION log_data_proc.acv_generate_batch_no(p_prefix character varying DEFAULT 'BATCH'::character varying)
 RETURNS character varying
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_today VARCHAR := TO_CHAR(CURRENT_DATE, 'YYYYMMDD');
    v_max_increment INT;
    v_next_increment INT;
    v_batch_no VARCHAR;
    v_pattern TEXT := '^' || p_prefix || '-' || v_today || '-(\d{7})$';
BEGIN
    -- Cari nilai increment tertinggi untuk hari ini dengan pattern yang lebih ketat
    SELECT COALESCE(MAX(CAST(SUBSTRING(match[1] FROM 1 FOR 7) AS INT)), 0)
    INTO v_max_increment
    FROM (
        SELECT regexp_matches(batch_no, v_pattern) as match
        FROM log_data_proc.multilayer_aggregate_proc
        WHERE batch_no ~ v_pattern
    ) subq;

    -- Hitung increment berikutnya
    v_next_increment := v_max_increment + 1;

    -- Format batch_no dengan padding 7 digit
    v_batch_no := p_prefix || '-' || v_today || '-' || LPAD(v_next_increment::TEXT, 7, '0');

    RETURN v_batch_no;
END;
$function$
;

--------------------
-- DROP FUNCTION log_data_proc.upsert_multilayer_aggregate_proc(varchar, varchar, varchar, int4, int4, json, json, text);

CREATE OR REPLACE FUNCTION log_data_proc.upsert_multilayer_aggregate_proc(p_batch_no character varying, p_process_type character varying, p_process_code character varying, p_process_index integer, p_total_process integer, p_request json DEFAULT NULL::json, p_response json DEFAULT NULL::json, p_log text DEFAULT NULL::text)
 RETURNS integer
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_affected_rows INTEGER;
BEGIN
    INSERT INTO "log_data_proc".multilayer_aggregate_proc (
        batch_no,
        process_type,
        process_code,
        process_index,
        total_process,
        request,
        response,
        log
    )
    VALUES (
        p_batch_no,
        p_process_type::"log_data_proc".process_type_enum,
        p_process_code,
        p_process_index,
        p_total_process,
        p_request,
        p_response,
        p_log
    )
    ON CONFLICT (batch_no, process_code, process_index) DO UPDATE
    SET
        process_type = EXCLUDED.process_type,
        total_process = EXCLUDED.total_process,
        request = EXCLUDED.request,
        response = EXCLUDED.response,
        log = EXCLUDED.log,
        updated_at = CURRENT_TIMESTAMP;

    GET DIAGNOSTICS v_affected_rows = ROW_COUNT;
    
    RETURN v_affected_rows;
END;
$function$
;



-- ======================
-- DATA SAMPLING
-- ======================
INSERT INTO log_data_proc.multilayer_aggregate_proc
(id, batch_no, process_type, process_code, request, response, log, updated_at)
VALUES(1, 'BATCH-20260321-0000001', 'ML_AGGREGATE'::log_data_proc.process_type_enum, 'GC2', '{
  "batch_id": "BATCH-20260321-0000001",
  "total_items": 1,
  "process_index": 1,
  "code": "GC2",
  "data": {
    "employees": [
      {
        "id": 1,
        "name": "John Doe",
        "department": "IT",
        "salary": 10000000,
        "bonus": 2000000,
        "isActive": true,
        "joinDate": "2020-01-15"
      },
      {
        "id": 2,
        "name": "Jane Smith",
        "department": "HR",
        "salary": 9000000,
        "bonus": 1500000,
        "isActive": true,
        "joinDate": "2019-05-20"
      },
      {
        "id": 3,
        "name": "Bob Johnson",
        "department": "IT",
        "salary": 8500000,
        "bonus": 1200000,
        "isActive": true,
        "joinDate": "2021-03-10"
      },
      {
        "id": 4,
        "name": "Alice Williams",
        "department": "Finance",
        "salary": 12000000,
        "bonus": 3000000,
        "isActive": true,
        "joinDate": "2018-11-05"
      },
      {
        "id": 5,
        "name": "Charlie Brown",
        "department": "Marketing",
        "salary": 7500000,
        "bonus": 800000,
        "isActive": true,
        "joinDate": "2022-02-14"
      },
      {
        "id": 6,
        "name": "David Miller",
        "department": "IT",
        "salary": 11000000,
        "bonus": 2500000,
        "isActive": false,
        "joinDate": "2017-08-30"
      },
      {
        "id": 7,
        "name": "Eve Davis",
        "department": "HR",
        "salary": 9200000,
        "bonus": 1600000,
        "isActive": true,
        "joinDate": "2020-11-11"
      },
      {
        "id": 8,
        "name": "Frank Wilson",
        "department": "Finance",
        "salary": 13000000,
        "bonus": 3500000,
        "isActive": true,
        "joinDate": "2016-04-25"
      },
      {
        "id": 9,
        "name": "Grace Taylor",
        "department": "Marketing",
        "salary": 7800000,
        "bonus": 900000,
        "isActive": true,
        "joinDate": "2021-09-01"
      },
      {
        "id": 10,
        "name": "Henry Moore",
        "department": "IT",
        "salary": 10500000,
        "bonus": 2100000,
        "isActive": true,
        "joinDate": "2019-12-12"
      }
    ],
    "departments": [
      {
        "id": 1,
        "name": "IT",
        "budget": 150000000
      },
      {
        "id": 2,
        "name": "HR",
        "budget": 80000000
      },
      {
        "id": 3,
        "name": "Finance",
        "budget": 200000000
      },
      {
        "id": 4,
        "name": "Marketing",
        "budget": 60000000
      }
    ]
  }
}'::json, '{
  "aggr_sum": 98500000.0,
  "aggr_avg": 9850000.0,
  "aggr_min": 800000.0,
  "aggr_max": 200000000.0,
  "aggr_count_hr": 2.0,
  "arith_add": 10500000.0,
  "arith_sub": 110000000.0,
  "arith_mul": 13800000.00,
  "arith_div": 9850000.0,
  "arith_mod_like": 1000000.0,
  "filter_and": [
    "John Doe",
    "Bob Johnson",
    "Henry Moore"
  ],
  "filter_or": [
    "Alice Williams",
    "Frank Wilson",
    "Jane Smith",
    "Eve Davis"
  ],
  "filter_like": "Jane Smith",
  "filter_in": [
    "Alice Williams",
    "Frank Wilson",
    "John Doe",
    "Bob Johnson",
    "David Miller",
    "Henry Moore"
  ],
  "filter_not_in": [
    "Alice Williams",
    "Frank Wilson",
    "Jane Smith",
    "Eve Davis",
    "Charlie Brown",
    "Grace Taylor"
  ],
  "filter_complex_logic": [
    "Jane Smith",
    "Eve Davis",
    "John Doe",
    "Henry Moore",
    "David Miller"
  ],
  "order_salary_asc": [
    "Charlie Brown",
    "Grace Taylor",
    "Bob Johnson",
    "Jane Smith",
    "Eve Davis",
    "John Doe",
    "Henry Moore",
    "David Miller",
    "Alice Williams",
    "Frank Wilson"
  ],
  "order_name_desc": [
    "John Doe",
    "Jane Smith",
    "Henry Moore",
    "Grace Taylor",
    "Frank Wilson",
    "Eve Davis",
    "David Miller",
    "Charlie Brown",
    "Bob Johnson",
    "Alice Williams"
  ],
  "order_date_desc": [
    "Charlie Brown",
    "Grace Taylor",
    "Bob Johnson",
    "Eve Davis",
    "John Doe",
    "Henry Moore",
    "Jane Smith",
    "Alice Williams",
    "David Miller",
    "Frank Wilson"
  ],
  "order_multi_index": [
    "Frank Wilson",
    "David Miller"
  ],
  "order_dept_budget": [
    "Marketing",
    "HR",
    "IT",
    "Finance"
  ],
  "comb_it_bonus_ratio": 0.195,
  "comb_finance_net": 168500000.0,
  "comb_active_avg_tax": 972222.222222,
  "comb_max_salary_hr_it": 11000000.0,
  "comb_count_non_it_rich": 4.0,
  "prec_math_1": 20.0,
  "prec_math_2": 30.0,
  "prec_math_3": 12.0,
  "prec_math_4": 26.0,
  "prec_math_5": 90.0,
  "real_prec_nett_salary_it": 38000000.00,
  "real_prec_daily_rate": 2454545.454545,
  "paren_math_1": 30.0,
  "paren_math_2": 5.0,
  "paren_math_3": 240.0,
  "paren_math_4": 9.0,
  "paren_math_5": 100.0,
  "real_paren_avg_income_hr": 10650000.0,
  "real_paren_bonus_sharing": 11000000.0,
  "real_paren_bonus_achievement": 83.333333,
  "lookup_0": 98500000.0,
  "lookup_1": 9850000.0,
  "lookup_2": 9850000.0,
  "lookup_3": 98499000.0,
  "val_0": 10000000.0,
  "val_1": 100000000.0,
  "val_3": 5000000.0,
  "pct_0": 90.0,
  "pct_1": 50.0,
  "kenaikan_gaji_john_10pct": 11000000.00,
  "gap_gaji_ekstrim": 5500000.0,
  "it_staff_aktif_only": [
    "John Doe",
    "Bob Johnson",
    "Henry Moore"
  ],
  "top_2_nama_gaji_tertinggi": [
    "Frank Wilson",
    "Alice Williams"
  ],
  "it_bonus_ratio_real": 80.500,
  "budget_finance_surplus": 168500000.0,
  "pajak_total_perusahaan": 4925000.00
}'::json, 'Process completed successfully.
error1
erro2', '2026-03-21 02:04:16.803');