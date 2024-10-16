variable "app_configuration_name" {
  description = "The name of the App Configuration instance"
  type        = string
}

variable "location" {
  description = "The location of the App Configuration instance"
  type        = string
}

variable "resource_group_name" {
  description = "The name of the resource group in which the App Configuration instance will be created"
  type        = string
}

variable "tags" {
  description = "A mapping of tags to assign to the App Configuration instance"
  type        = map(string)
}

variable "identity_id" {
  description = "The ID of the user-assigned identity to assign to the App Configuration instance"
  type        = string
}