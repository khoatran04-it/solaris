namespace backend.DTOs.InventoryReconciliationDTOs
{
    public class ShiftClosingItemDto
    {
        public int VariantId { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public string VariantCode { get; set; } = string.Empty;
        public string UoMName { get; set; } = string.Empty;

        public decimal OpeningStock { get; set; }   // Tồn đầu ca
        public decimal TotalReceipt { get; set; }    // Nhập mua hàng (IR)
        public decimal TotalTransferIn { get; set; } // Chuyển đến (TRF In)
        public decimal TotalReturn { get; set; }     // Khách trả lại (RET)
        public decimal TotalIssue { get; set; }      // Xuất bán (ISS)
        public decimal TotalTransferOut { get; set; }// Chuyển đi (TRF Out)
        public decimal TotalAdjustment { get; set; } // Điều chỉnh (+ / -)
        public decimal ClosingStock { get; set; }    // Tồn cuối ca lý thuyết
        public decimal CurrentAvailable { get; set; }// Tồn khả dụng hiện tại
        public decimal CurrentReserved { get; set; } // Tồn giữ chỗ hiện tại
        public decimal CurrentDamaged { get; set; }  // Hàng hỏng hiện tại
        public decimal CurrentQC { get; set; }       // Hàng chờ kiểm định QC hiện tại
    }

    public class ShiftClosingReportDto
    {
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public decimal TotalOpeningItems { get; set; }
        public decimal TotalInflowItems { get; set; }
        public decimal TotalOutflowItems { get; set; }
        public decimal TotalClosingItems { get; set; }

        public List<ShiftClosingItemDto> Items { get; set; } = new();
    }

    public class StockLedgerEntryDto
    {
        public DateTime TransactionDate { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public string ReferenceCode { get; set; } = string.Empty;
        public string VariantName { get; set; } = string.Empty;
        public string BatchCode { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Note { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
    }
}
