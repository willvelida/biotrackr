variable "env_name" {
  description = "The name of the environment"
  type = string
}

variable "location" {
  description = "The location of the environment"
  type = string
}

variable "resource_group_name" {
  description = "The name of the resource group in which the environment should be created"
  type = string
}

variable "tags" {
  description = "A map of tags to add to the environment"
  type = map(string)
}

variable "dapr_application_insights_connection_string" {
  description = "The connection string to the Application Insights instance used by Dapr"
  type = string
}

variable "infrastructure_subnet_id" {
  description = "The ID of the Subnet for the infrastructure"
  type = string
}

variable "log_analytics_workspace_id" {
  description = "The ID of the Log Analytics Workspace"
  type = string
}