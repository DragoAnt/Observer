# Experimental API

Some types of DragoAnt.Observer.Core are marked `[Experimental]`: they ship so that format packages can be built against them, and they may still change in a minor release. Using one is a build error until the project opts in to its diagnostic id.

| Id | Types | Why it is experimental |
| --- | --- | --- |
| `DAOEXP001` | `MaskRules`, `MaskRulesBuilder`, `MaskRuleBuilder`; `MaskRule`, `MaskRuleScope`, `MaskRuleAction` in `DragoAnt.Observer.Formats` | the format-neutral rules grow a text grammar and named rules |
| `DAOEXP002` | `IObserverFormat`, `IObserverFormatBinding` in `DragoAnt.Observer.Formats`; the conformance kit (`DragoAnt.Observer.Conformance`) | the format seam is shaped by the formats that implement it and by content negotiation |

Opt in for a project:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);DAOEXP001</NoWarn>
</PropertyGroup>
```

Or opt in for one file with `#pragma warning disable DAOEXP001`.
