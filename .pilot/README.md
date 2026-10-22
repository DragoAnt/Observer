# Package pilot

The package pilot runs the repository's own tests against the packages **published on nuget.org** instead of the projects in this repository. A green source run proves the code; the pilot proves what users download.

## Why it exists

The source build cannot see these failures, because it never touches a `.nupkg`:

- a file that is not packed — a readme, a build asset, a dependency's assembly;
- a wrong dependency or version range in the `.nuspec`, for example from [DragoAnt.Observer.Core](https://www.nuget.org/packages/DragoAnt.Observer.Core) to [DragoAnt.Observer.Abstractions](https://www.nuget.org/packages/DragoAnt.Observer.Abstractions);
- a type or member that only exists when the library is built from source;
- a published binary built from a different commit, configuration or target framework than the one tested.

## What it runs

[`Pilot.slnx`](./Pilot.slnx) holds one project, `DragoAnt.Observer.Tests`, which compiles the sources of [`../DragoAnt.Observer.Tests`](../DragoAnt.Observer.Tests) against the DragoAnt.Observer.Core package; Abstractions comes in as its dependency, exactly as a user gets it.

- **The sources are linked, never copied**: the pilot project compiles `..\..\DragoAnt.Observer.Tests\**\*.cs` with a wildcard, so a new test is in the pilot the moment it is in the repository. [Verify](https://github.com/VerifyTests/Verify) finds the `api/*.verified.txt` snapshots next to the original sources, because the compiler records the original file paths — so `ApiSurfaceTests` compares the public API of the **published** assemblies with the snapshot.
- **The pilot project keeps the original assembly name**, `DragoAnt.Observer.Tests`. Core carries `InternalsVisibleTo` for that name and is not strong-named, so the tests that reach internals still compile against the package.
- **The package readmes the doc tests read are the ones inside the packages** (`readme.md`, what nuget.org shows), not the repository files.
- The pilot is not in [`DragoAnt.Observer.slnx`](../DragoAnt.Observer.slnx): the normal build and CI never touch it.

## Run it

From the repository root (the [MTP](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro) runner is set in its `global.json`):

```sh
dotnet test --solution .pilot/Pilot.slnx -c Release                               # the version in Directory.Packages.props
dotnet test --solution .pilot/Pilot.slnx -c Release -p:PilotVersion=1.1.0         # another published version
```

`PilotVersion` is defined once, in [`Directory.Packages.props`](./Directory.Packages.props); no pilot project names a version. Both packages of this repository share one version, and the doc tests read the Abstractions readme from that version. Restore reads nuget.org only ([`nuget.config`](./nuget.config)). A package cache that already holds that version — from a local `dotnet pack`, say — would answer instead of nuget.org; to be sure, point the cache into the pilot (ignored by git):

```sh
NUGET_PACKAGES="$PWD/.pilot/packages" dotnet test --solution .pilot/Pilot.slnx -c Release
```

`obj/project.assets.json` of the pilot lists both libraries as `"type": "package"`.

## Test an older version

The tests must match the version they test: newer tests fail against an older package for no fault of the package. Check out that release's tag in a worktree and run the pilot there:

```sh
git worktree add --detach ../observer-pilot-old v1.0.0-beta.1
cp -r .pilot ../observer-pilot-old/   # only when the tag is older than the pilot
cd ../observer-pilot-old
dotnet test --solution .pilot/Pilot.slnx -c Release -p:PilotVersion=1.0.0-beta.1
```

A copied pilot works while the test project keeps its name and folder. Remove the worktree afterwards (`git worktree remove --force ../observer-pilot-old`).

## Reading the result

| Source run | Pilot | Meaning |
| --- | --- | --- |
| green | green | the published packages behave as the tested code |
| green | red | **a package differs from the source**: a file not packed, a dependency, a different build — look at the first failing test, not at the code |
| red | — | fix the code first; the pilot says nothing yet |

A test that can only pass when built from source is excluded by name in the pilot project and listed under known differences below, with the reason.

## Known differences

None: every test of the source run also runs, and passes, in the pilot.

## What it does not cover

- [Package validation](https://learn.microsoft.com/dotnet/fundamentals/apicompat/package-validation/overview) against the previous release — the pack step on CI checks it.
- `README.md` and `docs/` are repository files, not package content; the doc tests read them from the repository.
- Abstractions on a framework other than the ones the tests run (`netstandard2.0` consumers such as .NET Framework), the `.snupkg` symbols, Source Link and the signature.

## On CI

The pilot is not run by CI yet. After a release, a job could wait until the version is listed on nuget.org (`https://api.nuget.org/v3-flatcontainer/dragoant.observer.core/index.json`) and then run `dotnet test --solution .pilot/Pilot.slnx -c Release -p:PilotVersion=<tag without v>` on the release tag, on Ubuntu and Windows.
