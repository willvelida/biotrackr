variable "law_name" {
  description = "The name of the Log Analytics Workspace"
  type = string
}

variable "location" {
  description = "The location of the Log Analytics Workspace"
  type = string
}

variable "resource_group_name" {
  description = "The name of the resource group in which the Log Analytics Workspace should be created"
  type = string
}

variable "tags" {
  description = "A map of tags to add to the Log Analytics Workspace"
  type = map(string)
}