# Why this DLL is here

`AWS.Distro.OpenTelemetry.AutoInstrumentation` 1.15.0 on NuGet references
`OpenTelemetry.Sampler.AWS, Version=1.0.0.0, PublicKeyToken=6ba7de5ce46d6af3` (ADOT's own build of the
X-Ray sampler) but does not ship it, so the app fails at startup with
`FileNotFoundException: OpenTelemetry.Sampler.AWS` in the auto-instrumentation log and exports no metrics.
The upstream `OpenTelemetry.Sampler.AWS` NuGet package is a different assembly (0.1.x, different key)
and cannot replace it.

This copy is taken unchanged from the official release asset
`aws-distro-opentelemetry-dotnet-instrumentation-linux-glibc-arm64.zip` (v1.15.0, `net/` folder).
The zip's SHA-256 was checked against the published `.sha256` file:
`67f983cd25bb37f55a3d59ae600a0c4f2d93ac1f21dc34c38e22d3d2853c3c18`.

It is managed code (any OS/CPU). Remove the `<Reference>` in Shop.Api/Shop.Web and this folder once a
fixed ADOT package is published.
