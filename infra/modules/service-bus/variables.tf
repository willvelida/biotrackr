variable "sb_name" {
  description = "The name of the Service Bus"
  type        = string
}

variable "location" {
  description = "The location of the Service Bus"
  type        = string
}

variable "rg_name" {
  description = "The name of the Resource Group"
  type        = string
}

variable "sb_sku" {
  description = "The SKU of the Service Bus"
  type        = string
  default     = "Basic"
}

variable "tags" {
  description = "The tags for the Service Bus"
  type        = map(string)
}

variable "identity_id" {
  description = "The ID of the user-assigned managed identity"
  type        = string
}