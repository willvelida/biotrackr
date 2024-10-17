resource_group_name = "rg-biotrackr-prod-ae"
location = "australiaeast"
tags = {
  environment = "Production"
  owner = "willvelida"
  Application = "biotrackr"
  Component = "Core-Infra"
}
law_name = "law-biotrackr-prod-ae"
vnet_name = "vnet-biotrackr-prod-ae"
vnet_address_space = [
    "10.0.0.0/16"
]
aca_subnet_name = "infrastructure-subnet"
aca_subnet_address_prefixes = [
    "10.0.0.0/23"
]
app_insights_name = "ai-biotrackr-prod-ae"
aca_env_name = "env-biotrackr-prod-ae"
acr_name = "acrbiotrackrprodae"
user_assigned_identity_name = "uai-biotrackr-prod-ae"
kv_name = "kv-biotrackr-prod-ae"
sb_name = "sb-biotrackr-prod-ae"
app_configuration_name = "appconfig-biotrackr-prod-ae"
activity_queue_name = "activity-queue"
activity_queue_key_name = "biotrackr:activity-queue"
sb_pubsub_component_name = "sb-pubsub"