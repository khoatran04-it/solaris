import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import InventoryReceiptForm from '../../../pages/inventoryReceipt/InventoryReceiptForm';
import { inventoryReceiptApi } from '../../../api/inventoryReceiptApi';
import { warehouseApi } from '../../../api/warehouseApi';
import { supplierApi } from '../../../api/supplierApi';
import { productVariantApi } from '../../../api/productVariantApi';
import { uomApi } from '../../../api/uomApi';
import { productBatchApi } from '../../../api/productBatchApi';

// Mock APIs
vi.mock('../../../api/inventoryReceiptApi', () => ({
  inventoryReceiptApi: {
    create: vi.fn(),
  },
}));

vi.mock('../../../api/warehouseApi', () => ({
  warehouseApi: {
    getAllList: vi.fn(),
    getCapacityStatus: vi.fn().mockResolvedValue({
      warehouseId: 1,
      warehouseCode: 'WH-01',
      warehouseName: 'Kho Test',
      totalCapacityCbm: 500,
      occupiedCbm: 50,
      availableCbm: 450,
      occupancyRateCbm: 10,
      maxWeightCapacityKg: 100000,
      occupiedWeightKg: 5000,
      availableWeightKg: 95000,
      occupancyRateWeight: 5,
      warningThresholdPercent: 85,
      status: 'Safe',
    }),
  },
}));

vi.mock('../../../api/supplierApi', () => ({
  supplierApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/productVariantApi', () => ({
  productVariantApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/uomApi', () => ({
  uomApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/productBatchApi', () => ({
  productBatchApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/purchaseOrderApi', () => ({
  purchaseOrderApi: {
    getById: vi.fn(),
  },
}));

vi.mock('../../../stores/useAuthStore', () => ({
  useAuthStore: () => ({
    userInfo: { id: 1, fullName: 'Admin' },
  }),
}));

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useSearchParams: () => [new URLSearchParams()],
  };
});

/**
 * ============================================================================
 * 📥 MODULE 10: INVENTORY RECEIPTS & QUALITY CONTROL (GRN)
 * 🧪 COMPONENT TEST: InventoryReceiptForm (Tạo Mới Phiếu Nhập Kho & Kiểm Đếm)
 * ============================================================================
 */
