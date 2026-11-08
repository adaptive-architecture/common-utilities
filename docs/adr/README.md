# Architecture Decision Records

This directory records significant technical decisions for the Common Utilities repository, including the context, the decision, and the consequences. The format follows [Michael Nygard’s ADR pattern](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions).

| ADR | Title | Status |
|---|---|---|
| [0001](0001-multiple-base-paths.md) | Multiple application base paths | Accepted |
| [0002](0002-extensionless-static-files.md) | Extensionless static files middleware | Accepted |

## When to add an ADR

Add an ADR when a decision:

- changes the public API surface of a package;
- introduces or removes a dependency;
- affects how consumers deploy or operate an application;
- resolves a trade-off that is likely to be revisited; or
- is required by the project constitution’s “complex architectural decisions MUST be documented” rule.

Keep ADRs short and factual. Link to issues, documentation, and source where relevant. If a decision is later reversed, add a new ADR and mark the old one as `Superseded` rather than rewriting history.
