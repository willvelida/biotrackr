output "id" {
  value = azurerm_servicebus_namespace.sb.id
  description = "The ID of the Service Bus Namespace."
}

output "endpoint" {
  value = azurerm_servicebus_namespace.sb.endpoint
  description = "The endpoint for the Service Bus Namespace."
}