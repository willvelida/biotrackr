<constants>
CICD_VULN_CATALOG: JSON<<
{
  "cicd_sec_01": {"id": "CICD-SEC-1:2025", "title": "Insufficient Flow Control Mechanisms", "category": "Flow Control"},
  "cicd_sec_02": {"id": "CICD-SEC-2:2025", "title": "Inadequate Identity and Access Management", "category": "Identity Management"},
  "cicd_sec_03": {"id": "CICD-SEC-3:2025", "title": "Dependency Chain Abuse", "category": "Supply Chain"},
  "cicd_sec_04": {"id": "CICD-SEC-4:2025", "title": "Poisoned Pipeline Execution", "category": "Pipeline Security"},
  "cicd_sec_05": {"id": "CICD-SEC-5:2025", "title": "Insufficient PBAC", "category": "Access Controls"},
  "cicd_sec_06": {"id": "CICD-SEC-6:2025", "title": "Insufficient Credential Hygiene", "category": "Credential Management"},
  "cicd_sec_07": {"id": "CICD-SEC-7:2025", "title": "Insecure System Configuration", "category": "Configuration Management"},
  "cicd_sec_08": {"id": "CICD-SEC-8:2025", "title": "Ungoverned Usage of 3rd Party Services", "category": "Third-Party Governance"},
  "cicd_sec_09": {"id": "CICD-SEC-9:2025", "title": "Improper Artifact Integrity Validation", "category": "Artifact Integrity"},
  "cicd_sec_10": {"id": "CICD-SEC-10:2025", "title": "Insufficient Logging and Visibility", "category": "Logging and Visibility"}
}
>>

CICD_VULN_CATEGORIES: JSON<<
["Access Controls", "Artifact Integrity", "Configuration Management", "Credential Management", "Flow Control", "Identity Management", "Logging and Visibility", "Pipeline Security", "Supply Chain", "Third-Party Governance"]
>>
</constants>
