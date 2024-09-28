variable "app_insights_name" {
  description = "The name of the Application Insights instance"
  type = string
}

variable "location" {
  description = "The location of the Application Insights instance"
  type = string
}

variable "resource_group_name" {
  description = "The name of the resource group in which the Application Insights instance should be created"
  type = string
}

variable "workspace_id" {
  description = "The ID of the Log Analytics Workspace to link to the Application Insights instance"
  type = string
}

variable "tags" {
  description = "A map of tags to add to the Application Insights instance"
  type = map(string)
}