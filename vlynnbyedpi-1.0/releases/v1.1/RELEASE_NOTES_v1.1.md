# VlynnByeDPI v1.1 — Windows PC & Android 16 Release

**Tag**: `v1.1`  
**Release Title**: `VlynnByeDPI v1.1 (Windows PC & Android 16 Edition)`

---

## 📦 Download Packages / Tải xuống

| Platform | File | Description |
| :--- | :--- | :--- |
| **Windows 10/11 (Single EXE)** | `VlynnByeDPI_v1.1_Windows_Portable.exe` | **All-in-One Portable EXE** — Self-extracts `x86_64` engine files automatically on any PC. |
| **Windows 10/11 (Full ZIP)** | `VlynnByeDPI_v1.1_Windows_x64.zip` | Full folder archive (`VlynnByeDPI_GUI.exe` + `x86_64\` + `README.md`). |
| **Android 8.0 – Android 16** | `VlynnByeDPI_v1.1_Android16.apk` | **Mobile Edition (API 36)** — Zero-lag Split-Tunnel VPN, DoH & Non-standard Port DNS Engine (No Root required). |

---

## ✨ What's New in v1.1 (English)
- **NEW: Android 16 (API 36) Mobile App (`.apk`)**:
  - Built-in non-root `VpnService` using split-tunnel DNS routing (keeps 100% native Wi-Fi/5G speed and low gaming ping).
  - Supports **Mode 1 (AdGuard DoH)**, **Mode 2 (Unfiltered DNS Port 1253)**, **Mode 3 (Gaming & Safe DNS Port 1253)**, and **Mode 4 (Custom DNS: Cloudflare, Google, Quad9, OpenDNS Port 5353, AdGuard)**.
  - Includes Android 16 Quick Settings Tile, Auto-Start on Boot, and a real-time Protected DNS Query counter.
- **Self-Contained All-in-One Windows Executable**:
  - Embedded `vlynnengine.exe`, `WinDivert.dll`, `WinDivert64.sys`, and `icon.ico` inside `VlynnByeDPI_GUI.exe`.
  - Automatically extracts missing files and removes Windows Mark-of-the-Web (`Zone.Identifier`) so all 4 Modes work reliably across all PCs.
- **Process Health-Check & Automatic IPv4 Fallback**:
  - Verifies engine startup health and automatically falls back to IPv4 parameters if IPv6 is disabled on the host network adapter.
- **Fixed Mode 4 Custom DNS Port Routing**:
  - Uses standard Port `53` for Cloudflare (`1.1.1.1`), Google (`8.8.8.8`), Quad9 (`9.9.9.9`), and AdGuard (`94.140.14.14`), and Port `5353` for OpenDNS (`208.67.222.222`).
- **Smart `STOP PROTECTION` Button & Modern Dark Dashboard UI**:
  - `STOP` button is strictly hidden when `Status: OFF` or if activation fails, and only appears when protection is verified active.
  - Removed the 1.5s artificial transition delay for instant mode switching.

---

## 🇻🇳 Điểm mới trong bản v1.1 (Tiếng Việt)
- **Ra mắt bản Android 16 (`.apk`)**:
  - Hoạt động không cần Root qua cơ chế Split-Tunnel VPN (chỉ định tuyến luồng DNS qua DoH / cổng `1253` / `5353` để vượt chặn và chặn quảng cáo, giữ nguyên 100% tốc độ mạng gốc và ping khi chơi game).
  - Hỗ trợ đầy đủ 4 Mode giống bản PC, có nút bật/tắt nhanh trên thanh trạng thái (Quick Settings Tile) và bộ đếm truy vấn DNS theo thời gian thực.
- **Đóng gói Windows All-in-One (`.exe` độc lập)**:
  - Nhúng toàn bộ thư mục `x86_64` vào trong file `.exe`. Mang sang máy tính khác chỉ cần chạy 1 file `.exe` duy nhất là tự bung đầy đủ thành phần và tự gỡ chặn `Zone.Identifier`.
- **Tự động kiểm tra tiến trình & Fallback IPv4**:
  - Khắc phục triệt để lỗi Mode 2, 3, 4 không lên trạng thái trên các PC tắt IPv6 hoặc thiếu file.
- **Sửa lỗi cổng DNS ở Mode 4**:
  - Định tuyến chuẩn Port `53` cho Cloudflare, Google, Quad9, AdGuard và Port `5353` cho OpenDNS.
- **Tối ưu nút `STOP PROTECTION` & Giao diện Dark Dashboard**:
  - Ẩn hoàn toàn nút `STOP` khi đang tắt (`OFF`) hoặc khi bật không thành công; chỉ hiện lên khi hệ thống đang chạy thực sự.
  - Loại bỏ độ trễ 1.5s, chuyển đổi chế độ mượt mà tức thì.

---

## 🔒 SHA-256 Checksums
```text
C9DC7305550943C6B87E503BEA2171B5C9F56E1DA727C6D0EA634CE4F7172108  VlynnByeDPI_v1.1_Windows_Portable.exe
377367F0AC0393DDC542B6BCF95DA844C74F5A4503324BA85392580372C5EA02  VlynnByeDPI_v1.1_Windows_x64.zip
3C066F1649BB4442EBED46B0355084D8CBB57347B40C11106EF3A2C776004763  VlynnByeDPI_v1.1_Android16.apk
```

