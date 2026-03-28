TRUNCATE TABLE aggregation.aggregate_configurations_version RESTART IDENTITY;

-- =============================================
-- Table: aggregation.aggregate_configurations_version
-- =============================================
CREATE TABLE aggregation.aggregate_configurations_version (
    -- Primary Key & Identitas
    gcv_id bigserial NOT NULL,
    gcv_code varchar(50) NOT NULL, -- Group ID (e.g., 'CONFIG_A')
    
    -- Versi & Status
    gcv_version varchar(20) NOT NULL, -- Format {yy}.{autoIncrement}
    gcv_status varchar(50) NOT NULL,  -- e.g., 'CREATED', 'CHECKED', 'APPROVED', 'REJECTED'
    
    -- Flagging Pointer
    is_last_active boolean DEFAULT false,
    is_last_version boolean DEFAULT true,

    -- Data Utama
    -- ini dibikin unique aja, dan harus ada proses replace jadi spasi (" ") contoh: "@end@ng sukamti" -> "_end_ng sukamti"
    gcv_name varchar(255) NOT NULL, 
    gcv_desc text NULL,
    gcv_data_applied varchar(50) NULL,
    gcv_type varchar(50) NULL,
    gcv_json_list text NULL,
    gcv_json_condition text NULL,
    gcv_config_final text NULL,
    
    -- Audit & Activity Trail (Pengganti nama workflow)
    -- Format JSONB: 
    -- [
    --   {"status": "CREATED", user_type: "MAKER", "user": "SIPIR", "note": "Initial Create", "date": "..."},
    --   {"status": "REJECTED", user_type: "CHECKER", "user": "ENDANG", "note": "Initial Create", "date": "..."},
    --   {"status": "UPDATED", user_type: "MAKER", "user": "SIPIR", "note": "Initial Create", "date": "..."},
    --   {"status": "CHECKED", user_type: "CHECKER", "user": "ENDANG", "note": "Initial Create", "date": "..."},
    --   {"status": "REJECTED", user_type: "APPROVER", "user": "ALI", "note": "Initial Create", "date": "..."}
    --   {"status": "UPDATED", user_type: "MAKER", "user": "SIPIR", "note": "Initial Create", "date": "..."},
    -- ]
    gcv_activity_log jsonb NULL,
    
    gcv_is_deleted bool DEFAULT false, -- tambahin field ini
    
    -- Metadata
    gcv_created_by varchar(100) NULL, -- ENDANG
    gcv_created_date timestamp(6) DEFAULT CURRENT_TIMESTAMP, -- NOW
    gcv_updated_by varchar(100) NULL, -- ALI
    gcv_updated_date timestamp(6) NULL, -- NOW

    CONSTRAINT aggregate_configurations_version_pkey PRIMARY KEY (gcv_id)
);

-- =============================================
-- Constraints & Triggers
-- =============================================

-- Unique Constraint: Nama harus unik selama belum di-delete
CREATE UNIQUE INDEX uq_gcv_name_not_deleted 
ON aggregation.aggregate_configurations_version (gcv_name) 
WHERE (gcv_is_deleted = false);

-- Hapus index lama
DROP INDEX IF EXISTS aggregation.uq_gcv_name_not_deleted;

-- Buat Unique Index baru berdasarkan 4 kriteria
CREATE UNIQUE INDEX uq_gcv_code_name_version_not_deleted 
ON aggregation.aggregate_configurations_version (gcv_code, gcv_name, gcv_version) 
WHERE (gcv_is_deleted = false);

-- Function Sanitasi: Alphanumeric, Space, Underscore. Selain itu ganti ke '_'
CREATE OR REPLACE FUNCTION aggregation.acv_sanitize_aggregate_name()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.gcv_name IS NOT NULL THEN
        -- Replace karakter yang BUKAN alphanumeric, spasi, atau underscore menjadi '_'
        NEW.gcv_name := REGEXP_REPLACE(NEW.gcv_name, '[^a-zA-Z0-9_ ]', '_', 'g');
        NEW.gcv_name := TRIM(NEW.gcv_name);
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_before_upsert_sanitize_name
BEFORE INSERT OR UPDATE OF gcv_name ON aggregation.aggregate_configurations_version
FOR EACH ROW EXECUTE FUNCTION aggregation.acv_sanitize_aggregate_name();

-- =============================================
-- Indexing Strategy
-- =============================================

-- B-Tree Index (Standar & Pencarian Grup)
CREATE INDEX idx_gcv_code ON aggregation.aggregate_configurations_version (gcv_code) 
WHERE (gcv_is_deleted = false);

CREATE INDEX idx_gcv_status ON aggregation.aggregate_configurations_version (gcv_status) 
WHERE (gcv_is_deleted = false);

-- Partial Index
-- Query: WHERE gcv_code = 'XYZ' AND is_last_active = true
CREATE INDEX idx_gcv_fast_active ON aggregation.aggregate_configurations_version (gcv_code) 
WHERE is_last_active = true AND gcv_is_deleted = false;

