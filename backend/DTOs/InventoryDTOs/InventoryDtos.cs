namespace backend.DTOs.InventoryDTOs
{
    // DTO này dùng để hiển thị trên Bảng danh sách Tồn kho hiện tại
    public class InventoryReadDto
    {
        public int Id { get; set; } // ID dòng tồn kho

        // 1. Tọa độ Kho
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string WarehouseCode { get; set; } = string.Empty;

        // 2. Thông tin Sản phẩm (Biến thể)
        public int VariantId { get; set; }
        public string VariantCode { get; set; } = string.Empty; // Mã SKU
        public string VariantName { get; set; } = string.Empty; // Tên SP
        public string BaseUoMName { get; set; } = string.Empty; // ĐVT (Cực kỳ quan trọng: Kg, Bó, Quả...)

        // 3. Thông tin Lô hàng & Cảnh báo Nông sản
        public int BatchId { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public DateTime ManufactureDate { get; set; }
        public DateTime ExpiryDate { get; set; }

        // 🔥 Trường tính toán cho Frontend: Số ngày còn lại trước khi hết hạn
        public int DaysToExpiry => (ExpiryDate - DateTime.UtcNow).Days;

        // Tên Nhà cung cấp (Để biết lô rau này của bác nông dân nào)
        public string SupplierName { get; set; } = string.Empty;

        // 4. Số liệu Tồn kho (Hiển thị 4 cột rõ ràng)
        public decimal QuantityAvailable { get; set; } // Hàng xanh (Bán được)
        public decimal QuantityReserved { get; set; }  // Hàng vàng (Đã chốt đơn, chờ giao)
        public decimal QuantityQC { get; set; }        // Hàng cam (Boom hàng chờ check)
        public decimal QuantityDamaged { get; set; }   // Hàng đỏ (Thối, hỏng)

        // Tổng tồn kho vật lý đang nằm trong tòa nhà
        public decimal TotalQuantity => QuantityAvailable + QuantityReserved + QuantityQC + QuantityDamaged;
    }
}