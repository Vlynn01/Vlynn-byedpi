# VlynnByeDPI v1.0

VlynnByeDPI is an advanced, standalone network optimization and Deep Packet Inspection (DPI) bypass tool exclusively authored and developed by Vlynn. It helps users easily bypass firewall restrictions, unblock websites, block advertisements at the DNS level, and secure their internet connection without compromising speed.

## 🇺🇸 English Guide

### Key Features
- **Modern Graphical User Interface (GUI)**: Easy one-click activation.
- **System Tray Integration**: Runs silently in the background. Right-click the tray icon to quickly switch modes.
- **Auto-Start**: Automatically launches with Windows.
- **Auto-Connect**: Automatically connects to the recommended Gaming Mode on startup.
- **Smart Anti-Leak Protection**: Automatically routes IPv6 DNS traffic to secure providers to prevent ISP tracking.
- **DNS-over-HTTPS (DoH)**: Native Windows 11 DoH integration for unbreakable Ad-Blocking.

### How to Build
To compile the GUI and self-sign it (to prevent Windows Smart App Control issues), run the provided PowerShell build script:
```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

### How to use
Run `VlynnByeDPI_GUI.exe`. The application features 4 specialized modes:

1. **Mode 1: AdBlock DNS (DoH)**
   - **Pros**: Configures native Windows 11 DNS-over-HTTPS (DoH) via AdGuard. Functions identically to Android's "Private DNS". Blocks up to 80% of ads system-wide without fragmentation.

2. **Mode 2: Unfiltered DNS (Max Compatibility)**
   - **Pros**: Maximum bypass capability, designed for heavily restricted websites.

3. **Mode 3: Gaming & Safe DNS (Recommended)**
   - **Pros**: Optimized for gamers (e.g., Steam, Discord) and uses secure DNS to prevent ISP tracking. Highly recommended for daily use.

4. **Mode 4: Custom DNS (Power User)**
   - **Pros**: Gives you absolute control over your DNS routing. Choose from 4 top-tier global DNS providers (Google, Cloudflare, Quad9, OpenDNS). All traffic will be firmly routed through your selected provider with full IPv6 leak protection.

### How to stop
Click the red **STOP** button in the main interface or right-click the System Tray icon and select **Exit completely**.

---

## 🇻🇳 Hướng dẫn Tiếng Việt

### Tính năng nổi bật
- **Giao diện trực quan (GUI)**: Chỉ cần 1 click là có thể vượt rào mượt mà.
- **Chạy ngầm (System Tray)**: Thu nhỏ xuống góc màn hình. Chuột phải để chuyển Mode siêu nhanh.
- **Khởi động cùng Windows (Auto-Start)**: Tự động chạy ngầm mỗi khi mở máy tính.
- **Tự động kết nối (Auto-Connect)**: Tự động bật mạng Mode 3 an toàn mà không cần bạn phải thao tác.
- **Chống rò rỉ IPv6**: Tự động bọc lót lớp IPv6 an toàn phía sau, chặn đứng mọi ý đồ theo dõi của nhà mạng.
- **Tích hợp DNS-over-HTTPS (DoH)**: Tận dụng cơ chế DoH gốc của Windows 11 để chặn quảng cáo tận gốc.

### Cách tự biên dịch (Build)
Để biên dịch ra file EXE và tự động ký chứng chỉ (chống bị Windows Smart App Control chặn), bạn mở PowerShell và chạy lệnh sau:
```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

### Cách sử dụng
Mở file `VlynnByeDPI_GUI.exe`. App có sẵn 4 chế độ chuyên biệt:

1. **Mode 1: AdBlock DNS (DoH)**
   - **Ưu điểm**: Hoạt động giống hệt tính năng "Private DNS" trên điện thoại Android. Thiết lập DoH (DNS mã hóa) của AdGuard thẳng vào nhân Windows 11 giúp chặn 80% quảng cáo toàn hệ thống.

2. **Mode 2: Unfiltered DNS (Max Compatibility)**
   - **Ưu điểm**: Sức mạnh vượt rào tối đa, chuyên trị các trang web bị chặn gắt gao nhất.

3. **Mode 3: Gaming & Safe DNS (Khuyên Dùng)**
   - **Ưu điểm**: Tối ưu hóa ping tốt nhất cho game thủ (Steam, Discord...) và được bảo vệ bằng DNS an toàn. Đây là chế độ tốt nhất để dùng hàng ngày.

4. **Mode 4: Custom DNS (Dành cho Chuyên gia)**
   - **Ưu điểm**: Cho phép bạn toàn quyền sinh sát lựa chọn mạng DNS của 4 "ông lớn" (Google, Cloudflare, Quad9, OpenDNS). Hệ thống sẽ bẻ lái 100% luồng mạng của bạn qua hàng IP này mà không bị rò rỉ bất cứ thông tin nào.

### Cách dừng phần mềm
Bạn có thể bấm nút **STOP** màu đỏ to đùng ở giữa màn hình, hoặc chuột phải vào biểu tượng cái khiên dưới góc phải màn hình và chọn **Exit completely** để thoát hoàn toàn.

---

*Author: Vlynn*
*Developed exclusively by Vlynn.*
