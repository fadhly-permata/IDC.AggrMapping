CREATE OR REPLACE FUNCTION "interface"."get_interface_aggr_lib"()
  RETURNS TABLE("gc_id" int8, "gc_name" varchar, "gc_desc" varchar, "gc_code" varchar) AS $BODY$
BEGIN
    RETURN QUERY 
    SELECT *
    FROM interface.dblink(
        'host=localhost port=5438 dbname=''idc.kaml'' user=postgres password=admin123'::text,
        $db$
        SELECT 
            gcv_id as gc_id,
            COALESCE(
                REGEXP_REPLACE(
                    gcv_name,
                    '[\[\]\(\)!\\/:\*\?"<>|{}]',
                    '',
                    'g'
                ),
                ''
            ) as gc_name,
            gcv_desc as gc_desc,
            gcv_code as gc_code 
        FROM aggregation.aggregate_configurations_version
        WHERE is_last_active = true
				AND lower(gcv_type) = 'response'
        $db$::text
    ) AS t(
        gc_id BIGINT,
        gc_name VARCHAR,
        gc_desc VARCHAR,
        gc_code VARCHAR
    );
END
$BODY$
  LANGUAGE plpgsql VOLATILE
  COST 100
  ROWS 1000;