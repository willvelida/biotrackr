variable "resource_group_name" {
  description = "The name of the resource group in which the resources should be created"
  type        = string
}

variable "location" {
  description = "The location in which the resources should be created"
  type        = string
}

variable "tags" {
  description = "A map of tags to add to the resources"
  type        = map(string)
}

variable "law_name" {
  description = "The name of the Log Analytics Workspace"
  type        = string
}

variable "app_insights_name" {
  description = "The name of the Application Insights instance"
  type        = string
}

variable "vnet_name" {
  description = "The name of the Virtual Network"
  type        = string
}

variable "vnet_address_space" {
  description = "The address space that is used the Virtual Network"
  type        = list(string)
}

variable "aca_subnet_name" {
  description = "The name of the Subnet for the Azure Container Agent"
  type        = string
}

variable "aca_subnet_address_prefixes" {
  description = "The address prefixes that should be used for the Subnet for the Azure Container Agent"
  type        = list(string)
}

variable "aca_env_name" {
  description = "The name of the Container App environment"
  type        = string

}

variable "acr_name" {
  description = "The name of the Azure Container Registry"
  type        = string
}

variable "acr_admin_enabled" {
  description = "Enable admin user for the Azure Container Registry"
  type        = bool
  default     = true
}

variable "acr_sku" {
  description = "The SKU of the Azure Container Registry"
  type        = string
  default     = "Basic"
}

variable "user_assigned_identity_name" {
  description = "The name of the user-assigned managed identity that's used by the AKS cluster"
  type        = string
}

variable "acr_pull_role_name" {
  type        = string
  description = "The name of the AcrPull role given to the user-assigned identity"
  default     = "AcrPull"
}

variable "email_address" {
  description = "The email address of the user that will be assigned the AcrPull role"
  type        = string
  default     = "willvelida@hotmail.co.uk"
}

variable "email_receiver_name" {
  description = "The name of the user that will be assigned the AcrPull role"
  type        = string
  default     = "Will Velida (Biotrackr)"
}

variable "monitor_action_group_name" {
  description = "The name of the Monitor Action Group"
  type        = string
  default     = "Biotrackr-Action-Group"
}

variable "monitor_action_group_short_name" {
  description = "The short name of the Monitor Action Group"
  type        = string
  default     = "BAG"
}

variable "budget_name" {
  description = "The name of the budget"
  type        = string
  default     = "Biotrackr-Budget"
}

variable "kv_name" {
  description = "The name of the Key Vault"
  type        = string
}

variable "kv_sku_name" {
  description = "The SKU of the Key Vault"
  type        = string
  default     = "standard"
}

variable "sb_name" {
  description = "The name of the Service Bus"
  type        = string
}

variable "app_configuration_name" {
  description = "The name of the App Configuration instance"
  type        = string
}

variable "activity_queue_name" {
  description = "The name of the Activity Queue in Service Bus"
  type        = string
}

variable "activity_queue_key_name" {
  description = "The name of the Key in App Configuration for the Activity Queue"
  type        = string
}