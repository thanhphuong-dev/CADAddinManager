# Đóng góp

Cảm ơn bạn đã muốn cải thiện CAD Add-in Manager.

## Phát triển

1. Cài Visual Studio 2022 hoặc mới hơn.
2. Cài .NET SDK 8.0.
3. Cài .NET Framework 4.8 Developer Pack khi build target AutoCAD 2020-2024.
4. Restore package cho configuration bạn muốn build, ví dụ `dotnet restore CADAddinManager.sln --property:Configuration="Release A26"`.
5. Build configuration AutoCAD cần dùng, ví dụ:

```powershell
dotnet build CADAddinManager.sln --configuration "Release A26"
```

## Pull Request

- Giữ thay đổi tập trung, tránh gom nhiều việc không liên quan vào một PR.
- Ghi rõ bạn đã build hoặc test như thế nào.
- Cập nhật README hoặc tài liệu nếu hành vi public thay đổi.
- Thêm ảnh chụp màn hình hoặc mô tả ngắn nếu có thay đổi giao diện.

## Issue

Khi báo lỗi, hãy ghi rõ phiên bản Windows, phiên bản AutoCAD, configuration build, log hoặc ảnh chụp màn hình nếu có.
