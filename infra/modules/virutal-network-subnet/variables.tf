variable "subnet_name" {
  description = "The name of the Subnet"
  type = string
}

variable "resource_group_name" {
  description = "The name of the resource group in which the Subnet should be created"
  type = string
}

variable "vnet_name" {
  description = "The name of the Virtual Network in which the Subnet should be created"
  type = string 
}

variable "subnet_address_prefixes" {
  description = "The address prefixes that should be used for the Subnet"
  type = list(string)
}