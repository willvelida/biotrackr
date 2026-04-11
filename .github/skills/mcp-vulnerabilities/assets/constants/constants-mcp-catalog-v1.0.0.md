<constants>
MCP_VULN_CATALOG: JSON<<
{
  "mcp01": {"id": "MCP01:2025", "title": "Token Mismanagement and Secret Exposure", "category": "Credential Hygiene"},
  "mcp02": {"id": "MCP02:2025", "title": "Privilege Escalation via Scope Creep", "category": "Access Control"},
  "mcp03": {"id": "MCP03:2025", "title": "Tool Poisoning", "category": "Supply Chain / Integrity"},
  "mcp04": {"id": "MCP04:2025", "title": "Software Supply Chain Attacks and Dependency Tampering", "category": "Supply Chain / Integrity"},
  "mcp05": {"id": "MCP05:2025", "title": "Command Injection and Execution", "category": "Injection"},
  "mcp06": {"id": "MCP06:2025", "title": "Prompt Injection via Contextual Payloads", "category": "Injection"},
  "mcp07": {"id": "MCP07:2025", "title": "Insufficient Authentication and Authorization", "category": "Access Control"},
  "mcp08": {"id": "MCP08:2025", "title": "Lack of Audit and Telemetry", "category": "Observability"},
  "mcp09": {"id": "MCP09:2025", "title": "Shadow MCP Servers", "category": "Governance"},
  "mcp10": {"id": "MCP10:2025", "title": "Context Injection and Over-Sharing", "category": "Data Isolation"}
}
>>

VULN_CATEGORIES: JSON<<
["Access Control", "Credential Hygiene", "Data Isolation", "Governance", "Injection", "Observability", "Supply Chain / Integrity"]
>>
</constants>
