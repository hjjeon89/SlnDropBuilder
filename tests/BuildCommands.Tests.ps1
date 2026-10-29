#Requires -Version 5.1
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$check = Join-Path $root '.artifacts/test-check/build-commands'
New-Item -ItemType Directory -Path $check -Force | Out-Null
$utf8 = New-Object Text.UTF8Encoding($false)

& dotnet build (Join-Path $root 'SlnDropBuilder.csproj') --no-restore "-p:OutDir=$root/.artifacts/test-check/" -clp:ErrorsOnly
if ($LASTEXITCODE -ne 0) { throw 'App build failed.' }

$project = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Program.cs" />
    <Reference Include="SlnDropBuilder"><HintPath>../SlnDropBuilder.dll</HintPath></Reference>
  </ItemGroup>
</Project>
'@
[IO.File]::WriteAllText((Join-Path $check 'Check.csproj'), $project, $utf8)
$source = @'
using System.Reflection;
using SlnDropBuilder;

internal static class Check
{
    private static readonly MethodInfo Build = typeof(Form1).GetMethod("BuildTargetAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo Execute = typeof(Form1).GetMethod("ExecuteBuildAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo Log = typeof(Form1).GetField("_logBox", BindingFlags.NonPublic | BindingFlags.Instance)!;

    [STAThread]
    private static int Main(string[] args)
    {
        using Form1 form = new();
        _ = form.Handle;
        int result = 1;
        form.BeginInvoke(async () =>
        {
            try
            {
                await RunChecks(form, args[0]);
                if (args.Length > 1)
                    await CheckNet10(form, args[0], args[1]);
                else
                    Console.WriteLine("SKIP: .NET 10 project/solution checks require an installed .NET 10 SDK.");
                result = 0;
                Console.WriteLine("PASS: project output cleanup, failed project retry, normal Debug/Release, target working directory, MSB4803 fallback, fallback failure, unrelated error, active cancellation.");
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); }
            finally { Application.ExitThread(); }
        });
        Application.Run();
        return result;
    }

    private static async Task RunChecks(Form1 form, string root)
    {
        await CheckOutputCleanup(form, root);
        foreach (string mode in new[] { "Normal", "Framework", "FrameworkFails", "OtherError" })
        {
            string folder = Path.Combine(root, mode);
            Directory.CreateDirectory(folder);
            string project = Path.Combine(folder, "Fixture.proj");
            File.WriteAllText(project, $$"""
                <Project>
                  <Target Name="CheckWorkingDirectory" BeforeTargets="Clean;Restore;Build">
                    <Error Condition="'$(MSBuildStartupDirectory)' != '$(MSBuildProjectDirectory)'" Text="Build did not start in the target directory." />
                  </Target>
                  <Target Name="Clean" />
                  <Target Name="Restore" />
                  <Target Name="Build">
                    <Error Condition="'{{mode}}' == 'OtherError'" Code="TEST001" Text="Unrelated failure" />
                    <Error Condition="'{{mode}}' != 'Normal' and '$(MSBuildRuntimeType)' == 'Core'" Code="MSB4803" Text="LC requires Framework MSBuild" />
                    <Error Condition="'{{mode}}' == 'FrameworkFails'" Code="TEST002" Text="Framework failure" />
                    <MakeDir Directories="$(OutputPath)" />
                    <WriteLinesToFile File="$(OutputPath)result.txt" Lines="$(MSBuildRuntimeType)|$(Configuration)|$(Platform)" Overwrite="true" />
                  </Target>
                </Project>
                """);
            RichTextBox log = (RichTextBox)Log.GetValue(form)!;
            log.Clear();
            bool success = await (Task<bool>)Build.Invoke(form, [project, Path.Combine(folder, "output with spaces"), CancellationToken.None])!;
            bool expectedSuccess = mode is "Normal" or "Framework";
            Assert(success == expectedSuccess, $"{mode}: wrong result.\n{log.Text}");
            bool expectedRetry = mode is "Framework" or "FrameworkFails";
            Assert(log.Text.Contains("retrying with Visual Studio MSBuild") == expectedRetry, $"{mode}: wrong retry behavior.\n{log.Text}");
            if (expectedSuccess)
            {
                foreach (string configuration in new[] { "Debug", "Release" })
                {
                    string actual = File.ReadAllText(Path.Combine(folder, "output with spaces", "Fixture", configuration, "result.txt")).Trim();
                    string runtime = mode == "Normal" ? "Core" : "Full";
                    Assert(actual == $"{runtime}|{configuration}|Any CPU", $"{mode}: arguments were not preserved: {actual}");
                }
            }
        }

        string cancelProject = Path.Combine(root, "Cancel.proj");
        string marker = Path.Combine(root, $"started-{Guid.NewGuid():N}.txt");
        File.WriteAllText(cancelProject, $$"""
            <Project>
              <Target Name="Clean">
                <WriteLinesToFile File="{{System.Security.SecurityElement.Escape(marker)}}" Lines="started" />
                <Exec Command="powershell.exe -NoProfile -NonInteractive -Command &quot;Start-Sleep -Seconds 30&quot;" />
              </Target>
            </Project>
            """);
        using CancellationTokenSource cancellation = new(TimeSpan.FromSeconds(15));
        Task<bool> task = (Task<bool>)Build.Invoke(form, [cancelProject, Path.Combine(root, "cancel-output"), cancellation.Token])!;
        while (!File.Exists(marker) && !task.IsCompleted)
        {
            cancellation.Token.ThrowIfCancellationRequested();
            await Task.Delay(50);
        }
        Assert(File.Exists(marker), "Cancellation fixture did not start.");
        cancellation.Cancel();
        try { await task; throw new Exception("Cancellation was ignored."); }
        catch (OperationCanceledException) { }
    }

