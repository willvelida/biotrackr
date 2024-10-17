data "azurerm_subscription" "sub" {

}

data "azurerm_container_registry" "acr" {
  name = var.acr_name
  resource_group_name = var.resource_group_name
}

data "azurerm_app_configuration" "appconfig" {
  name = var.app_configuration_name
  resource_group_name = var.resource_group_name
}

module "tf-resource-group" {
  source   = "../modules/resource-group"
  name     = var.tf_state_rg_name
  location = var.location
  tags     = var.tags
}

module "identity-resource-group" {
  source   = "../modules/resource-group"
  name     = var.identity_rg_name
  location = var.location
  tags     = var.tags
}

module "tf-state-storage" {
  source                   = "../modules/tfstate-storage"
  storage_account_name     = var.storage_account_name
  resource_group_name      = module.tf-resource-group.name
  location                 = var.location
  tags                     = var.tags
  account_replication_type = var.account_replication_type
  account_tier             = var.account_tier
  container_names          = var.container_names
}

module "gh_usi" {
  source   = "../modules/user-assigned-identity"
  name     = "${var.gh_uai_name}-${var.environment}"
  location = var.location
  rg_name  = module.identity-resource-group.name
  tags     = var.tags
}

module "gh_federated_credential" {
  source                             = "../modules/federated-identity-credential"
  federated_identity_credential_name = "${var.github_organization_target}-${var.github_repository}-${var.environment}"
  rg_name                            = module.identity-resource-group.name
  user_assigned_identity_id          = module.gh_usi.user_assinged_identity_id
  subject                            = "repo:${var.github_organization_target}/${var.github_repository}:environment:${var.environment}"
  audience_name                      = local.default_audience_name
  issuer_url                         = local.github_issuer_url
}

module "gh_federated_credential-pr" {
  source                             = "../modules/federated-identity-credential"
  federated_identity_credential_name = "${var.github_organization_target}-${var.github_repository}-pr"
  rg_name                            = module.identity-resource-group.name
  user_assigned_identity_id          = module.gh_usi.user_assinged_identity_id
  subject                            = "repo:${var.github_organization_target}/${var.github_repository}:pull_request"
  audience_name                      = local.default_audience_name
  issuer_url                         = local.github_issuer_url
}

module "tfstate_role_assignment" {
  source       = "../modules/role-assignment"
  principal_id = module.gh_usi.user_assinged_identity_principal_id
  role_name    = "Storage Blob Data Contributor"
  scope_id     = module.tf-state-storage.id
}

module "sub_owner_role_assignment" {
  source       = "../modules/role-assignment"
  principal_id = module.gh_usi.user_assinged_identity_principal_id
  role_name    = var.owner_role_name
  scope_id     = data.azurerm_subscription.sub.id
}

module "acr_pull_role_assignment" {
  source       = "../modules/role-assignment"
  principal_id = module.gh_usi.user_assinged_identity_principal_id
  role_name    = "AcrPull"
  scope_id     = data.azurerm_container_registry.acr.id
}

module "acr_push_role_assignment" {
  source       = "../modules/role-assignment"
  principal_id = module.gh_usi.user_assinged_identity_principal_id
  role_name    = "AcrPush"
  scope_id     = data.azurerm_container_registry.acr.id
}

module "app_configuration_data_owner_role_assignment" {
  source = "../modules/role-assignment"
  principal_id = module.gh_usi.user_assinged_identity_principal_id
  role_name = "App Configuration Data Owner"
  scope_id = data.azurerm_app_configuration.appconfig.id
}