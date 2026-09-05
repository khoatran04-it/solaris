# TỔNG HỢP ĐẶC TẢ KỸ THUẬT TOÀN DIỆN 15 MODULE HỆ THỐNG SOLARIS

> **Dự án:** Sàn Thương Mại Điện Tử & Chuỗi Cung Ứng Nông Sản Sạch **Solaris**  
> **Kiến trúc:** Clean Architecture (.NET 8 Web API + Entity Framework Core 8 + SQL Server + Next.js 16 + React 18 + Tailwind CSS + Zustand)  
> **Trạng thái kiểm thử:** 799/799 Tests Passed (Backend: 390/390, Frontend Shop: 75/75, Frontend Admin: 334/334). Mã nguồn 100% Clean Comment.

---

## MỤC LỤC 15 MODULE

1. [Module 01: Identity & Access Management (IAM) & Phân quyền RBAC](#module-01-identity--access-management-iam--phan-quyen-rbac)
2. [Module 02: Unit of Measure (UoM) Master Data & Động cơ Quy đổi Đa chiều](#module-02-unit-of-measure-uom-master-data--dong-co-quy-doi-da-chieu)
3. [Module 03: Supplier Master Data & Quản lý Nhà Cung Cấp](#module-03-supplier-master-data--quan-ly-nha-cung-cap)
4. [Module 04: Product Master Data & Hệ thống Thuộc tính Nông sản Động (EAV)](#module-04-product-master-data--he-thong-thuoc-tinh-nong-san-dong-eav)
5. [Module 05: Product Catalog, Biến thể SKU & Bảng giá Đa quy cách](#module-05-product-catalog-bien-the-sku--bang-gia-da-quy-cach)
6. [Module 06: Customer Master Data, Phân hạng Thành viên & Sổ địa chỉ](#module-06-customer-master-data-phan-hang-thanh-vien--so-dia-chi)
7. [Module 07: Promotion Campaign & Động cơ Chiết khấu Khuyến mãi](#module-07-promotion-campaign--dong-co-chiet-khau-khuyen-mai)
8. [Module 08: Warehouse & Tối ưu Không gian Địa lý (Geospatial Fulfillment)](#module-08-warehouse--toi-uu-khong-gian-dia-ly-geospatial-fulfillment)
9. [Module 09: Purchasing & Quy trình Mua hàng Nhà cung cấp (PO)](#module-09-purchasing--quy-trinh-mua-hang-nha-cung-cap-po)
10. [Module 10: Core Inventory Engine & Két sắt Tồn kho 4 ngăn (FEFO)](#module-10-core-inventory-engine--ket-sat-ton-kho-4-ngan-fefo)
11. [Module 11: Inventory Audit & Đối soát Kiểm kê, Xuất hủy](#module-11-inventory-audit--doi-soat-kiem-ke-xuat-huy)
12. [Module 12: Shopping Cart (B2C) & Cơ chế Tự động Hợp nhất (Auto-Merge)](#module-12-shopping-cart-b2c--co-che-tu-dong-hop-nhat-auto-merge)
13. [Module 13: Order Processing & Tích hợp Vận chuyển GHN API v2](#module-13-order-processing--tich-hop-van-chuyen-ghn-api-v2)
14. [Module 14: Payment Gateway (VNPay Sandbox) & Đổi trả Nông sản (RMA)](#module-14-payment-gateway-vnpay-sandbox--doi-tra-nong-san-rma)
15. [Module 15: AI Chatbot & Thương mại Đàm thoại (Conversational Commerce)](#module-15-ai-chatbot--thuong-mai-dam-thoai-conversational-commerce)

---

## Module 01: Identity & Access Management (IAM) & Phân quyền RBAC

### 1. Mục tiêu Nghiệp vụ
Quản lý định danh toàn bộ cán bộ nhân viên vận hành, phân quyền chi tiết theo ma trận Vai trò - Quyền hạn (Role-Based Access Control - RBAC) và bảo mật phiên làm việc qua cơ chế JWT (JSON Web Token) kết hợp Refresh Token.

### 2. Mô hình Thực thể (Entities)
- `User`: Tài khoản người dùng nội bộ (Mã NV, Họ tên, Email, SĐT, Mật khẩu băm PBKDF2/BCrypt, Trạng thái hoạt động, Cờ xóa mềm `ISoftDelete`).
- `Role`: Vai trò hệ thống (Admin, Quản lý kho, Nhân viên mua hàng, Kế toán, Chăm sóc khách hàng).
- `Permission`: Danh mục quyền hạn nguyên tử (`user.view`, `inventory.receipt.create`, `order.approve`, v.v.).
- `UserRole`: Bảng liên kết nhiều-nhiều giữa User và Role.
- `RolePermission`: Bảng liên kết nhiều-nhiều giữa Role và Permission.
- `UserToken`: Quản lý Refresh Token, thu hồi token khi đăng xuất hoặc đổi mật khẩu.

### 3. Thành phần Kỹ thuật Backend
- **Services:** `AuthService`, `IAUserService`, `IARoleService`, `IAPermissionService`.
- **Controllers:** `AuthController`, `UsersController`, `RolesController`, `PermissionsController`.
- **Bảo mật:** `JwtMiddleware`, `RequirePermissionAttribute` tùy biến kiểm tra claim quyền hạn trước khi cho phép request đi vào Action.
- **Tự động kích hoạt:** Seed data các Role và Permission cốt lõi khi khởi động ứng dụng.

### 4. Giao diện Frontend Admin
- Trang Đăng nhập bảo mật lưu Token trong LocalStorage với Interceptor tự động gắn Header `Authorization: Bearer <token>`.
- Màn hình Quản lý Người dùng, Quản lý Vai trò với ma trận checkbox gán quyền nhóm theo từng Module.

---

## Module 02: Unit of Measure (UoM) Master Data & Động cơ Quy đổi Đa chiều

### 1. Mục tiêu Nghiệp vụ
Nông sản đặc thù có nhiều quy cách đo lường: mua theo tấn/tạ nhưng lưu kho theo kilogram, bán lẻ theo túi 500g, trái, hoặc thùng 10kg. Module này thiết lập bảng danh mục đơn vị tính và ma trận tỷ lệ quy đổi chuẩn xác đến 3 chữ số thập phân (`decimal(18,3)`).

### 2. Mô hình Thực thể (Entities)
- `UoMCategory`: Nhóm đơn vị tính (Khối lượng, Thể tích, Số đếm, Chiều dài).
- `UnitOfMeasure`: Đơn vị tính (Tên, Mã, Nhóm, Cờ `IsBaseUnit` - đơn vị cơ sở chuẩn của nhóm).
- `UoMConversion`: Bảng quy đổi giữa 2 ĐVT (`FromUoMId`, `ToUoMId`, `ConversionFactor`).

### 3. Thành phần Kỹ thuật Backend
- **Services:** `UoMService`, `UoMCategoryService`, `UoMConversionService`.
- **Động cơ quy đổi:** Công thức chuẩn hóa:
  $$Q_{\text{cơ\_sở}} = Q_{\text{nguồn}} \times \text{Factor}$$
- **Ràng buộc nghiệp vụ:** Mỗi `UoMCategory` chỉ được phép có duy nhất một `IsBaseUnit = true`. Chặn việc khai báo vòng lặp quy đổi vô tận và hệ số $\le 0$.

### 4. Giao diện Frontend Admin
- Quản lý danh mục ĐVT, khai báo tỷ lệ quy đổi trực quan, hiển thị preview công thức quy đổi tương đương theo thời gian thực.

---

## Module 03: Supplier Master Data & Quản lý Nhà Cung Cấp

### 1. Mục tiêu Nghiệp vụ
Quản lý mạng lưới nông hộ, hợp tác xã, trang trại cung ứng nông sản; theo dõi hồ sơ năng lực, mã số thuế, phân loại đối tác, danh bạ kho nhận hàng và bảng giá cung ứng sản phẩm.

### 2. Mô hình Thực thể (Entities)
- `Supplier`: Thông tin pháp lý (Mã NCC, Tên, Mã số thuế, Email, Hotline, Đánh giá uy tín).
- `SupplierType`: Phân loại đối tác (Trang trại VietGAP, Hợp tác xã, Nhà nhập khẩu, Doanh nghiệp chế biến).
- `SupplierAddress`: Sổ địa chỉ kho bãi xuất phát của NCC có tọa độ GPS (`Latitude`, `Longitude`) để tối ưu định tuyến.
- `SupplierProduct`: Bảng giá cung ứng (`VariantId`, `SupplierId`, `LastImportPrice`, `MinimumOrderQuantity` - MOQ, `LeadTimeDays`).

### 3. Thành phần Kỹ thuật Backend
- **Services:** `SupplierService`, `SupplierTypeService`, `SupplierAddressService`, `SupplierProductService`.
- **Ràng buộc toàn vẹn:** Composite Unique Index trên `(VariantId, SupplierId)` đảm bảo một NCC chỉ có một chính sách giá/MOQ duy nhất cho mỗi biến thể tại một thời điểm.

### 4. Giao diện Frontend Admin
- Quản lý Master-Detail Nhà cung cấp, Tab Sổ địa chỉ (tích hợp chọn Tỉnh/Huyện/Xã GHN), Tab Bảng giá sản phẩm cung ứng với tính năng tìm kiếm và cập nhật nhanh.

---

## Module 04: Product Master Data & Hệ thống Thuộc tính Nông sản Động (EAV)

### 1. Mục tiêu Nghiệp vụ
Xây dựng cây phân loại nông sản 2 cấp (Nhóm ngành hàng $\rightarrow$ Danh mục con) kết hợp hệ thống thuộc tính động (Entity-Attribute-Value - EAV) để mô tả chi tiết các chỉ số chất lượng nông sản: Độ đường Brix, Tiêu chuẩn chất lượng (VietGAP/GlobalGAP/Organic), Vùng trồng (Đà Lạt, Lâm Đồng, Tiền Giang), v.v.

### 2. Mô hình Thực thể (Entities)
- `ProductCategoryGroup`: Nhóm ngành hàng lớn (Trái cây tươi, Rau củ hữu cơ, Nông sản sấy, Thảo mộc gia vị).
- `ProductCategory`: Danh mục cụ thể (Cam sành, Bơ sáp, Sầu riêng, Cà chua bi...).
- `AttributeDefinition`: Định nghĩa thuộc tính (Tên, Mã, Kiểu dữ liệu: `Text`, `Number`, `Select`, `Boolean`).
- `CategoryAttribute`: Khung thuộc tính gắn vào danh mục (Cờ `IsRequired`, thứ tự sắp xếp).

### 3. Thành phần Kỹ thuật Backend
- **Services:** `ProductCategoryGroupService`, `ProductCategoryService`, `AttributeDefinitionService`, `CategoryAttributeService`.
- **Kiến trúc EAV:** Cho phép thêm mới các tiêu chuẩn kiểm định nông sản mà không cần chạy lại Migration chỉnh sửa cấu trúc bảng vật lý.

### 4. Giao diện Frontend Admin
- Cấu hình cây danh mục kéo thả, màn hình thiết lập khung thuộc tính mẫu cho từng danh mục sản phẩm.

---

## Module 05: Product Catalog, Biến thể SKU & Bảng giá Đa quy cách

### 1. Mục tiêu Nghiệp vụ
Quản lý sản phẩm cha (`Product`) và các biến thể con (`ProductVariant` - SKU). Mỗi biến thể sở hữu bảng giá bán đa quy cách (`VariantPrice`) theo các ĐVT khác nhau (mua 1 Kg, mua 1 Thùng 5 Kg hoặc mua Sỉ), đồng thời theo dõi số lô sản xuất (`ProductBatch`).

### 2. Mô hình Thực thể (Entities)
- `Product`: Thông tin sản phẩm cha (Mã, Tên, Slug URL, Danh mục, ĐVT cơ sở `BaseUoM`, Ảnh đại diện, Mô tả chi tiết).
- `ProductVariant`: Biến thể SKU cụ thể (Mã barcode, Tên quy cách, Trọng lượng `grossWeightKg`, Thể tích `unitCbm`, Hướng dẫn bảo quản `inventoryGuideline`).
- `VariantAttributeValue`: Giá trị cụ thể của các thuộc tính EAV ứng với biến thể.
- `VariantPrice`: Bảng giá theo từng đơn vị tính (`UoMId`, `Price`, `IsDefault`).
- `ProductBatch`: Quản lý Lô hàng nông sản (`BatchCode`, `ManufactureDate`, `ExpiryDate`, `SupplierId`).

### 3. Thành phần Kỹ thuật Backend
- **Services:** `ProductService`, `ProductVariantService`, `ProductBatchService`, `ShopProductService`.
- **Tính toán On-the-fly:** `ShopProductService` tự động đối chiếu chiến dịch khuyến mãi đang hoạt động để tính giá chiết khấu, phần trăm giảm giá và kiểm tra tồn kho khả dụng tổng hợp trước khi trả về Frontend Shop.

### 4. Giao diện Frontend Admin & Shop
- **Admin:** Form tạo sản phẩm đa bước (Wizard) gồm: Thông tin cơ bản $\rightarrow$ Thuộc tính động $\rightarrow$ Biến thể SKU $\rightarrow$ Thiết lập bảng giá đa ĐVT.
- **Shop:** Trang chi tiết sản phẩm chuẩn SEO (JsonLd Schema), cho phép khách hàng chọn linh hoạt ĐVT và hiển thị giá tương ứng.

---

## Module 06: Customer Master Data, Phân hạng Thành viên & Sổ địa chỉ

### 1. Mục tiêu Nghiệp vụ
Quản lý cơ sở dữ liệu khách hàng B2C và đối tác bán buôn B2B; chính sách tích lũy điểm thăng hạng thành viên (Customer Tiers) được hưởng chiết khấu tự động; sổ địa chỉ giao hàng đa điểm.

### 2. Mô hình Thực thể (Entities)
- `Customer`: Hồ sơ khách hàng (Mã, Họ tên, SĐT đăng nhập, Email, Mật khẩu băm, Hạng thành viên, Trạng thái).
- `CustomerType`: Loại hình khách lẻ B2C hoặc đại lý B2B.
- `CustomerTier`: Hạng thành viên (Đồng, Bạc, Vàng, Kim Cương) kèm `DiscountPercent` ưu đãi trọn đời.
- `CustomerGroup`: Nhóm phân khúc khách hàng phục vụ Marketing.
- `CustomerAddress`: Sổ địa chỉ giao nhận (Tên người nhận, SĐT, Tỉnh/Thành, Quận/Huyện, Phường/Xã, Địa chỉ chi tiết, Tọa độ GPS `Latitude`/`Longitude`, Cờ mặc định `IsDefault`).

### 3. Thành phần Kỹ thuật Backend
- **Services:** `CustomerService`, `CustomerTypeService`, `CustomerTierService`, `CustomerGroupService`, `CustomerAddressService`, `ShopCustomerService`.
- **Liên kết Cascade:** Xóa khách hàng hoặc cập nhật địa chỉ mặc định được quản lý giao dịch an toàn; tự động bỏ cờ mặc định cũ khi kích hoạt địa chỉ mặc định mới.

### 4. Giao diện Frontend Admin & Shop
- **Admin:** Xem danh sách khách hàng, lịch sử mua hàng, điều chỉnh hạng thẻ.
- **Shop:** Trang Quản lý tài khoản cá nhân, cập nhật hồ sơ, quản lý Sổ địa chỉ giao hàng với dropdown kết nối GHN API.

---

## Module 07: Promotion Campaign & Động cơ Chiết khấu Khuyến mãi

### 1. Mục tiêu Nghiệp vụ
Tạo lập các chương trình khuyến mãi theo mùa vụ nông sản, Flash Sale, giảm giá theo tỷ lệ % hoặc số tiền cố định áp dụng cho danh sách các biến thể nông sản được chỉ định trong khung thời gian nhất định.

### 2. Mô hình Thực thể (Entities)
- `PromotionCampaign`: Thông tin chiến dịch (Tên, Mã, Banner, Thời gian bắt đầu `StartDate`, Thời gian kết thúc `EndDate`, Cờ phần trăm `IsPercentage`, Giá trị giảm `DiscountValue`, Trạng thái `IsActive`).
- `PromotionVariant`: Danh sách biến thể SKU tham gia chương trình.

### 3. Thành phần Kỹ thuật Backend
- **Services:** `PromotionCampaignService`.
- **Ràng buộc:** Composite Unique Index trên `(PromotionCampaignId, VariantId)` bảo vệ dữ liệu không bị trùng lặp.
- **Thuật toán tính giá:** Tự động lọc các chiến dịch đang trong khoảng thời gian có hiệu lực (`DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate && IsActive`), áp dụng mức giảm tối đa cho người tiêu dùng.

### 4. Giao diện Frontend Admin & Shop
- **Admin:** Quản lý chiến dịch, chọn biến thể áp dụng qua modal tìm kiếm nhanh.
- **Shop:** Banner Slider nổi bật trên trang chủ, huy hiệu phần trăm giảm giá (-X%) đỏ rực trên thẻ sản phẩm và trang Landing Page chuyên biệt `/khuyen-mai/[slug]`.

---

## Module 08: Warehouse & Tối ưu Không gian Địa lý (Geospatial Fulfillment)

### 1. Mục tiêu Nghiệp vụ
Quản lý mạng lưới kho lạnh, trung tâm phân phối (Hub) bảo quản nông sản tươi. Tính toán khoảng cách địa lý giữa địa chỉ khách hàng và các kho để tự động phân bổ đơn hàng cho kho gần nhất nhằm bảo đảm độ tươi ngon và tiết kiệm chi phí giao vận.

### 2. Mô hình Thực thể (Entities)
- `Warehouse`: Thông tin kho (Mã kho, Tên kho, SĐT thủ kho, Tọa độ GPS `Latitude`, `Longitude`, Sức chứa m³, Trạng thái).
- `WarehouseInventory`: Bảng số dư tồn kho vật lý tại từng kho.

### 3. Thành phần Kỹ thuật Backend
- **Services:** `WarehouseService`, `DistanceService`.
- **Thuật toán Haversine Great-Circle:** Tính khoảng cách mặt cầu Trái Đất giữa hai điểm tọa độ:
  $$a = \sin^2\left(\frac{\Delta\text{lat}}{2}\right) + \cos(\text{lat}_1)\cos(\text{lat}_2)\sin^2\left(\frac{\Delta\text{lon}}{2}\right)$$
  $$c = 2 \cdot \text{atan2}\left(\sqrt{a}, \sqrt{1-a}\right)$$
  $$d = R \cdot c \quad (R \approx 6.371\text{ km})$$
- Tự động quét tìm kho gần nhất có đủ hàng tồn khả dụng để đáp ứng đơn hàng.

### 4. Giao diện Frontend Admin
- Quản lý danh sách kho bãi, định vị tọa độ trên bản đồ số, theo dõi sức chứa và công suất sử dụng.

---

## Module 09: Purchasing & Quy trình Mua hàng Nhà cung cấp (PO)

### 1. Mục tiêu Nghiệp vụ
Số hóa toàn diện quy trình cung ứng từ khâu lập Đơn đặt hàng mua (Purchase Order - PO), trình phê duyệt, đến việc theo dõi tiến độ giao hàng từ các nhà vườn, trang trại đối tác.

### 2. Mô hình Thực thể (Entities)
- `PurchaseOrder`: Thông tin phiếu mua (Số chứng từ `POCode`, Nhà cung cấp, Ngày hẹn giao hàng, Trạng thái: `Draft`, `Submitted`, `Approved`, `PartiallyReceived`, `Completed`, `Cancelled`, Tổng tiền, Ghi chú).
- `PurchaseOrderDetail`: Chi tiết từng dòng hàng (Biến thể SKU, ĐVT mua, Số lượng đặt, Đơn giá nhập, Thành tiền, Số lượng đã nhập kho thực tế).

### 3. Thành phần Kỹ thuật Backend
- **Services:** `PurchaseOrderService`.
- **Smart Auto-fill:** Khi nhân viên chọn biến thể và NCC, hệ thống tự động tra cứu bảng giá `SupplierProduct` để điền trước đơn giá nhập gần nhất và kiểm tra MOQ.
- **Workflow State Machine:** Kiểm soát chặt chẽ chuyển đổi trạng thái chứng từ; đơn hàng đã `Approved` mới được phép lập phiếu nhập kho kiểm định (GRN).

### 4. Giao diện Frontend Admin
- Màn hình lập phiếu PO với bảng chi tiết động, tính toán tổng tiền tức thời, luồng nút bấm Duyệt / Từ chối / Xuất file PDF.

---

## Module 10: Core Inventory Engine & Két sắt Tồn kho 4 ngăn (FEFO)

### 1. Mục tiêu Nghiệp vụ
Trọng tâm vận hành của chuỗi cung ứng nông sản: Động cơ quản lý tồn kho theo kiến trúc **Két sắt 4 ngăn** độc lập, quy trình nhập kho kiểm định chất lượng (GRN), điều chuyển nội bộ 2 bước, và cơ chế xuất kho ưu tiên hạn dùng **FEFO (First-Expired, First-Out)** để giảm thiểu tỷ lệ hao hụt hư hỏng.

### 2. Mô hình Két sắt Tồn kho 4 ngăn (4-Compartment Inventory Vault)
Mỗi bản ghi số dư `WarehouseInventory` gắn liền với bộ ba duy nhất `(WarehouseId, VariantId, BatchId)` được phân bổ thành 4 ngăn số lượng:
1. `QuantityAvailable`: Hàng khả dụng (Sẵn sàng bán trên Web/App hoặc xuất kho).
2. `QuantityReserved`: Hàng đã được khách đặt mua online (Tạm giữ để chờ đóng gói xuất hàng, ngăn chặn bán vượt tồn kho - Overselling).
3. `QuantityQC`: Hàng đang trong khu vực kiểm định chất lượng / chờ mẫu test dư lượng thuốc BVTV.
4. `QuantityDamaged`: Hàng hỏng hóc, dập nát, quá hạn (Chờ xử lý xuất hủy, tuyệt đối cách ly khỏi hàng bán).

### 3. Quy trình Vận hành Cốt lõi
- **Nhập kho kiểm định (Goods Receipt Note - GRN):** Phân định rạch ròi số lượng Đạt chuẩn (`AcceptedQuantity` $\rightarrow$ cộng vào `Available`) và Phế phẩm/Dập hỏng (`RejectedQuantity` $\rightarrow$ cộng vào `Damaged`).
- **Xuất kho FEFO (Goods Delivery Note - GDN):** Khi xử lý xuất đơn hàng, hệ thống tự động truy vấn các Lô hàng (`Batch`) có hạn sử dụng gần nhất (`OrderBy(b => b.ExpiryDate)`) để xuất trước.
- **Điều chuyển kho 2 bước (Inventory Transfer):**
  - *Bước 1 (Xuất chuyển):* Trừ `Available` ở Kho xuất, đưa vào trạng thái Đang vận chuyển (`InTransit`).
  - *Bước 2 (Nhập đích):* Kho nhận kiểm đếm thực tế, ghi nhận vào `Available` ở Kho đích.

### 4. Thành phần Kỹ thuật Backend
- **Services:** `InventoryService`, `InventoryReceiptService`, `InventoryIssueService`, `InventoryTransferService`.
- **Ràng buộc Database:** Composite Unique Index trên `(WarehouseId, VariantId, BatchId)` kết hợp kiểm tra số dư không âm.

### 5. Giao diện Frontend Admin
- Dashboard Tồn kho trực quan, cảnh báo màu sắc theo hạn sử dụng (Đỏ: Hết hạn, Vàng: Cận date < 7 ngày, Xanh lá: An toàn); Bộ form lập phiếu Nhập kho, Xuất kho, Điều chuyển chuyên nghiệp.

---

## Module 11: Inventory Audit & Đối soát Kiểm kê, Xuất hủy

### 1. Mục tiêu Nghiệp vụ
Tổ chức các đợt kiểm kê định kỳ tại kho bãi, ghi nhận số lượng kiểm đếm thực tế, đối soát với số dư sổ sách điện tử, phát hiện chênh lệch (Thừa/Thiếu) và lập Phiếu điều chỉnh (Adjustment/Write-off) có phê duyệt của Giám đốc kho.

### 2. Mô hình Thực thể (Entities)
- `InventoryAudit`: Phiếu kiểm kê (Mã kiểm kê, Kho hàng, Ngày kiểm, Trạng thái: `Draft`, `InProgress`, `Completed`, `Cancelled`).
- `InventoryAuditDetail`: Chi tiết kiểm đếm từng Lô/SKU (`SystemQuantity`, `ActualQuantity`, `DiscrepancyQuantity` = Thực tế - Sổ sách).
- `InventoryAdjustment`: Phiếu điều chỉnh tồn kho sinh ra từ kiểm kê hoặc xuất hủy hàng hỏng.
- `InventoryAdjustmentDetail`: Chi tiết số lượng tăng/giảm và lý do (Hao hụt tự nhiên, Dập nát do bảo quản, Hết hạn sử dụng).

### 3. Thành phần Kỹ thuật Backend
- **Services:** `InventoryAuditService`, `InventoryAdjustmentService`, `DashboardService`.
- **Audit Log:** Tự động điều chỉnh số dư trong két sắt tồn kho 4 ngăn tương ứng với số chênh lệch đã được phê duyệt, lưu trữ vết kiểm toán phục vụ kế toán quản trị.

### 4. Giao diện Frontend Admin
- Màn hình kiểm kê hỗ trợ nhập liệu nhanh bằng máy quét mã vạch, tự động highlight dòng chênh lệch bằng màu đỏ/vàng; Báo cáo tỷ lệ thất thoát nông sản.

---

## Module 12: Shopping Cart (B2C) & Cơ chế Tự động Hợp nhất (Auto-Merge)

### 1. Mục tiêu Nghiệp vụ
Cung cấp trải nghiệm mua sắm mượt mà cho khách hàng vãng lai (Guest) lẫn khách hàng thân thiết đã đăng nhập. Hỗ trợ lưu trữ giỏ hàng độc lập và tự động hợp nhất giỏ hàng khi người dùng tiến hành đăng nhập.

### 2. Mô hình Thực thể & Trạng thái (State Management)
- **Guest Cart:** Lưu trữ Client-side bằng Zustand Persist Middleware (`localStorage`), cấu trúc gồm: `variantId`, `uoMId`, `quantity`.
- **Customer Cart (Database):** Bảng `Cart` và `CartItem` lưu trên SQL Server gắn với `CustomerId`.
- **Cơ chế Auto-Merge:** Khi người dùng đăng nhập thành công:
  1. Frontend đọc toàn bộ items từ Guest Cart.
  2. Gửi payload lên endpoint `/api/shop/cart/sync`.
  3. Backend so khớp: Nếu mặt hàng và ĐVT đã tồn tại trong giỏ Database thì cộng dồn số lượng; nếu chưa có thì thêm dòng mới.
  4. Trả về giỏ hàng hợp nhất hoàn chỉnh và xóa sạch Guest Cart cục bộ.

### 3. Thành phần Kỹ thuật Backend & Frontend Shop
- **Backend:** `ShopCartService`, `ShopCartController`.
- **Frontend Shop:** `cartStore.ts` (Zustand), `shopCartApi.ts`. Tự động kiểm tra giá bán và tính toán tổng tiền, tiền giảm giá, trạng thái hết hàng của từng dòng sản phẩm.

---

## Module 13: Order Processing & Tích hợp Vận chuyển GHN API v2

### 1. Mục tiêu Nghiệp vụ
Xử lý luồng đặt hàng B2C trực tuyến khép kín từ khâu chọn địa chỉ giao hàng, áp mã giảm giá, kiểm tra và tạm giữ tồn kho (Hold Stock), đến việc tích hợp trực tiếp với dịch vụ chuyển phát nhanh **Giao Hàng Nhanh (GHN Express API v2)** để tính phí ship và xuất vận đơn tự động.

### 2. Mô hình Thực thể (Entities)
- `Order`: Đơn đặt hàng (Mã đơn `OrderCode`, Khách hàng, Tổng tiền hàng, Tiền chiết khấu, Phí vận chuyển, Tổng thanh toán, Phương thức thanh toán: `COD`, `VNPay`, Trạng thái: `Pending`, `Confirmed`, `Processing`, `Delivering`, `Completed`, `Cancelled`).
- `OrderDetail`: Chi tiết mặt hàng (Biến thể SKU, ĐVT, Số lượng, Đơn giá, Tiền giảm).
- `OrderStatusHistory`: Lịch sử vết trạng thái phục vụ tra cứu tiến độ đơn hàng.

### 3. Tích hợp GHN Express API v2
- **Master Data Địa chỉ:** Tích hợp bộ API GHN:
  - `GET /master-data/province` (Danh sách Tỉnh/Thành phố).
  - `GET /master-data/district` (Danh sách Quận/Huyện theo Tỉnh).
  - `GET /master-data/ward` (Danh sách Phường/Xã theo Huyện).
- **Tính cước vận chuyển chuẩn xác:** Gọi API GHN tính cước dựa trên trọng lượng đóng gói (`grossWeightKg`), kích thước kiện hàng (`lengthCm`, `widthCm`, `heightCm`) và địa chỉ kho gần nhất.
- **Tạo vận đơn thật (`createOrder`):** Khi đơn hàng được duyệt, hệ thống tự động gửi yêu cầu sang GHN tạo mã vận đơn bưu tá, đồng thời in tem phiếu gửi hàng.
- **Chính sách Freeship:** Đơn hàng từ 300.000 ₫ trở lên hoặc các chiến dịch đặc biệt được tự động miễn phí 100% cước giao vận.

### 4. Giao diện Frontend Admin & Shop
- **Shop:** Trang Thanh toán `/thanh-toan` chọn địa chỉ từ Sổ địa chỉ GHN, hiển thị chi tiết tiền hàng, phí ship và tổng thanh toán.
- **Admin:** Màn hình Quản lý đơn hàng, duyệt đơn, theo dõi mã vận đơn GHN và trạng thái giao vận theo thời gian thực.

---

## Module 14: Payment Gateway (VNPay Sandbox) & Đổi trả Nông sản (RMA)

### 1. Mục tiêu Nghiệp vụ
Tích hợp cổng thanh toán trực tuyến quốc gia **VNPay** để thanh toán qua mã QR / ATM / Thẻ quốc tế; đồng thời hỗ trợ quy trình bảo vệ quyền lợi người tiêu dùng thông qua cơ chế Đổi trả hàng nông sản (Return Merchandise Authorization - RMA) trong vòng 24-48 giờ khi phát hiện sản phẩm hư hỏng hoặc không đạt chuẩn cam kết.

### 2. Cổng Thanh toán VNPay Sandbox
- **Tạo URL Thanh toán:** Ký số dữ liệu bằng thuật toán băm bảo mật `HMAC-SHA512` kết hợp `vnp_HashSecret`. URL redirect khách hàng sang cổng thanh toán VNPay với đầy đủ tham số kiểm tra.
- **Xác thực IPN & Callback:** Backend kiểm tra tính hợp lệ của `vnp_SecureHash` trong callback từ VNPay để phòng ngừa gian lận sửa URL trên trình duyệt; tự động cập nhật trạng thái đơn hàng sang `Paid` và chuyển kho xuất hàng.

### 3. Đổi trả Hàng Nông sản (RMA)
- **Đặc thù nông sản:** Hàng tươi sống có hạn bảo quản ngắn, quy định đổi trả nhanh chóng đối với các lỗi: dập nát, héo úng, giao sai chủng loại, không đúng độ ngọt cam kết.
- **Entities:** `CustomerReturn`, `CustomerReturnDetail`.
- **Cơ chế thu hồi kho:** Hàng trả về được phân loại: Nếu hư hỏng đưa vào ngăn `QuantityDamaged` chờ hủy; nếu đạt chuẩn đưa về ngăn `QuantityAvailable`. Tính toán số tiền hoàn trả (`RefundAmount`) chính xác theo đơn giá mua.

### 4. Giao diện Frontend Admin & Shop
- **Shop:** Trang Quản lý Đơn hàng có nút bấm "Yêu cầu Đổi/Trả hàng", chụp ảnh hiện trạng và chọn lý do.
- **Admin:** Phê duyệt đơn trả hàng, ghi chú kiểm định chất lượng và kích hoạt hoàn tiền cho khách.

---

## Module 15: AI Chatbot & Thương mại Đàm thoại (Conversational Commerce)

### 1. Mục tiêu Nghiệp vụ
Trợ lý ảo thông minh **Solaris AI** tích hợp sâu trong hệ thống, phục vụ tư vấn sản phẩm chuẩn VietGAP, tra cứu thông tin dinh dưỡng, giải đáp chính sách khuyến mãi, tra cứu tiến độ giao vận GHN, và đặc biệt là **chốt đơn hàng trực tiếp ngay trong giao diện đàm thoại (Conversational Commerce)**.

### 2. Kiến trúc Kỹ thuật & RAG (Retrieval-Augmented Generation)
- **Model:** Google Gemini Flash 2.5 / Pro tích hợp qua Google GenAI SDK.
- **RAG Context Grounding:** Trước khi sinh câu trả lời, AI truy xuất dữ liệu thực tế từ Database:
  - Danh mục ngành hàng và sản phẩm kinh doanh hiện tại.
  - Số lượng tồn kho khả dụng (`QuantityAvailable`) theo thời gian thực.
  - Các chương trình khuyến mãi đang có hiệu lực.
  - Trạng thái vận đơn giao nhận GHN của khách hàng.
- **Tiered Fuzzy Search Engine (Tìm kiếm phân tầng chống ảo giác):**
  - *Tier 1 (Đích danh):* Tìm kiếm chính xác tên hoặc chuỗi chứa tên sản phẩm.
  - *Tier 2 (Token nông sản):* Tách các từ khóa đặc trưng (Ví dụ: "sầu riêng", "bơ sáp") để tìm đúng biến thể tương ứng, loại bỏ hoàn toàn hiện tượng tự ý thêm các món hàng không liên quan vào đơn.
  - *Tier 3 (Danh mục fallback):* Chỉ kích hoạt khi không tìm thấy kết quả ở Tier 1 và 2.

### 3. Thẻ Đơn Hàng Tương Tác (Interactive Order Card)
Khi khách hàng ra hiệu lệnh mua (Ví dụ: *"Cho tôi 2 trái sầu riêng"*):
- AI tự động phân tích ý định, kiểm tra tồn kho, chuẩn bị payload đặt hàng và trả về thẻ UI tương tác nằm ngay trong luồng chat.
- Thẻ cho phép khách hàng:
  - Chọn địa chỉ giao hàng từ Dropdown Sổ địa chỉ GHN đã lưu; nếu chưa có địa chỉ, hiện thông báo kèm nút chuyển hướng nhanh đến trang cập nhật `/tai-khoan/dia-chi`.
  - Tăng/giảm số lượng bằng nút `[-]` `[+]` với cơ chế kiểm tra giới hạn tồn kho tức thời (báo cảnh báo nếu vượt quá số lượng kho còn lại).
  - Chọn phương thức thanh toán: Nhận hàng trả tiền (COD) hoặc Cổng thanh toán VNPay Sandbox.
  - Bấm nút **"Xác nhận Đặt hàng Ngay"**: Hệ thống tạo đơn hàng thực tế, cập nhật số dư kho, tạo mã vận đơn GHN, và trả về liên kết thanh toán VNPay ngay trong tin nhắn chat.

### 4. Kiểm thử Chuyên sâu
- Bao phủ đầy đủ các ca kiểm thử Unit Test (`TC01` đến `TC15` trong `GeminiChatServiceTests.cs`): Tra cứu tồn kho, cảnh báo thiếu hàng, tính giá Freeship, ngăn chặn ảo giác thêm thừa món hàng, và chốt đơn đàm thoại.

---

## BẢNG TỔNG HỢP KIẾN TRÚC & METRICS HỆ THỐNG

| Chỉ số | Chi tiết số liệu | Ghi chú |
| :--- | :--- | :--- |
| **Tổng số Module** | **15 Module** | Bao phủ từ IAM, Kho bãi, ERP, TMĐT đến AI Commerce |
| **Backend Unit Tests** | **390/390 Passed (100%)** | XUnit, Moq, InMemoryDb, EF Core 8 |
| **Frontend Shop Tests** | **75/75 Passed (100%)** | Vitest, React Testing Library, Mock Axios |
| **Frontend Admin Tests** | **334/334 Passed (100%)** | Vitest, Component & Form Testing |
| **Tổng Test Toàn Hệ Thống** | **799/799 Passed (100%)** | Độ tin cậy và ổn định tuyệt đối |
| **Tiêu chuẩn Code Quality** | **0 Lint Errors / 0 Warnings** | Eslint + TypeScript Strict Mode |
| **Độ sạch Comment** | **0 Emoji / 0 Informal Words** | 100% comment chuẩn mực kỹ thuật chuyên nghiệp |

---
*Tài liệu đặc tả được biên soạn tự động và phê duyệt cho nền tảng Sàn Nông Sản Sạch Solaris.*
