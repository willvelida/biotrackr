variable "amg_name" {
  description = "The name of the Monitor Action Group"
  type        = string
}

variable "rg_name" {
  description = "The name of the resource group in which the resources should be created"
  type        = string
}

variable "amg_short_name" {
  description = "The short name of the Monitor Action Group"
  type        = string
}

variable "tags" {
  description = "A map of tags to add to the resources"
  type        = map(string)
}

variable "email_receiver_name" {
  description = "The name of the email receiver"
  type        = string
}

variable "email_address" {
  description = "The email address of the email receiver"
  type        = string
}