-- Query: WHERE gcv_code = 'XYZ' AND is_last_version = true
CREATE INDEX idx_gcv_fast_latest ON aggregation.aggregate_configurations_version (gcv_code) 
WHERE is_last_version = true AND gcv_is_deleted = false;

-- GIN Index (Pencarian di dalam JSONB)
-- Mengizinkan pencarian key/value di dalam JSONB activity log
CREATE INDEX idx_gcv_activity_json ON aggregation.aggregate_configurations_version USING GIN (gcv_activity_log);

-- Composite Index (Untuk Sorting & Versioning)
CREATE INDEX idx_gcv_history_sort ON aggregation.aggregate_configurations_version (gcv_code, gcv_version DESC)
WHERE (gcv_is_deleted = false);

-- =============================================
-- Functions
-- =============================================

CREATE OR REPLACE FUNCTION aggregation.acv_get_aggregation_grid(
    p_mode varchar(20) DEFAULT 'ALL' -- 'LAST_VERSION', 'LAST_ACTIVE', 'ALL'
)
RETURNS jsonb AS $$
DECLARE
    v_result jsonb;
BEGIN
    WITH last_reject_info AS (
        -- Tambahkan filter is_deleted di sini biar gak proses data sampah
        SELECT 
            t.gcv_id,
            rej."user" as r_by,
            rej.note as r_note,
            (rej."date")::timestamp as r_date,
            rej.user_type as r_pos
        FROM aggregation.aggregate_configurations_version t
        CROSS JOIN LATERAL (
            SELECT * FROM jsonb_to_recordset(t.gcv_activity_log) 
            AS x("status" varchar, "user" varchar, "note" text, "date" varchar, "user_type" varchar)
            WHERE x."status" = 'REJECTED'
            ORDER BY x."date" DESC LIMIT 1
        ) rej
        WHERE t.gcv_is_deleted = false -- Filter di level logic reject
        AND NOT EXISTS (
            SELECT 1 FROM jsonb_to_recordset(t.gcv_activity_log) 
            AS app("status" varchar, "date" varchar, "user_type" varchar)
            WHERE app.user_type = rej.user_type 
            AND app."status" IN ('APPROVED', 'CHECKED')
            AND app."date" > rej."date"
        )
    )
    SELECT 
        jsonb_agg(
            jsonb_build_object(
                'gc_id', main.gcv_id,
                'gc_code', main.gcv_code,
                'gc_name', main.gcv_name,
                'gc_desc', main.gcv_desc,
                'gc_data_applied', main.gcv_data_applied,
                'gc_type', main.gcv_type,
                'gc_json_list', main.gcv_json_list::jsonb,
                'gc_json_condition', main.gcv_json_condition::jsonb,
                'gc_version', main.gcv_version,
                'gc_status', main.gcv_status,
                'gc_created_by', main.gcv_created_by,
                'gc_created_date', main.gcv_created_date,
                'gc_updated_by', main.gcv_updated_by,
                'gc_updated_date', main.gcv_updated_date,
                'gc_reject_by', rj.r_by,
                'gc_reject_note', rj.r_note,
                'gc_reject_position', rj.r_pos,
                'gc_reject_date', rj.r_date
            )
        ) INTO v_result
    FROM aggregation.aggregate_configurations_version main
    LEFT JOIN last_reject_info rj ON main.gcv_id = rj.gcv_id
    WHERE main.gcv_is_deleted = false -- Filter di level main query
    AND (
        (p_mode = 'LAST_VERSION' AND main.is_last_version = true) OR
        (p_mode = 'LAST_ACTIVE' AND main.is_last_active = true) OR
        (p_mode = 'ALL')
    );

    RETURN COALESCE(v_result, '[]'::jsonb);
END;
$$ LANGUAGE plpgsql;


------

CREATE OR REPLACE FUNCTION aggregation.acv_get_aggregation_detail(
    p_id bigint
)
RETURNS jsonb AS $$
DECLARE
    v_result jsonb;
