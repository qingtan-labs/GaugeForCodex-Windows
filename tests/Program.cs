using System.Text.Json;
using GaugeForCodex;

var count = 0;
void Assert(bool condition, string name) { if (!condition) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); count++; }
void Reject(Action action, string name) { try { action(); } catch (System.IO.InvalidDataException) { Assert(true,name); return; } throw new Exception("FAIL: " + name); }
List<QuotaPeriod> Parse(string json) { using var doc = JsonDocument.Parse(json); return QuotaClient.ParseResult(doc.RootElement); }
var codex = Parse("""{"rateLimitsByLimitId":{"other":{"primary":{"usedPercent":99}},"codex":{"primary":{"usedPercent":23,"resetsAt":2000}}}}""");
Assert(codex.Count == 1 && codex[0].UsedPercent == 23, "Prefer Codex bucket");
var multiple = Parse("""{"rateLimitsByLimitId":{"codex":{"primary":{"usedPercent":10,"windowDurationMins":10080},"secondary":{"usedPercent":84,"windowDurationMins":300}}}}""");
Assert(new QuotaSnapshot(multiple,DateTimeOffset.Now).Primary?.RemainingPercent == 16, "Tightest quota window");
var shortWindow=new QuotaPeriod(2,DateTimeOffset.Now.AddHours(3),300,"secondary");
var weeklyWindow=new QuotaPeriod(11,DateTimeOffset.Now.AddDays(6),10080,"primary");
var both=new QuotaSnapshot([weeklyWindow,shortWindow],DateTimeOffset.Now);
Assert(both.DisplayPeriod==shortWindow && both.Primary==weeklyWindow, "Show actual five-hour window without changing tightest-limit logic");
Assert(both.AdditionalPeriods.SequenceEqual([weeklyWindow]), "Details contain only additional weekly window");
var single=new QuotaSnapshot([weeklyWindow],DateTimeOffset.Now);
Assert(single.DisplayPeriod==weeklyWindow && single.AdditionalPeriods.Count==0, "Weekly-only data has no duplicate detail");
Assert(new QuotaSnapshot([],DateTimeOffset.Now).DisplayPeriod is null && new QuotaSnapshot([],DateTimeOffset.Now).AdditionalPeriods.Count==0, "Empty snapshot has no display window or details");
Assert(new QuotaSnapshot([shortWindow],DateTimeOffset.Now).AdditionalPeriods.Count==0, "Five-hour-only data has no detail");
var duplicate=new QuotaSnapshot([weeklyWindow,weeklyWindow with {Kind="secondary"}],DateTimeOffset.Now);
Assert(duplicate.DisplayPeriods.Count==1 && duplicate.AdditionalPeriods.Count==0, "Duplicate server fields do not create duplicate detail");
var conflicting=new QuotaSnapshot([weeklyWindow,weeklyWindow with {UsedPercent=90,Kind="secondary"}],DateTimeOffset.Now);
Assert(conflicting.DisplayPeriods.Count==1 && conflicting.DisplayPeriod?.UsedPercent==90, "Repeated window uses conservative usage value");
var lowWeek=weeklyWindow with {UsedPercent=95};
Assert(new QuotaSnapshot([shortWindow,lowWeek],DateTimeOffset.Now).OtherLowPeriod==lowWeek, "Low weekly allowance remains visible outside expanded detail");
Assert(both.OtherLowPeriod is null, "Normal weekly allowance does not create a warning");
var unknownPrimary=new QuotaPeriod(20,null,0,"primary");
Assert(new QuotaSnapshot([unknownPrimary,unknownPrimary with {Kind="secondary"}],DateTimeOffset.Now).DisplayPeriods.Count==1, "Identical unknown-duration fields are deduplicated");
Assert(new QuotaSnapshot([unknownPrimary,unknownPrimary with {UsedPercent=80,Kind="secondary"}],DateTimeOffset.Now).DisplayPeriods.Count==2, "Different unknown-duration windows remain distinct");
Assert(!JsonSerializer.Serialize(both).Contains("DisplayPeriod",StringComparison.Ordinal), "Derived UI projection is not persisted as account data");
Assert(Parse("""{"rateLimits":{"primary":{"remainingPercent":62,"resetAt":3000}}}""")[0].UsedPercent == 38, "Legacy remaining percent");
Assert(Parse("""{"primary":{"usedPercent":"50","resetsAt":2000000000000}}""")[0].ResetsAt?.ToUnixTimeSeconds() == 2000000000, "Milliseconds normalized");
Assert(Parse("""{"primary":{"usedPercent":7,"resetsAt":"2026-10-06T00:58:00Z"}}""")[0].ResetsAt?.Hour == 0, "ISO reset timestamp");
Assert(Parse("""{"rateLimitsByLimitId":{"unrelated":{"primary":{"usedPercent":99}}}}""").Count == 0, "Never show unrelated quota");
Assert(Parse("""{"primary":{"usedPercent":300}}""")[0].UsedPercent == 100, "Clamp percentage");
Assert(Parse("""{"primary":{"usedPercent":"NaN"}}""").Count == 0, "Reject non-finite percentage");
Assert(Parse("""{"primary":{"usedPercent":3,"windowDurationMins":"NaN"}}""")[0].DurationMinutes == 0, "Normalize invalid duration");
Assert(Parse("[]").Count == 0 && Parse("{}").Count == 0, "Unknown payload is unavailable, not zero");
var now = DateTimeOffset.Parse("2026-09-30T08:00:00Z");
Assert(!PreferenceStore.IsWeeklyCheckDue(now.AddDays(-6),now), "Weekly check waits seven days");
Assert(PreferenceStore.IsWeeklyCheckDue(now.AddDays(-7),now), "Weekly check due at seven days");
Assert(PreferenceStore.IsWeeklyCheckDue(null,now), "Initial update check");
Assert(PreferenceStore.IsWeeklyCheckDue(now.AddDays(2),now), "Recover from clock rollback");
Assert(UpdateClient.TryVersion("v0.2.0",out var valid) && valid == new Version(0,2,0), "Accept stable semantic version");
Assert(!UpdateClient.TryVersion("v0.2.0-beta",out _) && !UpdateClient.TryVersion("v1.2",out _) && !UpdateClient.TryVersion("v01.2.0",out _), "Reject ambiguous/prerelease version");
var filename = "GaugeForCodex-Windows-0.2.0-win-x64.zip";
string Release(bool draft=false, bool prerelease=false, string host="github.com", string tag="v0.2.0") => JsonSerializer.Serialize(new {
    draft, prerelease, tag_name=tag, body="Example changes", assets=new[] {
        new { name=filename,browser_download_url=$"https://{host}/{AppPaths.Repository}/releases/download/{tag}/{filename}" },
        new { name="SHA256SUMS.txt",browser_download_url=$"https://{host}/{AppPaths.Repository}/releases/download/{tag}/SHA256SUMS.txt" }
    }
});
ReleaseInfo? ReadRelease(string json) { using var doc=JsonDocument.Parse(json);return UpdateClient.ParseRelease(doc.RootElement,"x64",new Version(0,1,0)); }
Assert(ReadRelease(Release())?.Version == new Version(0,2,0), "Choose exact architecture package");
Assert(ReadRelease(Release(draft:true)) is null && ReadRelease(Release(prerelease:true)) is null, "Never update to draft/prerelease");
Reject(()=>ReadRelease(Release(host:"evil.example")), "Reject untrusted update host");
Assert(!UpdateClient.AllowedAsset(new Uri($"https://github.com:444/{AppPaths.Repository}/releases/download/v0.2.0/{filename}"),"v0.2.0",filename), "Reject nonstandard update port");
var hash=new string('a',64);
Assert(UpdateClient.ChecksumFor($"{hash}  {filename}\n",filename)==hash, "Select exact checksum filename");
Reject(()=>UpdateClient.ChecksumFor($"{hash}  other.zip",filename), "Reject checksum for another file");
var root=Path.Combine(Path.GetTempPath(),"gauge-extraction-test");
Assert(UpdateClient.SafeEntryPath(root,"Assets/app.ico").StartsWith(Path.GetFullPath(root)), "Valid ZIP containment");
Reject(()=>UpdateClient.SafeEntryPath(root,"../escape.exe"), "Reject ZIP traversal");
Reject(()=>UpdateClient.SafeEntryPath(root,"C:/evil.exe"), "Reject absolute ZIP path");
Reject(()=>UpdateClient.SafeEntryPath(root,"evil\\path.exe"), "Reject backslash ZIP path");
foreach(var item in Text.Strings)
{
    Assert(item.Value.Length==4 && item.Value.All(v=>!string.IsNullOrWhiteSpace(v)), "Four translations: " + item.Key);
    var placeholders=item.Value.Select(v=>string.Join(",",System.Text.RegularExpressions.Regex.Matches(v,@"\{(\d+)\}").Select(m=>m.Groups[1].Value).Order())).Distinct().Count();
    if(placeholders!=1)throw new Exception("Localization placeholder mismatch: "+item.Key);
}
Assert(new Preferences().Topmost==false && new Preferences().WidgetVisible==false && new Preferences().FollowCodex, "Tray-first defaults, not always-on-top");
Assert(new Preferences().GlassTint==.58, "Desktop glass balances background visibility and text contrast at 58%");
Assert(PreferenceStore.ReadableTint(.16)==.56 && PreferenceStore.ReadableTint(.99)==.70, "Desktop tint protects contrast without turning opaque");
Assert(PreferenceStore.ReadableTint(double.NaN)==.58 && PreferenceStore.ReadableTint(double.PositiveInfinity)==.58, "Non-finite tint cannot make text disappear");
var oldGlass=new Preferences {GlassTint=.16,GlassStyleVersion=1,Left=186.6,Top=696,WidgetVisible=true};
Assert(PreferenceStore.UpgradeGlassReadability(oldGlass) && oldGlass.GlassTint==.58 && oldGlass.GlassStyleVersion==3, "Previously thin glass migrates to balanced defaults");
Assert(oldGlass.Left==186.6 && oldGlass.Top==696 && oldGlass.WidgetVisible, "Readability migration retains position and visibility");
Assert(!PreferenceStore.UpgradeGlassReadability(oldGlass), "Readability migration is idempotent");
var customGlass=new Preferences {GlassTint=.64,GlassStyleVersion=1};
Assert(PreferenceStore.UpgradeGlassReadability(customGlass) && customGlass.GlassTint==.64, "Balanced customized tint is preserved");
var editedGlass=new Preferences {GlassTint=.10,GlassStyleVersion=3};
Assert(PreferenceStore.UpgradeGlassReadability(editedGlass) && editedGlass.GlassTint==.56, "Manually edited unsafe tint is repaired");
var opaqueGlass=new Preferences {GlassTint=.80,GlassStyleVersion=2,Left=240,Top=758};
Assert(PreferenceStore.UpgradeGlassReadability(opaqueGlass) && opaqueGlass.GlassTint==.58 && opaqueGlass.Left==240 && opaqueGlass.Top==758, "White-looking version 2 tint is migrated without moving the widget");
var editedOpaque=new Preferences {GlassTint=1,GlassStyleVersion=3};
Assert(PreferenceStore.UpgradeGlassReadability(editedOpaque) && editedOpaque.GlassTint==.70, "Edited tint cannot hide the entire backdrop");
Console.WriteLine($"ALL {count} TESTS PASSED");
if(args.Contains("--live-update")) { var release=await new UpdateClient().Check(); Console.WriteLine(release is null ? "LIVE: current release is up to date" : "LIVE: newer stable release found"); }
