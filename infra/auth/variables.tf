variable "resource_group_name" {
  description = "The name of the resource group in which the resources will be created."
  type        = string
}

variable "env_name" {
  description = "The name of the ACA environment."
  type        = string
}

variable "aca_app_name" {
  description = "The name of the ACA app."
  type        = string
}

variable "tags" {
  description = "A map of tags to add to the resources."
  type        = map(string)
}

variable "uai_name" {
  description = "The name of the user-assigned identity."
  type        = string
}

variable "acr_name" {
  description = "The name of the Azure Container Registry."
  type        = string
}

variable "image_name" {
  description = "The name of the image to use for the container."
  type        = string
}

variable "key_vault_name" {
  description = "The name of the key vault."
  type        = string
}