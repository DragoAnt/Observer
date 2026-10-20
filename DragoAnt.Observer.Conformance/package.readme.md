# DragoAnt.Observer.Conformance

Conformance scenarios and invariants for formats built on [DragoAnt.Observer.Core](https://www.nuget.org/packages/DragoAnt.Observer.Core): one rule declaration, a document per format, and leak, truncation, status, policy, path, case, comment and round-trip checks run against any `IObserverFormat`.

Every type is experimental (`DAOEXP002`) until the case format is frozen.

Run the same seed against a format from an xUnit test:

```csharp
var report = await ConformanceKit.RunAsync(MyFormat.Instance, new ConformanceTraits());
Assert.Empty(report.Failures);
```

Source and docs: [DragoAnt/Observer](https://github.com/DragoAnt/Observer).
