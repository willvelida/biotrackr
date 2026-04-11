# 07 Insufficient Binary Protection

Identifier: MOB07:2024
Category: Binary Security

## Description

Insufficient binary protection occurs when mobile application binaries lack adequate
defenses against reverse engineering and code tampering. App binaries can be downloaded from
app stores or copied from devices, making binary attacks easy to set up.
The binary may contain valuable secrets such as commercial API keys or hardcoded
cryptographic secrets. The code itself may be valuable due to critical business logic or
pre-trained AI models. Attackers may also analyze binaries to discover backend weaknesses.
Beyond information extraction, attackers can manipulate binaries to access paid features for
free, bypass security checks, or redistribute popular apps with malicious code injected.
Fully compiled iOS apps are generally less susceptible than higher-level bytecode found in
Android apps, though cross-platform technologies may reduce this advantage.

## Risk

- Leakage of commercial API keys, cryptographic secrets, and proprietary algorithms from
  the application binary.
- Theft of intellectual property including AI models and business logic.
- Bypassing of payment, license, and security checks through code tampering.
- Redistribution of tampered applications with malicious code through app stores.
- Financial damage from large-scale misuse of leaked API keys.
- Competitive disadvantage when proprietary algorithms or models are extracted by rivals.
- Reputational damage from unauthorized copies of the app distributed with malware.

## Vulnerability checklist

- Sensitive data such as API keys, cryptographic secrets, or credentials is hardcoded in
  the binary.
- The application binary is not obfuscated or only minimally obfuscated.
- No integrity checks are performed on the application binary at runtime.
- Proprietary algorithms or AI models are embedded in the binary without protection.
- License or payment checks are performed only on the client side without backend
  verification.
- The app lacks mechanisms to detect and report unauthorized redistribution.
- SUID/SGID binaries or debug symbols are present in release builds.

## Prevention controls

1. Apply obfuscation to the application binary using established free or commercial
   obfuscation tools to make reverse engineering significantly more difficult.
2. Compile sensitive parts of the application natively (iOS and Android) or use interpreters
   or nested virtual machines to increase reverse engineering complexity.
3. Never hardcode API keys, cryptographic secrets, or credentials in the binary; retrieve
   them from secure server-side sources at runtime.
4. Enforce security checks (license, payment, feature access) on the backend in addition
   to any local checks.
5. Implement runtime integrity checks to detect code tampering and render the installation
   unusable if modification is detected.
6. Implement redistribution detection mechanisms that automatically report unauthorized
   copies for removal from app stores.
7. Perform threat modeling to identify the highest-risk binary attack vectors and allocate
   protection effort proportionally to potential financial impact.
8. Test the quality of binary protections by using the same decompilation and analysis tools
   that attackers would use (MobSF, otool, apktool, Ghidra).

## Example attack scenarios

### Scenario A — Hardcoded API keys
An app uses a commercial API with per-call billing and hardcodes the API key in its
unprotected binary. An attacker reverse-engineers the app with free tools and extracts the
key. Since API access is protected only by the key with no additional user authentication,
the attacker freely uses the API or sells the key, causing substantial financial damage or
blocking legitimate users through rate limiting.

### Scenario B — Disabling payment and license checks
A mobile game ships all level resources with the app, protecting later levels with a
license check. An attacker reverse-engineers the app, locates the license verification,
replaces it with a static success statement, recompiles the app, and plays for free or
sells the cracked version through app stores.

### Scenario C — Hardcoded AI model extraction
A medical app includes a specialized, quality-assured AI model in its source code for
offline access. The model represents years of development effort. An attacker extracts the
model and its usage parameters from the insufficiently protected binary and sells them to
competitors.

## Detection guidance

- Inspect application binaries using decompilation tools (MobSF, otool, apktool, Ghidra)
  to assess the effectiveness of obfuscation and identify exposed secrets.
- Search decompiled code for hardcoded strings, API keys, and cryptographic material.
- Attempt to tamper with the binary and verify whether integrity checks detect and respond
  to the modification.
- Monitor app stores for unauthorized copies or repackaged versions of the application.
- Review release builds for the presence of debug symbols or development artifacts.
- Assess whether security checks (license, payment) can be bypassed through binary
  modification alone.

## Remediation

- Remove all hardcoded secrets, API keys, and credentials from the application binary.
- Apply comprehensive obfuscation to the release binary using industry-standard tools.
- Implement server-side enforcement for all license, payment, and feature access checks.
- Add runtime integrity verification to detect and respond to code tampering.
- Deploy redistribution monitoring to detect and remove unauthorized app copies.
- Protect proprietary algorithms and AI models through server-side execution or strong
  binary protection layers.
- Validate remediation by performing reverse engineering analysis on the obfuscated
  release binary.
