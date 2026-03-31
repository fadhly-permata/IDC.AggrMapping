INSERT INTO "public"."menu_list" ("id", "mn_name", "mn_link", "mn_parentid", "mn_acl", "mn_order", "mn_icon", "breadcrumb", "mn_module", "mn_name_chi", "mn_breadcrumb_chi", "mn_name_bah", "mn_breadcrumb_bah", "mn_name_hin", "mn_breadcrumb_hin", "mn_source", "mn_icon_img", "mn_source_react") VALUES ((SELECT MAX(id) FROM public.menu_list)+1, 'Aggregation Configuration', '/aggr_config', 3, 'lvl_aggr_cofig', 58, NULL, 'Aggregation Configuration', 'CORE', 'Aggregation Configuration -chi', 'Aggregation Configuration -chi', 'Aggregation Configuration -bah', 'Aggregation Configuration -bah', 'Aggregation Configuration -hin', 'Aggregation Configuration -hin', 'main', NULL, 'parm-ims');

INSERT INTO "public"."role_master" ("rlm_id", "rlm_name", "rlm_code", "rlm_parentid", "rlm_order", "rlm_license_module", "rlm_is_action") VALUES ((SELECT MAX(rlm_id) FROM public.role_master)+1, 'Aggregation Configuration', 'lvl_aggr_cofig', 3, 24, 'CORE', NULL);


INSERT INTO "public"."role_master" ("rlm_id", "rlm_name", "rlm_code", "rlm_parentid", "rlm_order", "rlm_license_module", "rlm_is_action") VALUES ((SELECT MAX(rlm_id) FROM public.role_master)+1, 'Add', 'lvl_aggr_cofig_create', 0, 1, 'CORE', 't');

INSERT INTO "public"."role_master" ("rlm_id", "rlm_name", "rlm_code", "rlm_parentid", "rlm_order", "rlm_license_module", "rlm_is_action") VALUES ((SELECT MAX(rlm_id) FROM public.role_master)+1, 'Edit', 'lvl_aggr_cofig_modify', 0, 2, 'CORE', 't');

INSERT INTO "public"."role_master" ("rlm_id", "rlm_name", "rlm_code", "rlm_parentid", "rlm_order", "rlm_license_module", "rlm_is_action") VALUES ((SELECT MAX(rlm_id) FROM public.role_master)+1, 'Delete', 'lvl_aggr_cofig_delete', 0, 3, 'CORE', 't');

INSERT INTO "public"."role_master" ("rlm_id", "rlm_name", "rlm_code", "rlm_parentid", "rlm_order", "rlm_license_module", "rlm_is_action") VALUES ((SELECT MAX(rlm_id) FROM public.role_master)+1, 'View', 'lvl_aggr_cofig_view', 0, 4, 'CORE', 't');

INSERT INTO "public"."role_master" ("rlm_id", "rlm_name", "rlm_code", "rlm_parentid", "rlm_order", "rlm_license_module", "rlm_is_action") VALUES ((SELECT MAX(rlm_id) FROM public.role_master)+1, 'Copy', 'lvl_aggr_cofig_copy', 0, 5, 'CORE', 't');

INSERT INTO "public"."role_master" ("rlm_id", "rlm_name", "rlm_code", "rlm_parentid", "rlm_order", "rlm_license_module", "rlm_is_action") VALUES ((SELECT MAX(rlm_id) FROM public.role_master)+1, 'Checker', 'lvl_aggr_cofig_checker', 0, 6, 'CORE', 't');

INSERT INTO "public"."role_master" ("rlm_id", "rlm_name", "rlm_code", "rlm_parentid", "rlm_order", "rlm_license_module", "rlm_is_action") VALUES ((SELECT MAX(rlm_id) FROM public.role_master)+1, 'Approval', 'lvl_aggr_cofig_approval', 0, 7, 'CORE', 't');

INSERT INTO "public"."role_master" ("rlm_id", "rlm_name", "rlm_code", "rlm_parentid", "rlm_order", "rlm_license_module", "rlm_is_action") VALUES ((SELECT MAX(rlm_id) FROM public.role_master)+1, 'Log', 'lvl_aggr_cofig_auditrail', 0, 8, 'CORE', 't');
