variable "account_name" {
  description = "The name of the Cosmos DB account"
  type        = string
}

variable "location" {
  description = "The location of the Cosmos DB account"
  type        = string
}

variable "tags" {
  description = "A mapping of tags to assign to the resource"
  type        = map
}

variable "resource_group_name" {
  description = "value of the resource group name"
    type        = string
}

variable "database_name" {
  description = "The name of the Cosmos DB database"
  type        = string
}

variable "container_name" {
  description = "The name of the Cosmos DB container"
  type        = string
}

variable "throughput" {
  description = "The throughput of the Cosmos DB account"
  type        = number
  default = 1000
}