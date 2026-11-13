# VlynnByeDPI v1.1 (Windows PC & Android 16)

VlynnByeDPI is an advanced, standalone network optimization, Deep Packet Inspection (DPI) bypass, and encrypted DNS routing suite exclusively authored and developed by **Vlynn**. Available for both **Windows PC (x64)** and **Android (up to Android 16 / API 36)**, it helps users bypass firewall restrictions, unblock websites, block advertisements at the DNS level, and secure their internet connection without compromising speed or gaming latency.

---

## 🚀 What's New in v1.1
- **Android 16 (API 36) Edition (`.apk`)**: Official mobile version featuring a zero-lag split-tunnel `VpnService`, DNS-over-HTTPS (DoH), non-standard port DNS tunneling (`1253` / `5353`), real-time query counter, and Quick Settings Tile.
- **Self-Contained All-in-One Windows `.exe`**: `x86_64` engine binaries (`vlynnengine.exe`, `WinDivert.dll`, `WinDivert64.sys`, `icon.ico`) are now embedded directly inside `VlynnByeDPI_GUI.exe` and auto-extracted + unblocked (`Zone.Identifier` stripped) on any PC.
- **Smart Health-Check & IPv4 Fallback**: Verifies engine process health in real time and automatically falls back to IPv4 mode if IPv6 is disabled on the target PC's network adapter.
- **Smart `STOP` Button**: Automatically hidden when `Status: OFF` or if activation fails; only appears when protection is verified active and running.
- **Fixed Mode 4 Custom DNS Ports**: Properly maps standard Port `53` for Cloudflare (`1.1.1.1`), Google (`8.8.8.8`), Quad9 (`9.9.9.9`), and AdGuard (`94.140.14.14`), while keeping Port `5353` for OpenDNS (`208.67.222.222`).
- **Redesigned Modern Dark Dashboard UI**: Sleek status card, active mode highlight with `✓` indicator, and instant response (removed artificial 1.5s delay).

---

## 🇺🇸 English Guide

### 1. Windows PC Edition (`VlynnByeDPI_v1.1_Windows_Portable.exe` / `.zip`)
- **Modern Dark Dashboard GUI**: One-click activation with live status feedback.
- **Self-Contained Portable EXE**: Copy just the `.exe` to any Windows 10/11 PC and run—engine files are automatically extracted and unblocked.
- **System Tray & Auto-Start**: Minimizes to the system tray and optionally launches & connects automatically on Windows startup.

#### How to Build (Windows)
```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

### 2. Android 16 Edition (`VlynnByeDPI_v1.1_Android16.apk`)
- **No Root Required**: Uses Android's native `VpnService` in split-tunnel DNS mode—100% of game/video/download traffic goes directly through Wi-Fi/5G with zero speed loss.
- **Android 16 (API 36) Ready**: Supports edge-to-edge UI, foreground service special-use compliance, and Quick Settings Tile.

#### How to Build (Android)
```powershell
powershell -ExecutionPolicy Bypass -File build_android.ps1
```

### 4 Specialized Protection Modes (Windows & Android)
1. **Mode 1: AdBlock DNS & Bypass (DoH)**
   - **Pros**: Routes DNS through AdGuard DNS-over-HTTPS (DoH) + DPI Bypass. Blocks up to 80% of ads and trackers system-wide.
2. **Mode 2: Unfiltered DNS (Max Compatibility)**
   - **Pros**: Routes DNS via non-standard Port `1253` (`77.88.8.8`) + DPI fragmentation to bypass strict ISP port-53 hijacking.
3. **Mode 3: Gaming & Safe DNS (Recommended)**
   - **Pros**: Optimized low-latency routing (`77.88.8.1:1253`) for gamers (Steam, Discord, mobile games) with full IPv6 anti-leak protection.
4. **Mode 4: Custom DNS (Power User)**
   - **Pros**: Choose between **Cloudflare (`1.1.1.1`)**, **Google (`8.8.8.8`)**, **Quad9 (`9.9.9.9`)**, **OpenDNS (`208.67.222.222:5353`)**, and **AdGuard (`94.140.14.14`)**.

---

## 🇻🇳 Hướng dẫn Tiếng Việt

### Điểm mới trong bản v1.1
- **Ra mắt bản Android 16 (`.apk`)**: Hoạt động không cần Root qua cơ chế Split-Tunnel VPN siêu nhẹ (chỉ bẻ lái luồng DNS chống chặn qua DoH / cổng `1253` / `5353`, giữ nguyên 100% tốc độ mạng gốc và ping khi chơi game), hỗ trợ bật/tắt nhanh trên thanh Quick Settings.
- **Bản Windows PC All-in-One (`.exe` độc lập)**: Nhúng toàn bộ thư mục `x86_64` vào thẳng file `.exe`. Mang sang bất kỳ máy tính nào khác chỉ cần 1 file `.exe` duy nhất là tự bung file, tự gỡ chặn `Zone.Identifier` và chạy mượt cả 4 Mode.
- **Nút `STOP PROTECTION` thông minh**: Tự động ẩn hoàn toàn khi đang `OFF` hoặc khi bật chưa thành công; chỉ hiện lên khi hệ thống đã chạy thực sự.
- **Sửa lỗi cổng DNS Mode 4 & Tự động Fallback IPv4**: Sửa chuẩn port `53` cho Cloudflare/Google/Quad9/AdGuard và port `5353` cho OpenDNS; tự động chuyển về IPv4 nếu card mạng trên PC khác tắt IPv6.
- **Giao diện Dark Dashboard mới**: Phản hồi tức thì (bỏ độ trễ 1.5s cũ), đóng khung trạng thái sắc nét và đánh dấu `✓` trực quan cho Mode đang chạy.

### Cách sử dụng
- **Trên Windows PC**: Tải `VlynnByeDPI_v1.1_Windows_Portable.exe` (hoặc giải nén bản `.zip`) -> Mở app -> Chọn 1 trong 4 Mode (Khuyên dùng **Mode 3** hàng ngày hoặc **Mode 1** để chặn quảng cáo).
- **Trên Điện thoại Android (Hỗ trợ đến Android 16)**: Tải và cài đặt `VlynnByeDPI_v1.1_Android16.apk` -> Mở app -> Chọn Mode và bấm **OK** ở hộp thoại cấp quyền VPN lần đầu.

### Cách tự biên dịch & Đóng gói Release
```powershell
# Build bản Windows EXE
powershell -ExecutionPolicy Bypass -File build.ps1

# Build bản Android 16 APK
powershell -ExecutionPolicy Bypass -File build_android.ps1
```

---

*Author: Vlynn*  
*Developed exclusively by Vlynn.*
