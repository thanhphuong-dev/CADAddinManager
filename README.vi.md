# CAD Add-in Manager

[English](README.md) | [Tiếng Việt](README.vi.md)

CAD Add-in Manager là công cụ WPF dành cho quá trình phát triển add-in CAD. Công cụ cho phép nạp, kiểm tra và chạy các command .NET bên ngoài mà không phải liên tục khởi động lại phần mềm CAD.

## Phần mềm được hỗ trợ

- AutoCAD 2020–2026.
- VinaCAD 2026.07.x sử dụng VinaCAD/Teigha .NET 8 API.
- ZWCAD 2021 và ZWCAD 2026 sử dụng ZRX.NET API đi kèm bộ cài.
- AutoCAD và VinaCAD sử dụng hai bundle autoload riêng biệt.

## Các lệnh

- `CADManager` — mở trình quản lý add-in.
- `CADManagerRunLast` — chạy lại command được chọn gần nhất.
- `CADManagerDockPanel` — bật/tắt bảng trace/debug.
- `CADListLoadedDlls` — hiển thị các DLL bên thứ ba đang được nạp.
- `InitCADManager` — tạo lại tab ribbon.
- `CADVinaCadTest` — kiểm tra đăng ký command trong VinaCAD.

## Yêu cầu

- Windows x64 và Visual Studio 2022 hoặc mới hơn.
- .NET Framework 4.8 Developer Pack cho AutoCAD 2020–2024.
- .NET SDK 8.0 cho AutoCAD 2025–2026 và VinaCAD 2026.
- Khi build VinaCAD cần có `TD_Mgd_Net8_26.12_17.dll` và `VinaCADCoreMgd_Net8.dll` từ bộ cài VinaCAD 2026.

## Cài đặt từ GitHub Release

Đóng phần mềm CAD trước khi cài đặt hoặc cập nhật.

### AutoCAD

Chạy `CADAddinManager-<version>-setup.exe`. Bộ cài sẽ chép bundle vào:

```text
%APPDATA%\Autodesk\ApplicationPlugins\CADAddinManager.bundle
```

### VinaCAD

Chạy `CADAddinManager-VinaCAD-<version>-setup.exe`. Bộ cài sẽ chép bundle vào:

```text
%APPDATA%\VinaCAD\ApplicationPlugins\CADAddinManager.bundle
```

Khởi động lại phần mềm CAD và chạy `CADManager`. Khi dùng bundle autoload, bạn không cần chạy `NETLOAD`.

Để cài thủ công, giải nén file `*-bundle.zip` tương ứng và chép thư mục `CADAddinManager.bundle` vào thư mục `ApplicationPlugins` nêu trên.

## Các cấu hình build

| Cấu hình | Phần mềm |
|---|---|
| `Debug/Release A20` | AutoCAD 2020 |
| `Debug/Release A21` | AutoCAD 2021 |
| `Debug/Release A22` | AutoCAD 2022 |
| `Debug/Release A23` | AutoCAD 2023 |
| `Debug/Release A24` | AutoCAD 2024 |
| `Debug/Release A25` | AutoCAD 2025 |
| `Debug/Release A26` | AutoCAD 2026 |
| `Debug/Release V26` | VinaCAD 2026.07.x |
| `Debug/Release Z21` | ZWCAD 2021 |
| `Debug/Release Z26` | ZWCAD 2026 |

Ví dụ:

```powershell
dotnet restore CADAddinManager.sln --property:Configuration="Release A26"
dotnet build CADAddinManager.sln --configuration "Release A26" --no-restore
```

Mặc định project tìm VinaCAD tại `C:\Program Files\VinaCAD\VinaCAD 2026`. Có thể thay đổi đường dẫn:

```powershell
dotnet build CADAddinManager.sln --configuration "Release V26" -p:VinaCadInstallDir="D:\VinaCAD 2026"
```

ZWCAD mặc định được tìm tại `C:\Program Files\ZWSOFT\ZWCAD 2021` và
`C:\Program Files\ZWSOFT\ZWCAD 2026`. Build và đóng gói cả hai bản bằng:

```powershell
 .\tools\package-zwcad.ps1 -Version v1.1.1
```

Sau khi giải nén gói tương ứng, chạy `install-zwcad.bat`, khởi động lại ZWCAD,
rồi chạy lệnh `CADManager`. Nếu cần, dùng `NETLOAD` để nạp DLL thủ công.

## Triển khai bundle trên máy phát triển

```powershell
# Bundle debug cho AutoCAD
dotnet build CADAddinManager.sln --configuration "Debug A26" -p:DeployToAutoCAD=true

# Bundle release cho VinaCAD
dotnet build CADAddinManager.sln --configuration "Release V26" -p:DeployToVinaCAD=true
```

## Đóng gói bản phát hành

Tạo toàn bộ ZIP và EXE cho AutoCAD và VinaCAD:

```powershell
.\tools\package-all.ps1 -Version v1.1.0
```

Hoặc chạy riêng từng bước:

```powershell
.\tools\package-release.ps1 -Version v1.1.0
.\tools\package-vinacad.ps1 -Version v1.1.0
.\tools\build-installer.ps1 -Version v1.1.0
.\tools\build-vinacad-installer.ps1 -Version v1.1.0
```

Các file được tạo trong `artifacts/`. Gói VinaCAD phải được build trên máy có cài VinaCAD SDK; GitHub-hosted workflow công khai tiếp tục build các gói AutoCAD.

## Cấu trúc project

```text
src/CADAddinManager/
  Command/                       Các command CAD
  Model/                         Nạp add-in và tiện ích
  View/                          Giao diện và control WPF
  ViewModel/                     Trạng thái UI và command
  PackageContents.xml            Manifest bundle AutoCAD
  PackageContents.VinaCAD.xml    Manifest bundle VinaCAD
```

## Đóng góp và giấy phép

Xem [CONTRIBUTING.md](CONTRIBUTING.md), [CONTRIBUTING.vi.md](CONTRIBUTING.vi.md) và [License.md](License.md).