    private static async Task CheckOutputCleanup(Form1 form, string root)
    {
        string folder = Path.Combine(root, "project-cleanup");
        string output = Path.Combine(folder, "build") + Path.DirectorySeparatorChar;
        string runId = Guid.NewGuid().ToString("N");
        string kept = Path.Combine(output, "Unselected", "Debug", "keep.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(kept)!);
        File.WriteAllText(kept, runId);
        string rootFile = Path.Combine(output, "keep-root.txt");
        File.WriteAllText(rootFile, runId);
        string[] targets = new string[2];
        for (int i = 0; i < targets.Length; i++)
        {
            string name = i == 0 ? "Successful" : "Failed";
            string projectFolder = Path.Combine(folder, runId, name);
            Directory.CreateDirectory(projectFolder);
            targets[i] = Path.Combine(projectFolder, "DifferentProjectName.csproj");
            File.WriteAllText(targets[i], $$"""
                <Project>
                  <Target Name="Clean" />
                  <Target Name="Restore" />
                  <Target Name="Build">
                    <Error Condition="'{{name}}' == 'Failed' and !Exists('$(MSBuildProjectDirectory)/allow-build')" Code="TEST003" Text="Expected failure" />
                    <MakeDir Directories="$(OutputPath)" />
                    <WriteLinesToFile File="$(OutputPath)result.txt" Lines="{{runId}}" Overwrite="true" />
                  </Target>
                </Project>
                """);
            foreach (string configuration in new[] { "Debug", "Release" })
            {
                string stale = Path.Combine(output, name, configuration, "stale.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(stale)!);
                File.WriteAllText(stale, "obsolete");
            }
        }

        int failures = await (Task<int>)Execute.Invoke(form, [output, targets, 2, CancellationToken.None])!;
        Assert(failures == 1, "Mixed build did not report one failure.");
        Assert(File.Exists(kept) && File.ReadAllText(kept) == runId, "An unselected project's output was deleted.");
        Assert(File.Exists(rootFile) && File.ReadAllText(rootFile) == runId, "An unrelated file in the build root was deleted.");
        foreach (string name in new[] { "Successful", "Failed" })
        {
            foreach (string configuration in new[] { "Debug", "Release" })
                Assert(!File.Exists(Path.Combine(output, name, configuration, "stale.txt")), "Selected project retained stale output.");
        }

        string completed = Path.Combine(output, "Successful", "keep-completed.txt");
        File.WriteAllText(completed, runId);
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(targets[1])!, "allow-build"), "retry");
        failures = await (Task<int>)Execute.Invoke(form, [output, new[] { targets[1] }, 2, CancellationToken.None])!;
        Assert(failures == 0, "Retry did not succeed.");
        Assert(File.Exists(completed) && File.ReadAllText(completed) == runId, "Retry deleted a successful project's output.");
        foreach (string name in new[] { "Successful", "Failed" })
        {
            foreach (string configuration in new[] { "Debug", "Release" })
                Assert(File.ReadAllText(Path.Combine(output, name, configuration, "result.txt")).Trim() == runId, "Project output was not rebuilt or preserved.");
        }

        try
        {
            await (Task<int>)Execute.Invoke(form, [output, new[] { Path.Combine(folder, "..sln") }, 1, CancellationToken.None])!;
            throw new Exception("A target resolving to the build root was accepted.");
        }
        catch (InvalidOperationException) { }
        Assert(File.Exists(rootFile), "The build root was deleted.");
    }

