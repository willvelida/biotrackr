resource "azurerm_servicebus_queue" "queue" {
  name = var.queue_name
  namespace_id = var.namespace_id
}