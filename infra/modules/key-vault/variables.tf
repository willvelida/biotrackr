variable "kv_name" {
  description = "Name of the Key Vault"
  type = string
}

variable "location" {
  description = "Location of the Key Vault"
  type = string
} 

variable "rg_name" {
  description = "Name of the Resource Group"
  type = string
}

variable "tags" {
  description = "Tags for the Key Vault"
  type = map(string)
}

variable "kv_sku_name" {
  description = "SKU of the Key Vault"
  type = string
}