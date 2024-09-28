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