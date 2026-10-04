# Lupira.Depz.Yarp

`/depz` roster for YARP BFFs: one anonymous `readyz` probe per `ReverseProxy:Clusters` entry.

```csharp
builder.Services.AddLupiraDepz(o => { /* … */ });
builder.Services.AddLupiraDepzYarpTargets(o => o.ServiceNames["cal-api"] = "lupira-cal-api");
```
