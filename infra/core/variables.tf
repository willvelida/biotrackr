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