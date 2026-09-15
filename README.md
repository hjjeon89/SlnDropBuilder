# SlnDropBuilder

SlnDropBuilder is a Windows desktop utility for building multiple .NET executable projects from a dropped folder. It scans C# projects and solution files, lets you choose only the executable projects you want, and builds Debug and Release outputs with live logs.

## Features

- Drag and drop a root folder into the app.
- Automatically finds executable `.csproj` projects.
- Supports `.sln` and `.slnx` as fallback build targets when no project files are found.
- Excludes library projects from the selection list.
- Builds selected projects in Debug and Release configurations.
- Builds project references automatically through MSBuild.
- Outputs files to `build/{Project}/Debug` and `build/{Project}/Release`.
- Supports parallel project builds with a configurable maximum parallel count.
- Shows a global log tab and separate per-project log tabs.
- Marks completed project tabs with `LightBlue` for success and `Orange` for failure.
- Can hide warning lines from the live log.
- Provides a rebuild button for the previous selection.

## Requirements

- Windows
- .NET 8 SDK or later

## Build

```powershell
dotnet build
```

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

SlnDropBuilder itself is a WinForms app, but it can build executable projects such as WinForms, WPF, console apps, and other .NET projects supported by `dotnet build`.

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
- 결과물을 `build/{Project}/Debug`, `build/{Project}/Release`에 생성
- 최대 병렬 빌드 개수 설정 가능
- 전체 로그 탭과 프로젝트별 로그 탭 제공
- 빌드 성공 탭은 `LightBlue`, 실패 탭은 `Orange`로 표시
- warning 로그 숨김 옵션 제공
- 이전 선택 항목을 다시 빌드하는 Rebuild 버튼 제공

## 요구사항

- Windows
- .NET 8 SDK 이상

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

SlnDropBuilder 자체는 WinForms 앱이지만, 빌드 대상은 WinForms에 한정되지 않습니다. `dotnet build`로 빌드 가능한 WPF, 콘솔 앱, 기타 .NET 실행 프로젝트도 빌드할 수 있습니다.
