# EstateReportingAPI

## Development HTTPS

The development profile listens on `https://localhost:5012` and keeps `http://localhost:5011` available during the HTTPS rollout. Kestrel loads its CA-issued server certificate from the `ServiceOptions:KestrelOptions` configuration section through `TransactionProcessing.CertificateDefaults`.

For a password-protected PFX, supply `ServiceOptions:KestrelOptions:Path` and `ServiceOptions:KestrelOptions:Password` through User Secrets locally or the deployment secret provider. For PEM, set `Path` to the full-chain PEM file and `KeyPath` to its separate private key. Mount certificate files read-only and restrict access to the service identity; do not commit certificate files, private keys, or passwords.

Callers must trust the issuing CA root and use the hostname present in the certificate SAN. The SecurityService URL remains on its current transport for this pilot; its HTTPS migration is tracked in [issue #671](https://github.com/TransactionProcessing/EstateReportingAPI/issues/671).

## Transaction report limits

The transaction reporting endpoints use bounded queries:

- Date-range reports accept an inclusive range of at most 30 calendar days.
- Paged reports accept `pageSize` values from 1 to 50; the default is 50, except recent activity, which defaults to 10.
- Transaction mix `topN` accepts values from 1 to 20 and defaults to 5.
- Merchant, operator, and product filter lists accept at most 50 IDs.

Paged responses expose `pagination` with `pageNumber`, `pageSize`, `totalItems`, and `totalPages`. Summary totals cover the complete filtered result set, while detail rows contain only the requested page. Invalid values return HTTP 400 using `ProblemDetails` with a human-readable `detail` value.
