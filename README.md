# TMCryptoCore

TMCryptoCore is an **ASP.NET Core 8 Web API** that provides encryption and decryption services for the Globe Life application. It exposes simple HTTP endpoints that encrypt or decrypt strings using the **Triple DES (3DES)** algorithm, with optional session-based authentication backed by a SQL Server database.

---

## Main Functionality

### 1. Encryption

**Endpoint:** `POST /GlobeLife/Crypto/Encrypt/{plainString}`

- Accepts a plain-text string as a URL segment.
- Encrypts it with Triple DES and returns a **Base64-encoded** cipher string.
- Optionally validates the caller's session before encrypting (see [Session Validation](#3-session-validation)).

**Query parameters**

| Parameter      | Type   | Default | Description                                      |
|----------------|--------|---------|--------------------------------------------------|
| `CheckSession` | `bool` | `false` | When `true`, validates the `Auth` request header |

**Example**

```
POST https://localhost:5001/GlobeLife/Crypto/Encrypt/HelloWorld
```

Returns a Base64 string such as `eyJleGFtcGxlIjoidmFsdWUifQ==`.

On error the response starts with `Exception:` followed by the error message.

---

### 2. Decryption

**Endpoint:** `POST /GlobeLife/Crypto/Decrypt/{encryptedString}`

- Accepts a Base64-encoded cipher string produced by the Encrypt endpoint (or by the legacy VB COM+ counterpart).
- Decrypts it with Triple DES and returns the original **UTF-8 plain-text** string.
- Includes a byte-offset adjustment (`length - 1`) for compatibility with legacy VB COM+ encrypted data.

**Example**

```
POST https://localhost:5001/GlobeLife/Crypto/Decrypt/eyJleGFtcGxlIjoidmFsdWUifQ==
```

Returns `HelloWorld`.

On error the response starts with `Exception:` followed by the error message.

---

### 3. Session Validation

When `CheckSession=true` is passed to the Encrypt endpoint, the API reads a **GUID** from the `Auth` request header and validates it against the `ASPSessionState` table in SQL Server:

- If the GUID is not found → returns `TMCrypto:UserNotFound`
- If the session is older than **5 minutes** → returns `TMCrypto:SessionExpired`
- Otherwise → encryption proceeds normally

> **Note:** The 5-minute session timeout is currently hardcoded in `EncryptionController.cs`. For flexibility across environments, consider externalising this value to `appsettings.json`.

---

### 4. Welcome / Health Check

**Endpoint:** `GET /GlobeLife`

Returns the plain-text string `Welcome to Globe Life!` and can be used as a basic health check.

---

## Cryptographic Details

| Property  | Value                                                              |
|-----------|--------------------------------------------------------------------|
| Algorithm | Triple DES (3DES)                                                  |
| Key       | 16-byte hardcoded key                                              |
| IV        | 8-byte hardcoded initialization vector                            |
| Output    | Base64-encoded cipher text                                         |
| Input     | UTF-8 plain text                                                   |

> ⚠️ **Security Warning:** The encryption key and IV are currently hardcoded in `EncryptionController.cs`. Hardcoded cryptographic secrets are a critical vulnerability — if the source code is ever exposed, all encrypted data is compromised. Before deploying to any environment, migrate the key and IV to a secure secrets-management solution such as **Azure Key Vault**, **AWS Secrets Manager**, or at minimum **ASP.NET Core User Secrets / environment variables**, and remove the hardcoded values from source code entirely.

---

## Technology Stack

| Category         | Technology                                     |
|------------------|------------------------------------------------|
| Language         | C# (.NET)                                      |
| Framework        | ASP.NET Core 8                                 |
| Cryptography     | `System.Security.Cryptography` (Triple DES)    |
| Database ORM     | Entity Framework Core 5                        |
| Database         | SQL Server (Windows Integrated Authentication) |
| Hosting options  | Kestrel, IIS, IIS Express                      |

---

## Project Structure

```
TMCryptoCore/
├── Controllers/
│   ├── EncryptionController.cs   # Encrypt, Decrypt, and health-check endpoints
│   └── WeatherForecastController.cs  # Sample ASP.NET Core endpoint (not production)
├── DAL/
│   └── TMCryptoContext.cs        # EF Core DbContext for session state
├── Model/
│   └── ASPSessionState.cs        # ASPSessionState entity model
├── Program.cs                    # Application entry point
├── Startup.cs                    # DI registration and middleware pipeline
├── appsettings.json              # Production configuration (DB connection string)
├── appsettings.Development.json  # Development logging overrides
├── web.config                    # IIS hosting configuration
└── TMCryptoCore.csproj           # Project file and NuGet dependencies
```

---

## Configuration

The database connection string is set in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<server>;Database=<database>;Trusted_Connection=True;"
  }
}
```

Update `<server>` and `<database>` to match your SQL Server instance before running.

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server instance (required only when `CheckSession=true` is used)

### Run locally

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run (Kestrel — https://localhost:5001, http://localhost:5000)
dotnet run
```

### Publish

```bash
dotnet publish -c Release
# Output: bin/Release/net8.0/publish/
```

---

## API Summary

| Method | Route                                    | Description                    |
|--------|------------------------------------------|--------------------------------|
| POST   | `/GlobeLife/Crypto/Encrypt/{plainText}`  | Encrypt a plain-text string    |
| POST   | `/GlobeLife/Crypto/Decrypt/{cipherText}` | Decrypt a Base64 cipher string |
| GET    | `/GlobeLife`                             | Health check / welcome message |
