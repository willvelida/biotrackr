<constants>
SERVERLESS_VULN_CATALOG: JSON<<
{
  "sls01": {"id": "SLS01:2017", "title": "Injection", "category": "Input Validation"},
  "sls02": {"id": "SLS02:2017", "title": "Broken Authentication", "category": "Identity and Access"},
  "sls03": {"id": "SLS03:2017", "title": "Sensitive Data Exposure", "category": "Data Protection"},
  "sls04": {"id": "SLS04:2017", "title": "XML External Entities", "category": "Input Validation"},
  "sls05": {"id": "SLS05:2017", "title": "Broken Access Control", "category": "Identity and Access"},
  "sls06": {"id": "SLS06:2017", "title": "Security Misconfiguration", "category": "Configuration Management"},
  "sls07": {"id": "SLS07:2017", "title": "Cross-Site Scripting", "category": "Input Validation"},
  "sls08": {"id": "SLS08:2017", "title": "Insecure Deserialization", "category": "Input Validation"},
  "sls09": {"id": "SLS09:2017", "title": "Using Components with Known Vulnerabilities", "category": "Supply Chain"},
  "sls10": {"id": "SLS10:2017", "title": "Insufficient Logging and Monitoring", "category": "Observability"}
}
>>

SERVERLESS_VULN_CATEGORIES: JSON<<
["Configuration Management", "Data Protection", "Identity and Access", "Input Validation", "Observability", "Supply Chain"]
>>
</constants>