    private static async Task CheckNet10(Form1 form, string root, string sdkVersion)
    {
        string folder = Path.Combine(root, "net10");
        string targetFolder = Path.Combine(folder, "target");
        string appFolder = Path.Combine(targetFolder, "App");
        string callerFolder = Path.Combine(folder, "caller");
        Directory.CreateDirectory(appFolder);
        Directory.CreateDirectory(callerFolder);
        File.WriteAllText(Path.Combine(callerFolder, "global.json"), """{"sdk":{"version":"99.0.100","rollForward":"disable"}}""");
        File.WriteAllText(Path.Combine(targetFolder, "global.json"), $$"""
            {
              "sdk": { "version": "{{sdkVersion}}", "rollForward": "disable" }
            }
            """);
        string project = Path.Combine(appFolder, "App.csproj");
        File.WriteAllText(project, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <NuGetAudit>false</NuGetAudit>
              </PropertyGroup>
              <Target Name="RecordSdk" AfterTargets="Build">
                <WriteLinesToFile File="$(OutputPath)sdk.txt" Lines="$(NETCoreSdkVersion)|$(TargetFramework)" Overwrite="true" />
              </Target>
            </Project>
            """);
        File.WriteAllText(Path.Combine(appFolder, "Program.cs"), "System.Console.WriteLine(\".NET 10\");");
        string solution = Path.Combine(targetFolder, "Fixture.slnx");
        File.WriteAllText(solution, """<Solution><Project Path="App/App.csproj" /></Solution>""");
        string originalDirectory = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = callerFolder;
            foreach (string target in new[] { project, solution })
            {
                string output = Path.Combine(folder, "output with spaces");
                bool success = await (Task<bool>)Build.Invoke(form, [target, output, CancellationToken.None])!;
                Assert(success, $".NET 10 target failed: {target}\n{((RichTextBox)Log.GetValue(form)!).Text}");
                Assert(Environment.CurrentDirectory == callerFolder, "Build changed the application's working directory.");
                foreach (string configuration in new[] { "Debug", "Release" })
                {
                    string result = Path.Combine(output, Path.GetFileNameWithoutExtension(target), configuration);
                    Assert(File.Exists(Path.Combine(result, "App.dll")), ".NET 10 assembly was not built.");
                    Assert(File.ReadAllText(Path.Combine(result, "sdk.txt")).Trim() == $"{sdkVersion}|net10.0", "Target global.json did not select the expected SDK.");
                }
            }
        }
        finally { Environment.CurrentDirectory = originalDirectory; }
        Console.WriteLine("PASS: .NET 10 project/SLNX Debug/Release builds use the target global.json despite an incompatible caller global.json.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
'@
[IO.File]::WriteAllText((Join-Path $check 'Program.cs'), $source, $utf8)
$sdks = @(& dotnet --list-sdks)
if ($LASTEXITCODE -ne 0) { throw 'Could not list installed SDKs.' }
$net10Sdk = $sdks | Where-Object { $_ -match '^10\.0\.\d+ ' } | ForEach-Object { ($_ -split ' ')[0] } | Select-Object -Last 1
$checkArguments = @($check)
if ($net10Sdk) { $checkArguments += $net10Sdk }
& dotnet run --project (Join-Path $check 'Check.csproj') -p:NuGetAudit=false -- @checkArguments
if ($LASTEXITCODE -ne 0) { throw 'Build command checks failed.' }
