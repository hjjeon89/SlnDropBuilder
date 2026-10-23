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
  </PropertyGroup>
  <ItemGroup>
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
                result = 0;
                Console.WriteLine("PASS: normal Debug/Release, MSB4803 fallback, fallback failure, unrelated error, active cancellation.");
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); }
            finally { Application.ExitThread(); }
        });
        Application.Run();
        return result;
    }

    private static async Task RunChecks(Form1 form, string root)
    {
        foreach (string mode in new[] { "Normal", "Framework", "FrameworkFails", "OtherError" })
        {
            string folder = Path.Combine(root, mode);
            Directory.CreateDirectory(folder);
            string project = Path.Combine(folder, "Fixture.proj");
            File.WriteAllText(project, $$"""
                <Project>
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

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
'@
[IO.File]::WriteAllText((Join-Path $check 'Program.cs'), $source, $utf8)
& dotnet run --project (Join-Path $check 'Check.csproj') -p:NuGetAudit=false -- $check
if ($LASTEXITCODE -ne 0) { throw 'Build command checks failed.' }
