# DragoAnt.Observer.Abstractions

The masking vocabulary of the DragoAnt observers, with no dependencies: `MaskKind` (`Full`, `Last4`, `Hash`, `Null`, `Custom`) and `MaskTag`, a kind plus an optional classification key that a custom strategy maps to its own masking. It targets `netstandard2.0`, so a model library can name how its values are masked without referencing an observer.

```csharp
using DragoAnt.Observer;

var tag = MaskTag.Custom("email");
Console.WriteLine(tag);
Console.WriteLine(MaskTag.Hash.WithKey("pii"));
// Custom (email)
// Hash (pii)
```

Observers that consume tags: [DragoAnt.System.Text.Json.Observer](https://www.nuget.org/packages/DragoAnt.System.Text.Json.Observer) for JSON. The format-neutral engine types are in [DragoAnt.Observer.Core](https://www.nuget.org/packages/DragoAnt.Observer.Core).

From version 1.1 this package will warn when a project builds with a .NET SDK older than 10 (the analyzers that ship then need it), and from 1.2 it will be an error; `<DragoAntObserverAllowOldCompiler>true</DragoAntObserverAllowOldCompiler>` keeps it a warning. Apps may still target `net8.0`.

- [Repository](https://github.com/DragoAnt/Observer) · [Issues](https://github.com/DragoAnt/Observer/issues) · [Changelog](https://github.com/DragoAnt/Observer/blob/main/CHANGELOG.md)
