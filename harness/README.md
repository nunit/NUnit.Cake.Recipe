# Recipe source harness

This harness loads `../recipe/*.cake` directly. No published recipe package or
copied implementation is used. It runs under the repository's .NET 10 SDK policy
and pinned Cake.Tool 6.3.0, with Cake.Testing 6.3.0 for its test doubles.

From the repository root:

```sh
dotnet tool restore
dotnet cake --target=Test
```

The repository's `Test` target runs both harness targets in a separate Cake
process and fails if that process fails. This isolates the harness's temporary
recipe settings from the parent build. `BuildTestAndPackage` and
`ContinuousIntegration` run `Test` before packaging and publishing, retaining the
recipe's standard dependencies and explicit `--notests` option.

To run the standalone harness or select one of its targets directly:

```sh
dotnet cake harness/tests.cake --target=Harness --packageVersion=1.0.0 --nopush
```

The first run may download the tools and addins declared by the recipe and
harness. Those are bootstrap dependencies. The test cases do not contact NuGet,
MyGet, Chocolatey or GitHub, install packages, or publish anything. After bootstrap,
the same command works with those dependencies cached. This does not verify live
feed availability or NuGet's own version-selection algorithm.

The entry point permits only its three harness targets. It supplies a fixed
package version to avoid deriving one from Git. A dummy package satisfies the
recipe's normal setup; no packaging or publishing target runs.

## Tests and reports

`Harness` runs both targets below. Either can also be selected individually.

| Target | Coverage | Report |
| --- | --- | --- |
| `Harness-Tests` | Deferred dependency checks, exact package IDs and versions, source/prerelease options, extension selectors, unresolved lookups, tool failures, unversioned checks and package-verification failure propagation | `harness/artifacts/tests.xml` |
| `Harness-Smoke` | Full source loading, recipe setup, Cake target execution, real filesystem package verification (including expected failure), and read-only startup of the Cake-installed release tool | `harness/artifacts/smoke.xml` |

Reports use JUnit XML. Every behavior case runs before the target fails for any
assertions. A failed target returns a nonzero exit code. Compilation/setup failures
can occur before report generation; CI should also check the command's exit code.

Behavior cases use real Cake contexts, aliases, globbing and NuGet argument
generation. Cake.Testing supplies an in-memory filesystem and process output; a
small `IProcessRunner` records requests and never starts a child process. Each
case gets fresh state. Windows and Linux fake environments exercise both path
forms on any host. These cases are **not** native Linux or macOS execution.

The smoke target uses a fresh directory under `harness/artifacts` and removes that
directory in `finally`. It neither executes NuGet nor changes any existing package
tree. During its deliberate missing-file verification, it captures the recipe's
log and asserts both the exception and the specific error diagnostic. A successful
run prints `Expected missing-file failure verified (payload.txt).` Unexpected
exceptions or diagnostics still fail the target and appear in the XML report.

The release-tool smoke case executes only `showconfig --no-logo`; it performs no
release operation. Both CI workflows also check the local-manifest release tool
after restore. GitReleaseManager 0.20.0 still targets older runtimes, so its local
manifest enables roll-forward and recipe calls supply `DOTNET_ROLL_FORWARD=Major`
only to that tool's child process. No job-wide runtime override is used.

## Regression history

The original six diagnostic cases are represented here by deferred construction,
lookup on application, four deferred agent declarations, production prerelease
source selection, rejecting unresolved latest versions, and preserving an
intentional unversioned check. The agent declarations use the corrected call-site
form from nunit-console #1944: apply `LatestStableRelease` to `HasDependency(...)`,
rather than resolving an `ExtensionSpecifier` while constructing the declaration.
Explicit extension-version selection remains eager and is covered separately.

The unresolved-lookup matrix covers all three selectors with an empty response or
only a similarly named package, zero/one/two installed versions, and originally
pinned package references. Verification must abort with a diagnostic identifying
the requested package. An intentionally unversioned check still accepts a single
installed version and rejects missing or ambiguous installations without a feed
lookup.

On main commit `4554436d`, 40 of the 102 behavior cases pass and 62 expose #130;
the smoke case passes. Those regression assertions deliberately remain active.
The separate #130 correction changes the unresolved lookup into a clear exception.
Keep recipe corrections separate from harness infrastructure when reviewing the
commits. CI invokes the harness through the normal `Test` target rather than a
separate workflow step. A failed harness or smoke test prevents packaging and
publishing, and the XML reports are uploaded even when a test fails.