BEGIN
    WITH reject_logic AS (
        SELECT 
            t.gcv_id,
            rej."user" as r_by,
            rej.note as r_note,
            (rej."date")::timestamp as r_date,
            rej.user_type as r_pos
        FROM aggregation.aggregate_configurations_version t
        CROSS JOIN LATERAL (
            SELECT * FROM jsonb_to_recordset(t.gcv_activity_log) 
            AS x("status" varchar, "user" varchar, "note" text, "date" varchar, "user_type" varchar)
            WHERE x."status" = 'REJECTED'
            ORDER BY x."date" DESC LIMIT 1
        ) rej
        WHERE t.gcv_id = p_id
        AND NOT EXISTS (
            SELECT 1 FROM jsonb_to_recordset(t.gcv_activity_log) 
            AS app("status" varchar, "date" varchar, "user_type" varchar)
            WHERE app.user_type = rej.user_type 
            AND app."status" IN ('APPROVED', 'CHECKED')
            AND app."date" > rej."date"
        )
    )
    SELECT 
        jsonb_build_object(
            'gcv_id', m.gcv_id,
            'gcv_code', m.gcv_code,
            'gcv_version', m.gcv_version,
            'gcv_status', m.gcv_status,
            'is_last_active', m.is_last_active,
            'is_last_version', m.is_last_version,
            'gcv_name', m.gcv_name,
            'gcv_desc', m.gcv_desc,
            'gcv_data_applied', m.gcv_data_applied,
            'gcv_type', m.gcv_type,
            'gcv_json_list', m.gcv_json_list,
            'gcv_json_condition', m.gcv_json_condition,
            'gcv_config_final', m.gcv_config_final,
            'gcv_is_deleted', m.gcv_is_deleted,
            'gcv_created_by', m.gcv_created_by,
            'gcv_created_date', m.gcv_created_date,
            'gcv_updated_by', m.gcv_updated_by,
            'gcv_updated_date', m.gcv_updated_date,
            'gcv_reject_by', rl.r_by,
            'gcv_reject_note', rl.r_note,
            'gcv_reject_date', rl.r_date,
            'gcv_reject_position', rl.r_pos
        ) INTO v_result
    FROM aggregation.aggregate_configurations_version m
    LEFT JOIN reject_logic rl ON m.gcv_id = rl.gcv_id
    WHERE m.gcv_id = p_id;

    -- Kembalikan object kosong jika ID tidak ketemu (biar di C# gak error)
    RETURN COALESCE(v_result, '{}'::jsonb);
END;
$$ LANGUAGE plpgsql;


--------------------

CREATE OR REPLACE FUNCTION aggregation.acv_remove_aggregation_data(
    p_id bigint,
    p_user varchar
)
RETURNS jsonb AS $$
DECLARE
    v_updated_row_count int;
    v_new_log jsonb;
BEGIN
    -- 1. Siapkan log baru dalam format JSONB
    v_new_log := jsonb_build_object(
        'status', 'DELETE',
        'user_type', NULL,
        'user', p_user,
        'note', NULL,
        'date', TO_CHAR(CURRENT_TIMESTAMP, 'YYYY-MM-DD HH24:MI:SS')
    );

    -- 2. Proses Soft Delete & Append Log
    UPDATE aggregation.aggregate_configurations_version
    SET 
        gcv_is_deleted = true,
        gcv_updated_by = p_user,
        gcv_updated_date = CURRENT_TIMESTAMP,
        -- Operator || digunakan untuk append object ke dalam array jsonb
        gcv_activity_log = COALESCE(gcv_activity_log, '[]'::jsonb) || v_new_log,
        -- Reset flag active/last version agar tidak muncul di filter 'LAST_VERSION' atau 'LAST_ACTIVE'
        is_last_active = false,
        is_last_version = false
    WHERE gcv_id = p_id 
    AND gcv_is_deleted = false; -- Pastikan hanya update yang belum terhapus

    GET DIAGNOSTICS v_updated_row_count = ROW_COUNT;

    -- 3. Return status response 
    IF v_updated_row_count > 0 THEN
        RETURN jsonb_build_object(
            'status', 'SUCCESS',
            'message', 'Data with ID ' || p_id || ' has been soft deleted.',
            'gcv_id', p_id
        );
    ELSE
        RETURN jsonb_build_object(
            'status', 'ERROR',
            'message', 'Data not found or already deleted.',
            'gcv_id', p_id
        );
    END IF;

EXCEPTION WHEN OTHERS THEN
    RETURN jsonb_build_object(
        'status', 'EXCEPTION',
        'message', SQLERRM
    );
END;
$$ LANGUAGE plpgsql;


----------------------

CREATE OR REPLACE FUNCTION aggregation.acv_get_next_version(
    p_code varchar
)
RETURNS varchar AS $$
DECLARE
    v_year varchar(2);
    v_last_inc int;
BEGIN
    v_year := TO_CHAR(CURRENT_DATE, 'YY');
    
    -- Hanya ambil versi dari data yang TIDAK terhapus
    SELECT COALESCE(MAX(SUBSTR(gcv_version, 4)::int), 0) INTO v_last_inc
    FROM aggregation.aggregate_configurations_version
    WHERE gcv_code = p_code 
    AND gcv_is_deleted = false
    AND gcv_version LIKE v_year || '.%';

    RETURN v_year || '.' || LPAD((v_last_inc + 1)::text, 4, '0');
END;
$$ LANGUAGE plpgsql;


----------------------

CREATE SEQUENCE IF NOT EXISTS aggregation.seq_gcv_code_mla START 1;
SELECT setval(
    'aggregation.seq_gcv_code_mla', 
    (
        SELECT COALESCE(MAX(REPLACE(gcv_code, 'MLA', '')::int), 0) 
        FROM aggregation.aggregate_configurations_version
    ) + 1, 
    false
);


CREATE OR REPLACE FUNCTION aggregation.acv_get_next_code()
RETURNS varchar AS $$
BEGIN
    RETURN 'ACV' || nextval('aggregation.seq_gcv_code_mla')::text;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION aggregation.acv_upsert_aggregation(
    p_user character varying, 
    p_id bigint DEFAULT NULL::bigint, 
    p_name character varying DEFAULT NULL::character varying, 
    p_desc text DEFAULT NULL::text, 
    p_data_applied character varying DEFAULT NULL::character varying, 
    p_type character varying DEFAULT NULL::character varying, 
    p_json_list text DEFAULT NULL::text, 
    p_json_condition text DEFAULT NULL::text, 
    p_config_final text DEFAULT NULL::text
)
 RETURNS jsonb
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_target_id bigint;
    v_new_version varchar(10);
    v_current_status varchar(20);
    v_is_deleted boolean;
    v_new_log jsonb;
    v_do_insert boolean := false;
    v_final_code varchar(50);
BEGIN
    -- 1. Identifikasi Context
    IF p_id IS NOT NULL THEN
        SELECT gcv_status, gcv_is_deleted, gcv_code 
        INTO v_current_status, v_is_deleted, v_final_code
        FROM aggregation.aggregate_configurations_version 
        WHERE gcv_id = p_id;

        -- APPROVED = Harus New Version (Row Baru)
        IF v_current_status IS NULL OR v_is_deleted = true OR v_current_status = 'APPROVED' THEN
            v_do_insert := true;
        END IF;
    ELSE
        -- DATA BARU (Initial Create)
        v_do_insert := true;
        v_final_code := aggregation.acv_get_next_code();
    END IF;

    -- 2. Flag Maintenance
    UPDATE aggregation.aggregate_configurations_version 
    SET is_last_version = false 
    WHERE gcv_code = v_final_code AND gcv_is_deleted = false;

    -- 3. Audit Log
    v_new_log := jsonb_build_object(
        'status', CASE WHEN v_do_insert THEN 'CREATED' ELSE 'UPDATED' END,
        'user_type', 'MAKER',
        'user', p_user,
        'note', CASE WHEN v_do_insert AND p_id IS NOT NULL AND v_is_deleted = false 
                     THEN 'New version generated from ID ' || p_id ELSE 'Initial version/Update record' END,
        'date', TO_CHAR(CURRENT_TIMESTAMP, 'YYYY-MM-DD HH24:MI:SS')
    );

    IF v_do_insert THEN
        v_new_version := aggregation.acv_get_next_version(v_final_code);
        
        INSERT INTO aggregation.aggregate_configurations_version (
            gcv_code, gcv_name, gcv_desc, gcv_data_applied, gcv_type,
            gcv_json_list, gcv_json_condition, gcv_config_final,
            gcv_version, gcv_status, is_last_version, is_last_active, gcv_is_deleted,
            gcv_created_by, gcv_created_date, gcv_activity_log
        ) VALUES (
            v_final_code, p_name, p_desc, p_data_applied, p_type,
            p_json_list, p_json_condition, p_config_final,
            v_new_version, 'CREATED', true, false, false,
            p_user, CURRENT_TIMESTAMP, jsonb_build_array(v_new_log)
        )
        RETURNING gcv_id INTO v_target_id;
    ELSE
        UPDATE aggregation.aggregate_configurations_version
        SET 
            gcv_name = COALESCE(p_name, gcv_name),
            gcv_status = 'UPDATED',
            gcv_desc = COALESCE(p_desc, gcv_desc),
            gcv_data_applied = COALESCE(p_data_applied, gcv_data_applied),
            gcv_type = COALESCE(p_type, gcv_type),
            gcv_json_list = COALESCE(p_json_list, gcv_json_list),
            gcv_json_condition = COALESCE(p_json_condition, gcv_json_condition),
            gcv_config_final = COALESCE(p_config_final, gcv_config_final),
            gcv_updated_by = p_user,
            gcv_updated_date = CURRENT_TIMESTAMP,
            gcv_activity_log = COALESCE(gcv_activity_log, '[]'::jsonb) || v_new_log,
            is_last_version = true
        WHERE gcv_id = p_id
        RETURNING gcv_id INTO v_target_id;
    END IF;

    RETURN jsonb_build_object(
        'status', 'SUCCESS',
        'gcv_id', v_target_id,
        'gcv_code', v_final_code,
        'gcv_version', (SELECT gcv_version FROM aggregation.aggregate_configurations_version WHERE gcv_id = v_target_id)
    );
EXCEPTION WHEN OTHERS THEN
    RETURN jsonb_build_object('status', 'EXCEPTION', 'message', SQLERRM);
END;
$function$;


--------------

CREATE OR REPLACE FUNCTION aggregation.acv_approval_proc_aggregation(
    p_id bigint,
    p_user varchar,
    p_role varchar, -- 'CHECKER' atau 'APPROVER'
    p_action varchar, -- 'APPROVE' atau 'REJECT'
    p_note text DEFAULT NULL
)
RETURNS jsonb AS $$
DECLARE
    v_current_status varchar(50);
    v_code varchar(50);
    v_new_status varchar(50);
    v_new_log jsonb;
BEGIN
    -- 1. Ambil status saat ini
    SELECT gcv_status, gcv_code INTO v_current_status, v_code
    FROM aggregation.aggregate_configurations_version
    WHERE gcv_id = p_id AND gcv_is_deleted = false;

    IF v_current_status IS NULL THEN
        RETURN jsonb_build_object('status', 'ERROR', 'message', 'Data tidak ditemukan.');
    END IF;

    -- 2. Tentukan status baru berdasarkan Role & Action
    IF p_action = 'REJECT' THEN
        v_new_status := 'REJECTED';
    ELSIF p_role = 'CHECKER' AND v_current_status IN ('CREATED', 'UPDATED', 'REJECTED') THEN
        v_new_status := 'CHECKED';
    ELSIF p_role = 'APPROVER' AND v_current_status = 'CHECKED' THEN
        v_new_status := 'APPROVED';
    ELSE
        RETURN jsonb_build_object('status', 'ERROR', 'message', 'Flow approval tidak valid untuk status ' || v_current_status);
    END IF;

    -- 3. Siapkan Log
    v_new_log := jsonb_build_object(
        'status', v_new_status,
        'user_type', p_role,
        'user', p_user,
        'note', p_note,
        'date', TO_CHAR(CURRENT_TIMESTAMP, 'YYYY-MM-DD HH24:MI:SS')
    );

    -- 4. Update Data
    UPDATE aggregation.aggregate_configurations_version
    SET 
        gcv_status = v_new_status,
        gcv_updated_by = p_user,
        gcv_updated_date = CURRENT_TIMESTAMP,
        gcv_activity_log = COALESCE(gcv_activity_log, '[]'::jsonb) || v_new_log
    WHERE gcv_id = p_id;

    -- 5. Logic Khusus: Jika APPROVED, pindahkan flag is_last_active
    IF v_new_status = 'APPROVED' THEN
        -- Matikan active lama untuk code yang sama
        UPDATE aggregation.aggregate_configurations_version
        SET is_last_active = false
        WHERE gcv_code = v_code;

        -- Aktifkan yang sekarang
        UPDATE aggregation.aggregate_configurations_version
        SET is_last_active = true
        WHERE gcv_id = p_id;
    END IF;

    RETURN jsonb_build_object(
        'status', 'SUCCESS',
        'new_status', v_new_status,
        'gcv_id', p_id
    );

EXCEPTION WHEN OTHERS THEN
    RETURN jsonb_build_object('status', 'EXCEPTION', 'message', SQLERRM);
END;
$$ LANGUAGE plpgsql;

--------------


CREATE OR REPLACE FUNCTION aggregation.acv_copy_aggregation(
    p_id bigint,
    p_name varchar,
    p_user varchar
)
RETURNS jsonb AS $$
DECLARE
    v_src record;
    v_new_id bigint;
    v_new_code varchar(50);
    v_new_ver varchar(20);
    v_result jsonb;
BEGIN
    -- 1. Ambil data source
    SELECT * INTO v_src FROM aggregation.aggregate_configurations_version 
    WHERE gcv_id = p_id AND gcv_is_deleted = false;

    IF NOT FOUND THEN
        RETURN jsonb_build_object('status', 'ERROR', 'message', 'Source record not found or already deleted.');
    END IF;

    -- 2. Generate CODE BARU & Versi Awal (Reset ke YY.0001)
    v_new_code := aggregation.acv_get_next_code();
    v_new_ver := aggregation.acv_get_next_version(v_new_code);

    -- 3. Insert Row Baru sebagai Master Data Baru (Cloning)
    INSERT INTO aggregation.aggregate_configurations_version (
        gcv_code, gcv_version, gcv_status, is_last_active, is_last_version,
        gcv_name, gcv_desc, gcv_data_applied, gcv_type, 
        gcv_json_list, gcv_json_condition, gcv_config_final,
        gcv_activity_log, gcv_is_deleted, gcv_created_by, gcv_created_date
    ) VALUES (
        v_new_code, v_new_ver, 'CREATED', false, true,
        p_name, v_src.gcv_desc, v_src.gcv_data_applied, v_src.gcv_type,
        v_src.gcv_json_list, v_src.gcv_json_condition, v_src.gcv_config_final,
        jsonb_build_array(jsonb_build_object(
            'status', 'CREATED', 
            'user_type', 'MAKER', 
            'user', p_user,
            'note', 'Cloned as new master from Source ID ' || p_id || ' (Orig Code: ' || v_src.gcv_code || ')',
            'date', TO_CHAR(CURRENT_TIMESTAMP, 'YYYY-MM-DD HH24:MI:SS')
        )),
        false, p_user, CURRENT_TIMESTAMP
    ) RETURNING gcv_id INTO v_new_id;

    -- 4. Ambil output lengkap menggunakan fungsi detail yang sudah ada
    -- Ini supaya FE dapet skema object yang konsisten
    v_result := aggregation.acv_get_aggregation_detail(v_new_id);

    -- Tambahkan flag status success di root object (optional, biar makin asoy)
    RETURN v_result || jsonb_build_object('status', 'SUCCESS');

EXCEPTION WHEN OTHERS THEN
    RETURN jsonb_build_object('status', 'EXCEPTION', 'message', SQLERRM);
END;
$$ LANGUAGE plpgsql;

-----------

CREATE OR REPLACE FUNCTION aggregation.acv_rollback_aggregation(
    p_id bigint,
    p_user varchar
)
RETURNS jsonb AS $$
DECLARE
    v_src record;           -- Data sumber (yang mau diambil isinya)
    v_latest record;        -- Data ID tertinggi saat ini
    v_new_version varchar(20);
    v_new_name varchar(255);
    v_log_status varchar(20);
    v_new_log jsonb;
    v_target_id bigint;
BEGIN
    -- 1. Ambil data SUMBER (is_deleted = false)
    SELECT * INTO v_src 
    FROM aggregation.aggregate_configurations_version 
    WHERE gcv_id = p_id AND gcv_is_deleted = false;

    IF NOT FOUND THEN
        RETURN jsonb_build_object('status', 'ERROR', 'message', 'Source ID not found or deleted');
    END IF;

    -- 2. Cari data TERAKHIR (ID tertinggi) untuk code yang sama
    SELECT * INTO v_latest 
    FROM aggregation.aggregate_configurations_version 
    WHERE gcv_code = v_src.gcv_code AND gcv_is_deleted = false
    ORDER BY gcv_id DESC LIMIT 1;

    -- 3. Tentukan Status & Nama Baru
    -- Format Nama: {OriginalName}_{YYMMDD}_RB
    v_new_name := v_src.gcv_name || '_' || TO_CHAR(CURRENT_DATE, 'YYMMDD') || '_RB';
    
    -- Status Log mengikuti mekanisme (CREATED jika row baru, UPDATED jika nimpa)
    v_log_status := CASE WHEN v_latest.is_last_active = true THEN 'CREATED' ELSE 'UPDATED' END;

    v_new_log := jsonb_build_object(
        'status', v_log_status,
        'user_type', 'MAKER',
        'user', p_user,
        'note', 'Rollback content from ID ' || p_id || ' (Version ' || v_src.gcv_version || ')',
        'date', TO_CHAR(CURRENT_TIMESTAMP, 'YYYY-MM-DD HH24:MI:SS')
    );

    -- 4. EKSEKUSI MEKANISME
    IF v_latest.is_last_active = true THEN
        -- MEKANISME A: INSERT ROW BARU
        v_new_version := aggregation.acv_get_next_version(v_src.gcv_code);

        UPDATE aggregation.aggregate_configurations_version 
        SET is_last_version = false 
        WHERE gcv_code = v_src.gcv_code AND gcv_is_deleted = false;

        INSERT INTO aggregation.aggregate_configurations_version (
            gcv_code, gcv_version, gcv_status, is_last_active, is_last_version,
            gcv_name, gcv_desc, gcv_data_applied, gcv_type, 
            gcv_json_list, gcv_json_condition, gcv_config_final,
            gcv_activity_log, gcv_is_deleted, gcv_created_by, gcv_created_date
        ) VALUES (
            v_src.gcv_code, v_new_version, 'CREATED', false, true,
            v_new_name, v_src.gcv_desc, v_src.gcv_data_applied, v_src.gcv_type,
            v_src.gcv_json_list, v_src.gcv_json_condition, v_src.gcv_config_final,
            jsonb_build_array(v_new_log), false, p_user, CURRENT_TIMESTAMP
        ) RETURNING gcv_id INTO v_target_id;

    ELSE
        -- MEKANISME B: OVERWRITE ROW TERAKHIR
        UPDATE aggregation.aggregate_configurations_version
        SET 
            gcv_name = v_new_name,
            gcv_status = 'UPDATED',
            gcv_desc = v_src.gcv_desc,
            gcv_data_applied = v_src.gcv_data_applied,
            gcv_type = v_src.gcv_type,
            gcv_json_list = v_src.gcv_json_list,
            gcv_json_condition = v_src.gcv_json_condition,
            gcv_config_final = v_src.gcv_config_final,
            gcv_updated_by = p_user,
            gcv_updated_date = CURRENT_TIMESTAMP,
            gcv_activity_log = COALESCE(gcv_activity_log, '[]'::jsonb) || v_new_log,
            is_last_version = true
        WHERE gcv_id = v_latest.gcv_id
        RETURNING gcv_id INTO v_target_id;
    END IF;

    RETURN jsonb_build_object(
        'status', 'SUCCESS',
        'applied_status', v_log_status,
        'target_id', v_target_id,
        'source_id', p_id
    );

EXCEPTION WHEN OTHERS THEN
    RETURN jsonb_build_object('status', 'EXCEPTION', 'message', SQLERRM);
END;
$$ LANGUAGE plpgsql;

--------------------------

CREATE OR REPLACE FUNCTION aggregation.acv_log_aggregation(
    p_code varchar
)
RETURNS jsonb AS $$
DECLARE
    v_result jsonb;
BEGIN
    -- 1. Ambil meta data dari record terbaru (is_last_version)
    -- 2. Tarik list history dengan kriteria: APPROVED atau LATEST VERSION
    SELECT 
        jsonb_build_object(
            'name', main.gcv_name,
            'desc', main.gcv_desc,
            'logs', (
                SELECT jsonb_agg(
                    jsonb_build_object(
                        'id', sub.gcv_id,
                        'version', sub.gcv_version,
                        'status', sub.gcv_status,
                        'name', sub.gcv_name,
                        'desc', sub.gcv_desc,
                        'is_active', sub.is_last_active, -- Tambahan info biar FE tau mana yang lagi tayang
                        'date', TO_CHAR(sub.gcv_created_date, 'YYYY-MM-DD HH24:MI:SS')
                    ) ORDER BY sub.gcv_id DESC -- List dari yang terbaru ke terlama
                )
                FROM aggregation.aggregate_configurations_version sub
                WHERE sub.gcv_code = main.gcv_code 
                  AND sub.gcv_is_deleted = false
                  -- KRITERIA BARU: Tampilkan yang sudah Approved ATAU yang sedang jadi versi terakhir
                  AND (sub.gcv_status = 'APPROVED' OR sub.is_last_version = true)
            )
        ) INTO v_result
    FROM aggregation.aggregate_configurations_version main
    WHERE main.gcv_code = p_code 
      AND main.gcv_is_deleted = false 
      AND main.is_last_version = true
    LIMIT 1;

    -- Fallback jika data tidak ditemukan
    IF v_result IS NULL THEN
        RETURN jsonb_build_object(
            'name', NULL,
            'desc', NULL,
            'logs', '[]'::jsonb
        );
    END IF;

    RETURN v_result;

EXCEPTION WHEN OTHERS THEN
    RETURN jsonb_build_object('status', 'EXCEPTION', 'message', SQLERRM);
END;
$$ LANGUAGE plpgsql;

-------------------------------

CREATE OR REPLACE FUNCTION aggregation.acv_get_aggr_config_by_code(p_gc_code character varying)
 RETURNS json
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_result json;
BEGIN
    SELECT acv.gcv_config_final::json
    INTO v_result
    FROM aggregation.aggregate_configurations_version AS acv
    WHERE acv.is_last_active = TRUE 
    AND acv.gcv_code = p_gc_code;

    RETURN v_result;
END;
$function$
;



-- =============================================
-- Sampling BARU
-- =============================================

SELECT 
    gcv_id, 
    gcv_code, 
    gcv_name,
    gcv_version, 
    gcv_status, 
    is_last_active,
    is_last_version, 
    gcv_activity_log,
    gcv_created_by,
    gcv_updated_by 
FROM aggregation.aggregate_configurations_version AS acv
WHERE gcv_is_deleted = false
ORDER BY gcv_code, gcv_version ASC;

SELECT * FROM aggregation.aggregate_configurations_version ;

DELETE FROM aggregation.aggregate_configurations_version WHERE gcv_id = 10;

SELECT * FROM aggregation.acv_get_aggregation_detail(
    p_id => 6
)

----------

-- =================================================================================
-- SKENARIO 1: Siklus Normal (Happy Flow - New Master Data)
-- Status: Create Baru -> Checker OK -> Approver OK -> LIVE.
-- =================================================================================

-- 1. MAKER: Buat Master Data Pertama
-- Hasil: ID: 1, Code: MLA1, Version: 26.0001, Status: CREATED
SELECT aggregation.acv_upsert_aggregation(
    'Fadhly Permata', NULL, NULL, 'BCA KlikPay', 'Initial Config', 
    'PROD', 'PAYMENT', '{"m":1}', '{"c":1}', '{"f":1}'
);

-- 2. CHECKER: Verifikasi (ID: 1, Status: CREATED -> CHECKED)
SELECT aggregation.acv_approval_proc_aggregation(1, 'Budi Checker', 'CHECKER', 'APPROVE', 'Struktur valid');

-- 3. APPROVER: Sahkan (ID: 1, Status: CHECKED -> APPROVED)
-- Hasil: ID 1: is_last_active = TRUE (LIVE).
SELECT aggregation.acv_approval_proc_aggregation(1, 'Ali Approver', 'APPROVER', 'APPROVE', 'Gaspol!');


-- =================================================================================
-- SKENARIO 2: Siklus Revisi (Reject & Fix di Row yang Sama)
-- Status: Ditolak -> Maker benerin di ID yang sama (karena belum APPROVED).
-- =================================================================================

-- 1. MAKER: Create Master Data Kedua (ID: 2, Code: MLA2, Version: 26.0001)
SELECT aggregation.acv_upsert_aggregation('Fadhly Permata', NULL, NULL, 'Mandiri VA', 'Config Mandiri');

-- 2. CHECKER: REJECT (ID: 2, Status: CREATED -> REJECTED)
SELECT aggregation.acv_approval_proc_aggregation(2, 'Budi Checker', 'CHECKER', 'REJECT', 'JSON detail kurang');

-- 3. MAKER: Fix di ID yang sama (ID: 2, Status: REJECTED -> UPDATED)
SELECT aggregation.acv_upsert_aggregation('Fadhly Permata', 2, 'MLA2', 'Mandiri VA', 'Update JSON detail');

-- 4. FINISH APPROVAL (ID: 2 -> APPROVED & LIVE)
SELECT aggregation.acv_approval_proc_aggregation(2, 'Budi Checker', 'CHECKER', 'APPROVE', 'Sip');
SELECT aggregation.acv_approval_proc_aggregation(2, 'Ali Approver', 'APPROVER', 'APPROVE', 'Oke');


-- =================================================================================
-- SKENARIO 3: Siklus Naik Versi (Version Up - Insert New Row)
-- Status: Data sudah LIVE -> Maker edit -> Sistem paksa bikin Baris Baru.
-- =================================================================================

-- 1. MAKER: Edit ID 2 yang sudah APPROVED.
-- Hasil: Baris BARU (ID: 3), Code: MLA2, Version: 26.0002, Status: CREATED.
SELECT aggregation.acv_upsert_aggregation('Fadhly Permata', 2, 'MLA2', 'Mandiri VA V2', 'Update diskon 25%');

-- 2. APPROVAL (ID: 3 -> APPROVED)
-- Hasil: ID 3: is_last_active = TRUE | ID 2: is_last_active = FALSE (Archive).
SELECT aggregation.acv_approval_proc_aggregation(3, 'Budi Checker', 'CHECKER', 'APPROVE', 'V2 Oke');
SELECT aggregation.acv_approval_proc_aggregation(3, 'Ali Approver', 'APPROVER', 'APPROVE', 'V2 Live');


-- =================================================================================
-- SKENARIO 4: Skenario Gantung (Draft Pending)
-- Status: Draft dibuat tapi "nyangkut" di meja Checker/Approver.
-- =================================================================================

-- 1. MAKER: Create Master Ketiga (ID: 4, Code: MLA3, Version: 26.0001)
SELECT aggregation.acv_upsert_aggregation('Fadhly Permata', NULL, NULL, 'Promo Gantung', 'Belum di-check');

-- 2. KONDISI GANTUNG:
-- ID 4 akan muncul di Grid 'LAST_VERSION' tapi tidak akan ada di 'LAST_ACTIVE'.


-- =================================================================================
-- SKENARIO 5: Siklus Cloning (Copy to New Identity)
-- Status: Copy dari MLA2 (ID 3) -> Jadi Master Baru MLA4 (ID 5).
-- =================================================================================

-- 1. MAKER: Copy isi ID 3 ke Identitas Baru
-- Hasil: ID: 5, Code: MLA4, Version: 26.0001 (Reset), Status: CREATED.
SELECT aggregation.acv_copy_aggregation(3, 'BNI VA Promo', 'Fadhly Permata');

-- 2. APPROVAL (ID: 5 -> APPROVED)
SELECT aggregation.acv_approval_proc_aggregation(5, 'Budi Checker', 'CHECKER', 'APPROVE', 'BNI Oke');
SELECT aggregation.acv_approval_proc_aggregation(5, 'Ali Approver', 'APPROVER', 'APPROVE', 'BNI Live');


-- =================================================================================
-- SKENARIO 6: Siklus Rollback (Kembali ke Masa Lalu)
-- Status: Versi baru jelek, ambil isi dari versi lama yang bagus.
-- =================================================================================

-- 1. MAKER: Rollback isi MLA2 ke kondisi ID 2 (Versi 26.0001).
-- Karena ID terakhir MLA2 (ID 3) sudah APPROVED, sistem INSERT baris baru.
-- Hasil: ID: 6, Code: MLA2, Version: 26.0003, Status: CREATED (Isi nyontek ID 2).
SELECT aggregation.acv_rollback_aggregation(2, 'Fadhly Permata');


-- =================================================================================
-- SKENARIO 7: Monitoring & Audit
-- =================================================================================

-- Cek Audit Trail MLA1 (Mandiri)
SELECT aggregation.acv_log_aggregation('ACV2');

-- Cek Audit Trail MLA2 (Mandiri - Harusnya ada 3 versi: 0001, 0002, 0003

SELECT * FROM aggregation.acv_get_aggregation_grid(
    p_mode => 'ALL' -- 'LAST_VERSION', 'LAST_ACTIVE', 'ALL'
)

SELECT * FROM aggregation.acv_get_aggregation_detail(
    p_id => 1
)


----


SELECT acv.gcv_code, acv.gcv_config_final  FROM aggregation.aggregate_configurations_version AS acv