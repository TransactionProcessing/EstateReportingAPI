# EstateReportingAPI

## Transaction report limits

The transaction reporting endpoints use bounded queries:

- Date-range reports accept an inclusive range of at most 30 calendar days.
- Paged reports accept `pageSize` values from 1 to 50; the default is 50, except recent activity, which defaults to 10.
- Transaction mix `topN` accepts values from 1 to 20 and defaults to 5.
- Merchant, operator, and product filter lists accept at most 50 IDs.

Paged responses expose `pagination` with `pageNumber`, `pageSize`, `totalItems`, and `totalPages`. Summary totals cover the complete filtered result set, while detail rows contain only the requested page. Invalid values return HTTP 400 using `ProblemDetails` with a human-readable `detail` value.
