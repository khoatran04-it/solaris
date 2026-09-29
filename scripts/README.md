# 🌾 SOLARIS THESIS CHAPTER 4 EVALUATION SUITE
**Đề tài:** *Development of an Agricultural E-Commerce System Integrating ERP and an AI Chatbot for Automated Order Placement*  
**Học viên / Sinh viên:** Trần Đăng Khoa (Mã đề tài: IT22.504)  
**Mục đích:** Bộ công cụ chạy thực nghiệm đo đạc định lượng (Quantitative Evaluation) phục vụ Thuyết trình & Bảo vệ Luận văn tốt nghiệp trước Hội đồng.

---

## 🚀 Cách chạy nhanh nhất

### Cách 1: Click đúp chuột (1-Click trên Windows)
- Bấm đúp vào file [`run.bat`](run.bat) trong thư mục này. Menu tương tác tự động xuất hiện.

### Cách 2: Chạy qua Terminal / Command Prompt
```bash
# Đảm bảo Backend đang chạy ở port 7070:
# cd d:\Project\Solaris\backend && dotnet run

# Mở terminal thứ 2 và chạy menu tổng:
cd d:\Project\Solaris\scripts
node run_all.js
```

---

## 📋 Danh sách các kịch bản kiểm thử (Tương ứng Slide 5, 6, 7)

| File thực thi | Câu hỏi nghiên cứu (RQ) | Mục tiêu kiểm thử | Chỉ số định lượng minh chứng |
| :--- | :--- | :--- | :--- |
| [`01_test_rq1_catalog_uom.js`](01_test_rq1_catalog_uom.js) | **RQ1: Mô hình Sản phẩm Nông sản** | Kiểm tra phân cấp 4 tầng danh mục, mô hình thuộc tính động **EAV** (VietGAP, Brix, xuất xứ) và bảng giá đa quy cách **UoM** (Kg, Hộp, Thùng). | • 100% SKU truy vấn nhất quán.<br>• Phân bổ EAV chính xác.<br>• Độ trễ API: ~10–30 ms. |
| [`02_test_rq2_concurrency_fefo.js`](02_test_rq2_concurrency_fefo.js) | **RQ2: Xuất kho FEFO & Tương tranh** | **Phần A:** Thuật toán FEFO tự động vét cạn lô cận hạn 3 ngày trước khi gối đầu lô 15 ngày.<br>**Phần B:** Bùng nổ tải **500 CCU đồng thời** tranh mua 50kg tồn kho. | • Tỷ lệ Overselling: **0.00%**.<br>• Tồn kho về đúng 0, không âm.<br>• Lỗi sập hệ thống (5xx): **0.00%**. |
| [`03_test_rq3_routing_coldchain.js`](03_test_rq3_routing_coldchain.js) | **RQ3: Định tuyến Kho & Chuỗi lạnh** | • Rào chắn xe lạnh TMS 2°C–8°C ($\le 15\text{ km}$).<br>• Miễn trừ rào chắn đối với hàng khô.<br>• Thuật toán **Single-Hub Allocation** chống xé lẻ đơn.<br>• Benchmark giải thuật **Haversine**. | • Tỷ lệ xé lẻ đơn: **0.00%**.<br>• Độ chính xác Geofence: **100%**.<br>• Độ trễ Haversine: **< 0.005 ms/phép tính**. |
| [`04_test_rq4_ai_chatbot.js`](04_test_rq4_ai_chatbot.js) | **RQ4: AI Chatbot & Chống ảo giác** | Đánh giá độ chính xác gọi Tool, Zero-Hallucination về giá/tồn kho và khả năng từ chối an toàn mặt hàng ngoài danh mục (xi măng, iPhone).<br>*Hỗ trợ `--quick` (10 câu ~15s) và `--full` (50 câu).* | • Độ chính xác Tool: **~96.0%**.<br>• Tỷ lệ ảo giác (Hallucination): **0.00%**.<br>• Từ chối hàng ngoài sàn: **100.0%**. |

---

## 💡 Hướng dẫn thuyết minh khi đứng trước Hội đồng

1. **Khi chạy RQ1:** Nhấn mạnh rằng khác với website bán quần áo/đồ gia dụng, nông sản bán theo nhiều đơn vị quy đổi (Kg lẻ, Thùng ưu đãi) và cần kiểm tra nguồn gốc VietGAP/độ ngọt Brix. Hệ thống thiết kế theo mô hình EAV và UoM linh hoạt.
2. **Khi chạy RQ2:** Cho Hội đồng thấy 500 requests lao vào Database cùng 1 tích tắc. Trừ kho nguyên tử bằng Atomic Update bảo đảm không bị bán âm dù chỉ 1 gram nông sản.
3. **Khi chạy RQ3:** Cho Hội đồng thấy khi giao sang Củ Chi (>15km) thì đồ tươi tự động bị chặn bảo vệ chuỗi lạnh 2°C–8°C, nhưng hàng khô (Gạo ST25) vẫn được chuyển sang GHN để giao toàn quốc. Và giỏ hàng 2 món nếu kho gần nhất thiếu 1 món thì hệ thống tự động chọn kho xa hơn có đủ 100% để giao 1 kiện duy nhất, không xé đơn của khách.
4. **Khi chạy RQ4:** Chọn tùy chọn **[4] Quick Demo (10 câu)**. Trong 15 giây, Hội đồng sẽ thấy AI nhận diện chính xác câu hỏi giá, tồn kho, từ chối an toàn câu hỏi bẫy ("bán xi măng", "bán iPhone") và tự động bung Thẻ Đơn Hàng Tương Tác khi người dùng gõ câu lệnh mua sắm tự nhiên.
