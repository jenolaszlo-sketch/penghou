# Workflow.Abstractions 1.0.0 release lineage

The 1.0.0-rc.1 source is `b2366a2933298b1a7f8a8e0c632f3cfdbbdcac2c`. Exact artifact qualification on Windows, Linux and macOS ran 15 contract cases per net8.0/net10.0, plus neutral/published Zhinu source and unchanged preview.2 binary probes. Public NuGet payloads matched qualified bytes, repository signature excluded; fresh public-only consumers passed on all platforms/frameworks in [release run 38437789310 attempt2](https://github.com/jenolaszlo-sketch/penghou/actions/runs/38437789310/attempts/2). The first public attempt expired during indexing; no consumer gate was bypassed or package replaced. See [per-platform evidence](qualification/workflow-1.0.0-rc.1/summary.json).

Stable promotion changes only the NuGet version to 1.0.0, aligns SDK Version with PackageVersion, and selects published RC1 for package API validation. AssemblyVersion remains 1.0.0.0. No API/behavior/serialization/ownership change is introduced after RC. The final artifact must independently pass exact artifacts on three operating systems qualification, public payload comparison, fresh consumers, and unchanged RC-compiled binaries before its stable tag/status is recorded.

The RC informational-version metadata uses SDK default 1.0.0 plus source commit, while its exact NuGet identity is 1.0.0-rc.1. RC remains immutable and correctly identified by NuGet version and repository commit. Final/future metadata follows PackageVersion explicitly.

Preview-to-stable migration: update exact package pins; context equality now compares requirements structurally in declared order. Review HashSet/Dictionary assumptions and never persist process-local hashes. No version-one JSON data migration; host engine/journal/digest envelopes retain separate migration rules. Published Zhinu's old exact dependency pin was overridden only in an isolated compatibility probe: SUPPORTED WITH QUALIFICATION, not an adoption metadata claim. Hufu/Gagamba implementation remains excluded.

Non-goals: no runtime redesign, authority implementation, new capability, storage, codec/digest, framework, production-host authority certification or downstream graduation. Future package releases validate against published stable 1.0.0 baseline after completion.

## Final status

**Published and qualified: Penghou.Workflow.Abstractions 1.0.0.** [Stable run 38484433010](https://github.com/jenolaszlo-sketch/penghou/actions/runs/38484433010) passed every exact artifact/public gate on Windows, Linux and macOS, net8.0/net10.0, including unchanged RC-compiled neutral and published Zhinu consumer behavior. The stable source is `1c856745c58339b0ea190ff6d639f97dc65c0861`, tagged `workflow-v1.0.0`. See [per-platform final qualification](qualification/workflow-1.0.0/summary.json) and the [public package](https://www.nuget.org/packages/Penghou.Workflow.Abstractions/1.0.0).

No graduation blocker remains. The qualified release uses RC1 as its SDK baseline; before the next release select public stable 1.0.0, retaining the shipped API and documented 1.x promises. Future runtime/authority/codec work stays outside this foundation. Graduation is closed.
