import { PaginationParams } from './common';

// =========================================================
// 1. ENUMS & UI CONSTANTS (Trạng thái Phiếu Nhập)
// =========================================================
export enum InventoryReceiptStatus {
  Pending = 1, // Chờ nhập kho (Phiếu vừa tạo, chờ xe tải tới)
  Inspecting = 2, // Đang kiểm đếm (Bốc hàng, cân đo, ghi nhận SL thực tế)
  Completed = 3, // Hoàn tất (Đã duyệt nhập kho, tồn kho đã được cộng)
  Cancelled = 4, // Đã hủy (Lập sai / Hàng bị từ chối toàn bộ)
}

// Dùng Object.keys để lấy key chuẩn số nguyên, chống lỗi Reverse Mapping
export const InventoryReceiptStatusLabels: Record<InventoryReceiptStatus, string> = {
  [InventoryReceiptStatus.Pending]: 'Chờ xử lý',
  [InventoryReceiptStatus.Inspecting]: 'Đang kiểm đếm',
  [InventoryReceiptStatus.Completed]: 'Hoàn tất',
  [InventoryReceiptStatus.Cancelled]: 'Đã hủy',
};

export const InventoryReceiptStatusColors: Record<InventoryReceiptStatus, string> = {
  [InventoryReceiptStatus.Pending]: 'bg-amber-50 text-amber-700 border-amber-200',
  [InventoryReceiptStatus.Inspecting]: 'bg-blue-50 text-blue-700 border-blue-200',
  [InventoryReceiptStatus.Completed]: 'bg-emerald-50 text-emerald-700 border-emerald-200',
  [InventoryReceiptStatus.Cancelled]: 'bg-rose-50 text-rose-700 border-rose-200',
};

// =========================================================
// 2. MODELS (Dùng để hứng dữ liệu từ Backend - ReadDto)
// =========================================================

// Chi tiết phiếu nhập
export interface InventoryReceiptDetail {
  id: number;

  variantId: number;
  variantName: string;
  variantCode: string; // Thêm mã SKU để UI dễ bề hiển thị

  batchId: number;
  batchCode: string;

  uoMId: number;
  uoMName: string;

  purchaseOrderDetailId?: number; // Biết được dòng nhập này từ dòng PO nào

  expectedQuantity: number;
  acceptedQuantity: number;
  rejectedQuantity: number;
  rejectReason?: string;

  actualWeightKg?: number;
  calculatedCbm?: number;
}

// Phiếu nhập kho (Header)
export interface InventoryReceipt {
  id: number;
  receiptCode: string;
  status: InventoryReceiptStatus;
  receiptDate?: string; // Ngày giờ thực tế xe tải cập bến (ISO String)
  note?: string;
  cancellationReason?: string;

  warehouseId: number;
  warehouseName: string;

  supplierId?: number;
  supplierName?: string;

  receivedById?: number;
  receivedByName?: string;

  createdAt: string;
  updatedAt: string;

  details: InventoryReceiptDetail[];
}

// =========================================================
// 3. PAYLOADS (Dùng để Thêm/Sửa gửi lên Backend)
// =========================================================

export interface InventoryReceiptDetailPayload {
  variantId: number;
  batchId: number; // Bắt buộc chỉ định BatchId để quản lý theo dõi lô hàng
  uoMId: number;
  purchaseOrderDetailId?: number;

  expectedQuantity: number;
  acceptedQuantity: number;
  rejectedQuantity: number;
  rejectReason?: string;

  actualWeightKg?: number;
  calculatedCbm?: number;
}

export interface InventoryReceiptCreatePayload {
  warehouseId: number;
  supplierId?: number;
  // Không cần gửi receivedById từ FE nếu Backend lấy từ Token của người đăng nhập (Giống CreatedById của PO).
  // Trừ trường hợp Kế toán nhập giùm cho Thủ kho thì mới cần truyền mã Thủ kho lên.
  receivedById?: number;

  receiptDate?: string;
  note?: string;
  details: InventoryReceiptDetailPayload[];
}

// Payload cập nhật trạng thái (Dùng cho API PATCH)
export interface InventoryReceiptUpdateStatusPayload {
  status: InventoryReceiptStatus;
  cancellationReason?: string;
}

// =========================================================
// 4. QUERY PARAMS (Dùng cho Bộ lọc)
// =========================================================
export interface InventoryReceiptQueryParams extends PaginationParams {
  warehouseId?: number | string;
  supplierId?: number | string;
  status?: InventoryReceiptStatus;
  startDate?: string;
  endDate?: string;
}
