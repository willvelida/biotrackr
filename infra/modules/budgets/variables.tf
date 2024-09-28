variable "budget_name" {
  description = "The name of the budget"
  type = string
}

variable "resource_group_id" {
  description = "The ID of the resource group"
  type = string
}

variable "amount" {
  description = "The amount of the budget"
  type = number
  default = 200
}

variable "start_date" {
  description = "The start date of the budget"
  type = string
  default = "2024-09-01T00:00:00Z"
}

variable "end_date" {
  description = "The end date of the budget"
  type = string
  default = "2094-09-01T00:00:00Z"
}

variable "resource_group_name" {
  description = "The name of the resource group"
  type = string
}

variable "threshold_one" {
  description = "The first threshold of the budget"
  type = number
  default = 150
}

variable "threshold_two" {
  description = "The second threshold of the budget"
  type = number
  default = 190
}

variable "monitor_action_group_id" {
  description = "The ID of the Monitor Action Group"
  type = string
}