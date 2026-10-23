# SlnDropBuilder

SlnDropBuilder is a Windows desktop utility for building multiple .NET executable projects from a dropped folder. It scans C# projects and solution files, lets you choose only the executable projects you want, and builds Debug and Release outputs with live logs.

## Features

- Drag and drop a root folder into the app.
- Automatically finds executable `.csproj` projects.
- Supports `.sln` and `.slnx` as fallback build targets when no project files are found.
- Excludes library projects from the selection list.
- Builds selected projects in Debug and Release configurations.
- Builds project references automatically through MSBuild.
- Automatically retries MSB4803 failures with Visual Studio MSBuild, including LC license compilation.
- Outputs files to `build/{Project}/Debug` and `build/{Project}/Release`.
- Supports parallel project builds with a configurable maximum parallel count.
- Shows a global log tab and separate per-project log tabs.
- Marks completed project tabs with `LightBlue` for success and `Orange` for failure.
- Can hide warning lines from the live log.
- Provides a rebuild button for the previous selection.

## Requirements

- Windows
- .NET 8 SDK or later
- Visual Studio or Build Tools with the required build components for projects that report MSB4803 (for example, the LC license compiler).

## Build

```powershell
dotnet build
```

## Build and install scripts

Double-click `build.cmd` to publish the app and its runtime to `build/Release`, or `install.cmd` to install it in `%LOCALAPPDATA%\Programs\SlnDropBuilder` and create a Start menu shortcut. Both batch files keep the console open so you can read the result.

Equivalent PowerShell commands:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\install.ps1
```

The scripts require 64-bit Windows and the entire repository. They reuse an installed .NET 8 or later SDK, or download .NET 8 from Microsoft into `%LOCALAPPDATA%\Microsoft\dotnet` if needed. SDK download and package restore require Internet access. No administrator rights are needed.

To choose another install folder or preview installation:

```powershell
.\install.ps1 -InstallDirectory 'D:\Apps\SlnDropBuilder'
.\install.ps1 -WhatIf
```

Close SlnDropBuilder before updating it. Copy the entire `build/Release` folder when distributing the app. Building other projects still requires a suitable .NET SDK; the app also searches the SDK folder provisioned by the installer. Failures report the step and a retained log in `%TEMP%`.

Run the script checks without downloading an SDK or changing the Start menu:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\Install.Tests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\Install.Tests.ps1 -BuildOnly
```

Run the build command checks (requires Visual Studio MSBuild) with:

```powershell
dotnet restore
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\BuildCommands.Tests.ps1
```

These checks cover normal Debug/Release builds, MSB4803 retry success and failure, unrelated errors, and cancellation while a command is running. Verification files are retained under `.artifacts/test-check`.

## Run

```powershell
dotnet run
```

Or run the generated executable from:

```text
bin/Debug/net8.0-windows/SlnDropBuilder.exe
```

## Output Structure

For each selected executable project:

```text
{DroppedFolder}/build/{Project}/Debug/
{DroppedFolder}/build/{Project}/Release/
```

`{Project}` is based on the final folder name that contains the selected `.csproj`.

## Notes

SlnDropBuilder itself is a WinForms app, but it can build executable projects such as WinForms, WPF, console apps, and other .NET projects. Commands use `dotnet msbuild`; on MSB4803, the failed command is retried with Visual Studio MSBuild discovered through `vswhere`, including Build Tools installations. License compilation remains enabled.

If a task such as `LC` reports MSB4803, the log shows `MSB4803: retrying with Visual Studio MSBuild (.NET Framework).` before retrying. If MSBuild cannot be found, install Visual Studio or Build Tools with the target project's required build components, then retry the build. The .NET SDK alone cannot run these tasks.

---

# SlnDropBuilder 한국어

SlnDropBuilder는 드래그 앤 드롭으로 선택한 폴더 안의 .NET 실행 프로젝트를 찾아 Debug와 Release로 빌드하는 Windows 데스크톱 유틸리티입니다. 여러 실행 프로젝트 중 필요한 것만 선택할 수 있고, 프로젝트별 로그를 분리해서 확인할 수 있습니다.

## 주요 기능

- 루트 폴더 드래그 앤 드롭 지원
- 실행 가능한 `.csproj` 프로젝트 자동 탐색
- 프로젝트 파일이 없을 경우 `.sln`, `.slnx`를 보조 빌드 대상으로 사용
- 라이브러리 프로젝트는 선택 목록에서 제외
- 선택한 프로젝트를 Debug와 Release 구성으로 빌드
- 실행 프로젝트가 참조하는 라이브러리는 MSBuild가 자동으로 함께 빌드
- LC 라이선스 컴파일 등에서 MSB4803 오류가 발생하면 Visual Studio MSBuild로 자동 재시도
- 결과물을 `build/{Project}/Debug`, `build/{Project}/Release`에 생성
- 최대 병렬 빌드 개수 설정 가능
- 전체 로그 탭과 프로젝트별 로그 탭 제공
- 빌드 성공 탭은 `LightBlue`, 실패 탭은 `Orange`로 표시
- warning 로그 숨김 옵션 제공
- 이전 선택 항목을 다시 빌드하는 Rebuild 버튼 제공

## 요구사항

- Windows
- .NET 8 SDK 이상
- MSB4803 오류가 발생하는 프로젝트는 필요한 빌드 구성 요소가 설치된 Visual Studio 또는 Build Tools 필요

## 빌드

```powershell
dotnet build
```

## 실행

```powershell
dotnet run
```

또는 생성된 실행 파일을 실행합니다.

```text
bin/Debug/net8.0-windows/SlnDropBuilder.exe
```

## 출력 구조

선택한 각 실행 프로젝트별로 아래 위치에 결과물이 생성됩니다.

```text
{선택한폴더}/build/{Project}/Debug/
{선택한폴더}/build/{Project}/Release/
```

`{Project}`는 선택한 `.csproj`가 들어있는 마지막 폴더명을 기준으로 합니다.

## 참고

SlnDropBuilder 자체는 WinForms 앱이지만, 빌드 대상은 WinForms에 한정되지 않습니다. WPF, 콘솔 앱, 기타 .NET 실행 프로젝트도 빌드할 수 있습니다. `dotnet msbuild`로 실행하며, MSB4803 오류가 발생하면 `vswhere`로 찾은 Visual Studio MSBuild로 재시도합니다. 이 경우 대상 프로젝트에 필요한 빌드 구성 요소가 포함된 Visual Studio 또는 Build Tools가 필요하며, 라이선스 컴파일은 유지됩니다.

`LC` 등의 작업에서 MSB4803 오류가 발생하면 로그에 `MSB4803: retrying with Visual Studio MSBuild (.NET Framework).`가 표시된 뒤 재시도합니다. MSBuild를 찾지 못하면 필요한 빌드 구성 요소가 포함된 Visual Studio 또는 Build Tools를 설치하고 다시 빌드하세요. .NET SDK만으로는 해당 작업을 실행할 수 없습니다.

## 빌드 명령 검증

Visual Studio MSBuild가 설치된 환경에서 다음 명령으로 검증합니다.

```powershell
dotnet restore
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\BuildCommands.Tests.ps1
```

일반 Debug·Release 빌드, MSB4803 재시도 성공·실패, 다른 오류 처리, 명령 실행 중 취소를 검증합니다. 검증 파일은 `.artifacts/test-check`에 보관합니다.
