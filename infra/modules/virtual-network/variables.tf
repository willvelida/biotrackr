variable "vnet_name" {
  description = "The name of the Virtual Network"
  type = string
}

variable "location" {
  description = "The location of the Virtual Network"
  type = string
}

variable "resource_group_name" {
  description = "The name of the resource group in which the Virtual Network should be created"
  type = string
}

variable "tags" {
  description = "A map of tags to add to the Virtual Network"
  type = map(string)
}

variable "address_space" {
  description = "The address space that is used the Virtual Network"
  type = list(string)
}