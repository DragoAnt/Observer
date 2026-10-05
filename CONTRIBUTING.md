# Contributing

Issues and pull requests are welcome. For anything larger than a small fix, open an issue first so the approach can be agreed before you write the code.

## Build and test

The shared MSBuild settings come from [DragoAnt.MSBuildKit](https://github.com/DragoAnt/MSBuildKit), committed under `.toolkit/`, so a plain clone builds:

```sh
git clone https://github.com/DragoAnt/Observer.git
cd Observer
```

You need the .NET SDK pinned in [global.json](./global.json), plus the .NET 8 runtime so the tests run for every target framework. Then run the same steps as CI:

```sh
dotnet restore
dotnet build -c Release --no-restore
dotnet test --solution DragoAnt.Observer.slnx -c Release --no-build
```

The stack:

- [xUnit v3](https://xunit.net/docs/getting-started/v3/cmdline) on [Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro) v2.
- [AwesomeAssertions](https://github.com/AwesomeAssertions/AwesomeAssertions) for assertions.
- [Verify](https://github.com/VerifyTests/Verify) snapshots, pinned to 32.0.0: later versions are expected to need a paid maintenance subscription, while 32.0.0 is MIT. When a snapshot changes on purpose, review the `*.received.*` file and replace the matching `*.verified.*` file with it.
- [package validation](https://learn.microsoft.com/dotnet/fundamentals/apicompat/package-validation/overview) on every pack, and a public API snapshot test per assembly (`ApiSurfaceTests`).
- [BenchmarkDotNet](https://benchmarkdotnet.org/) for performance claims.

## Public API

Both libraries build with `EnforcePublicApiDocs`: a public member without an XML doc comment fails the build. Write the comment for callers — what the member does and what they observe — not how it works inside.

The public surface is pinned by `ApiSurfaceTests`. A change to it updates the matching `*.verified.txt` file in the same pull request, and anything but an addition needs a major version.

## Build kit

`.toolkit/` is installed by DragoAnt.MSBuildKit; don't edit it by hand. Move to another kit release with its update script and commit the result:

```sh
sh .toolkit/update.sh --version 0.2.0      # or: pwsh .toolkit/update.ps1 -Version 0.2.0
```

Repository settings live in `Directory.Build.props` (target frameworks, copyright), `Directory.Version.props` (the next release's `VersionPrefix`, one version for every package of the repository) and `Directory.Packages.props` (package versions the kit does not provide).

## Pull requests

- Branch from `main` and target `main`.
- Add or update tests for every behavior change; add a regression test for a bug fix.
- Keep the build warning-free: warnings are treated as errors.
- Update `README.md` and `docs/` when usage changes; each package's readme is generated from `README.md` on `dotnet pack` (`<!-- nuget:only <PackageId> -->` blocks carry package-specific text).
- The `ci` workflow must pass on the pull request.

Releases are published to nuget.org by the maintainers from a GitHub release.

## Security

Do not open a public issue for a vulnerability — see [SECURITY.md](./SECURITY.md).
