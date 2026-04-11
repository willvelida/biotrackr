# 10 Insufficient Cryptography

Identifier: MOB10:2024
Category: Cryptography

## Description

Insufficient cryptography occurs when mobile applications use weak, outdated, or improperly
implemented cryptographic mechanisms to protect sensitive information. This includes using
weak encryption algorithms, inadequate key lengths, poor key management practices, improper
handling of encryption keys, insecure random number generation, flawed implementation of
cryptographic protocols, and vulnerabilities in cryptographic libraries or frameworks.
Attackers may employ cryptographic attacks, brute force attacks, or side-channel attacks to
exploit these weaknesses. Insecure hash functions and deprecated cryptographic algorithms
pose additional risks, enabling attackers to reverse-engineer hashed data and reveal
original content.

## Risk

- Unauthorized retrieval of sensitive information from the mobile device.
- Data breaches exposing customer PII, financial details, or intellectual property.
- Loss of intellectual property through compromised encryption of proprietary algorithms
  and trade secrets.
- Financial losses from unauthorized access to payment data and associated fraud.
- Compliance and legal consequences from failure to meet regulatory encryption requirements.
- Man-in-the-middle attacks enabled by weak transport-layer cryptography.
- Credential exposure through weak password hashing allowing brute-force or rainbow table
  attacks.

## Vulnerability checklist

- Weak or deprecated encryption algorithms are used (e.g., DES, RC4, MD5).
- Encryption key lengths are insufficient for the algorithm in use.
- Encryption keys are stored insecurely on the device in plain text or easily accessible
  locations.
- Key management practices are poor, including insecure key generation, storage, or
  transmission.
- Random number generation uses insecure or predictable sources.
- Cryptographic protocols are implemented incorrectly or contain programming flaws.
- Secure transport layer protocols (HTTPS) are not used for encrypted data transmission.
- Password hashing lacks salting or uses weak salting methods.
- Key derivation functions are not used for password hashing.
- Custom encryption implementations are used instead of established cryptographic libraries.

## Prevention controls

1. Use strong, widely accepted encryption algorithms such as AES, RSA, or Elliptic Curve
   Cryptography (ECC) and avoid deprecated or weak algorithms.
2. Select encryption keys with appropriate lengths following industry recommendations for
   the specific algorithm.
3. Employ secure key management using key vaults or hardware security modules (HSMs) to
   store encryption keys, with restricted access and encryption of keys at rest.
4. Use established cryptographic libraries and frameworks rather than custom encryption
   implementations.
5. Store encryption keys securely on the device using operating system secure storage
   mechanisms or hardware-based secure storage.
6. Use secure transport layer protocols (HTTPS) for all encrypted data transmission with
   proper certificate validation.
7. Implement strong validation and authentication of parties involved in encryption
   processes.
8. Use cryptographically secure hash functions such as SHA-256 or bcrypt.
9. Always apply strong random salting when hashing passwords.
10. Use Key Derivation Functions (KDFs) such as PBKDF2, bcrypt, or scrypt for password
    hashing with appropriate iteration counts.
11. Regularly update cryptographic components and stay informed about developments from
    NIST and IETF.
12. Conduct cryptographic vulnerability assessments, penetration testing, and code reviews.

## Example attack scenarios

### Scenario A — Man-in-the-middle attack
An attacker intercepts communication between the mobile application and the server. Weak
cryptography enables the attacker to decrypt intercepted data, modify it, and re-encrypt it
before forwarding, leading to unauthorized access, data manipulation, or injection of
malicious content.

### Scenario B — Brute-force key recovery
An attacker systematically tries key combinations to decrypt data. Weak cryptography with
short key lengths shortens the time required for the attack, exposing sensitive information.

### Scenario C — Cryptographic downgrade attack
A mobile application supports multiple encryption protocols for secure connections. Weak
cryptography is allowed as a fallback option. An attacker forces the application to use weak
encryption, enabling easier decryption of intercepted data.

### Scenario D — Insecure key management exploitation
Encryption keys are stored insecurely on the device or are easily guessable. An attacker
gains unauthorized access to the keys and decrypts all encrypted data, resulting in data
breaches and privacy violations.

### Scenario E — Implementation flaw exploitation
A mobile application has implementation flaws including incorrect usage of cryptographic
libraries, insecure key generation, or improper random number generation. An attacker
exploits these flaws to bypass or weaken the encryption protections.

## Detection guidance

- Audit the codebase for use of deprecated or weak encryption algorithms (DES, RC4, MD5,
  SHA-1).
- Review encryption key lengths against current industry recommendations.
- Inspect key storage locations on the device for plaintext or weakly protected keys.
- Test random number generation sources for predictability.
- Review password hashing implementations for missing salt or use of weak hash functions.
- Verify TLS configuration for weak cipher suite allowances.
- Use cryptographic analysis tools to assess the strength of encryption implementations.
- Check for custom cryptographic code that should be replaced with established libraries.

## Remediation

- Replace all deprecated or weak encryption algorithms with current industry-standard
  alternatives (AES-256, RSA-2048+, SHA-256+).
- Increase encryption key lengths to meet or exceed current recommendations.
- Migrate encryption key storage to platform-provided secure storage (Keychain, Keystore,
  HSM).
- Implement proper key management lifecycle including secure generation, distribution,
  rotation, and destruction.
- Replace insecure random number generators with cryptographically secure alternatives.
- Replace custom cryptographic implementations with vetted, established libraries.
- Implement proper password hashing with strong KDFs, salting, and appropriate iteration
  counts.
- Validate remediation by conducting cryptographic vulnerability assessments on the
  release build.