describe('Module 10 - InventoryReceiptForm Component', () => {
  const mockWarehouses = [{ id: 1, name: 'Tổng Kho Hà Nội' }];
  const mockSuppliers = [{ id: 1, name: 'Nông Trại Đà Lạt GAP' }];
  const mockVariants = [{ id: 1, name: 'Dâu Tây Đà Lạt Hộp 500g', code: 'SKU-DAUTAY-500G' }];
  const mockUoms = [{ id: 1, name: 'Hộp 500g' }];
  const mockBatches = [{ id: 1, variantId: 1, batchCode: 'BATCH-2026-001' }];

  beforeEach(() => {
    vi.clearAllMocks();
    (warehouseApi.getAllList as any).mockResolvedValue(mockWarehouses);
    (warehouseApi.getCapacityStatus as any).mockResolvedValue({
      warehouseId: 1,
      warehouseCode: 'WH-01',
      warehouseName: 'Kho Test',
      totalCapacityCbm: 500,
      occupiedCbm: 50,
      availableCbm: 450,
      occupancyRateCbm: 10,
      maxWeightCapacityKg: 100000,
      occupiedWeightKg: 5000,
      availableWeightKg: 95000,
      occupancyRateWeight: 5,
      warningThresholdPercent: 85,
      status: 'Safe',
    });
    (supplierApi.getAllList as any).mockResolvedValue(mockSuppliers);
    (productVariantApi.getAllList as any).mockResolvedValue(mockVariants);
    (uomApi.getAllList as any).mockResolvedValue(mockUoms);
    (productBatchApi.getAllList as any).mockResolvedValue(mockBatches);
  });

  // TC01: RENDER FORM TẠO MỚI PHIẾU NHẬP
  it('TC01 - Render form tạo phiếu nhập kho với các trường kho nhận, nhà cung cấp và bảng kiểm đếm', async () => {
    render(
      <MemoryRouter>
        <InventoryReceiptForm />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(warehouseApi.getAllList).toHaveBeenCalled();
      expect(supplierApi.getAllList).toHaveBeenCalled();
    });

    expect(screen.getByText('Phiếu Nhập Kho')).toBeInTheDocument();
    expect(screen.getByText('1. Thông Tin Chung')).toBeInTheDocument();
    expect(screen.getByText('2. Chi Tiết Mặt Hàng')).toBeInTheDocument();
  });

  // TC02: VALIDATION TRƯỜNG BẮT BUỘC
  it('TC02 - Báo lỗi validation khi chưa chọn kho nhận hoặc chưa hoàn thiện dòng kiểm đếm', async () => {
    render(
      <MemoryRouter>
        <InventoryReceiptForm />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(warehouseApi.getAllList).toHaveBeenCalled();
    });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    expect(await screen.findByText(/Vui lòng chọn Kho/i)).toBeInTheDocument();
    expect(inventoryReceiptApi.create).not.toHaveBeenCalled();
  });

  // TC03: THÊM DÒNG HÀNG VÀ TÍNH TOÁN QC
  it('TC03 - Cho phép thêm dòng hàng mới trong bảng kiểm đếm', async () => {
    render(
      <MemoryRouter>
        <InventoryReceiptForm />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(productVariantApi.getAllList).toHaveBeenCalled();
    });

    const addRowBtn = screen.getByRole('button', { name: /THÊM DÒNG MỚI/i });
    fireEvent.click(addRowBtn);

    // Có ít nhất 2 dòng số thứ tự 1 và 2
    expect(screen.getByText('2')).toBeInTheDocument();
  });

  // TC04: SUBMIT FORM HỢP LỆ
  it('TC04 - Submit form hợp lệ gọi API create và điều hướng', async () => {
    (inventoryReceiptApi.create as any).mockResolvedValue({ id: 10, message: 'Thành công' });

    render(
      <MemoryRouter>
        <InventoryReceiptForm />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(warehouseApi.getAllList).toHaveBeenCalled();
      expect(supplierApi.getAllList).toHaveBeenCalled();
      expect(productVariantApi.getAllList).toHaveBeenCalled();
      expect(uomApi.getAllList).toHaveBeenCalled();
    });

    // 1. Chọn Kho Lưu Trữ
    const whLabel = screen.getByText('Kho Lưu Trữ');
    const whSelectTrigger = whLabel.nextElementSibling as HTMLElement;
    fireEvent.click(whSelectTrigger);
    const whOption = await screen.findByText('Tổng Kho Hà Nội');
    fireEvent.click(whOption);

    // 2. Chọn Sản Phẩm (Dòng 1)
    const spTrigger = screen.getByText('Chọn SP...');
    fireEvent.click(spTrigger);
    const spOption = await screen.findByText('SKU-DAUTAY-500G - Dâu Tây Đà Lạt Hộp 500g');
    fireEvent.click(spOption);

    // 3. Chọn ĐVT (Dòng 1)
    const uomTriggers = screen.getAllByText('ĐVT');
    fireEvent.click(uomTriggers[1]); // The select placeholder (not table header th)
    const uomOption = await screen.findByText('Hộp 500g');
    fireEvent.click(uomOption);

    // 4. Chọn Lô (Dòng 1)
    const batchTrigger = screen.getByText('Lô...');
    fireEvent.click(batchTrigger);
    const batchOption = await screen.findByText('BATCH-2026-001');
    fireEvent.click(batchOption);

    // 5. Nhập số lượng thực nhận và dự kiến
    const numberInputs = screen.getAllByRole('spinbutton');
    fireEvent.change(numberInputs[0], { target: { value: '100' } }); // Dự kiến
    fireEvent.change(numberInputs[1], { target: { value: '95' } });  // Thực nhận
    fireEvent.change(numberInputs[2], { target: { value: '5' } });   // Trả về

    // 6. Nhập lý do lỗi nếu có trả về
    const reasonInput = screen.getByPlaceholderText(/Lý do.../i);
    fireEvent.change(reasonInput, { target: { value: 'Héo cuống' } });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(inventoryReceiptApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          warehouseId: 1,
          details: expect.arrayContaining([
            expect.objectContaining({
              variantId: 1,
              batchId: 1,
              uoMId: 1,
              acceptedQuantity: 95,
              rejectedQuantity: 5,
              rejectReason: 'Héo cuống',
            }),
          ]),
        })
      );
    });

    await waitFor(
      () => {
        expect(mockNavigate).toHaveBeenCalledWith('/inventory-receipts');
      },
      { timeout: 2500 }
    );
  });

  // #region TC04: HIỂN THỊ THANH TRẠNG THÁI SỨC CHỨA KHO HÀNG (CAPACITY GAUGE)
  it('TC04 - Render thanh trạng thái Sức chứa Kho hàng (Capacity Gauge) khi chọn kho tiếp nhận', async () => {
    render(
      <MemoryRouter>
        <InventoryReceiptForm />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(warehouseApi.getAllList).toHaveBeenCalled();
      expect(screen.getByText('Kho Lưu Trữ')).toBeInTheDocument();
    });

    // 1. Chọn Kho Lưu Trữ
    const whLabel = screen.getByText('Kho Lưu Trữ');
    const whSelectTrigger = whLabel.nextElementSibling as HTMLElement;
    fireEvent.click(whSelectTrigger);
    const whOption = await screen.findByText('Tổng Kho Hà Nội');
    fireEvent.click(whOption);

    // 2. Kiểm tra hiển thị thanh đo sức chứa kho
    await waitFor(() => {
      // Capacity gauge API được gọi với đúng warehouseId
      expect(warehouseApi.getCapacityStatus).toHaveBeenCalledWith(1);
      // Tiêu đề gauge hiển thị tên + mã kho
      expect(screen.getByText(/Sức chứa kho: Kho Test \(WH-01\)/i)).toBeInTheDocument();
      // CBM đã dùng / tổng CBM và tỷ lệ chiếm dụng
      expect(screen.getByText(/50 \/ 500 m³ \(Đang chứa 10%\)/i)).toBeInTheDocument();
      // Tải trọng sàn
      expect(screen.getByText(/Tải trọng sàn: 5000 \/ 100000 kg \(5%\)/i)).toBeInTheDocument();
    });
  });
  // #endregion
});
