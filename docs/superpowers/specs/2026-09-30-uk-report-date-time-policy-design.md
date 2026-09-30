# UK Report Date and Time Policy

## Goal

Standardize report date handling around the UK business timezone while making clock-dependent logic deterministic and making API date contracts explicit.

## Decisions

- Business/reporting dates are interpreted in `Europe/London`, including GMT/BST transitions.
- Existing persisted reporting date fields remain UK-local business values.
- Report date inputs use `DateOnly` semantics.
- Report ranges are inclusive to callers and normalized internally to an exclusive end date.
- Instant timestamps returned by the API use `DateTimeOffset`.
- Clock-dependent logic uses an injected `TimeProvider`.
- Estate-specific timezones are out of scope for this change.

## Scope

The policy applies to calendar defaults, transaction reports, settlement reports, file-import ranges, merchant KPI windows, and their tests and API contracts. UI/client changes are a follow-up job.

## Testing

Tests must cover fixed-clock behavior around midnight, UK daylight-saving transitions, inclusive report boundaries, calendar comparison dates, and settlement “today” handling.
