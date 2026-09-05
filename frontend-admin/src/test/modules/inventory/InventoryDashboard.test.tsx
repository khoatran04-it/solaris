import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import InventoryDashboard from '../../../pages/inventory/InventoryDashboard';
import { inventoryApi } from '../../../api/inventoryApi';
import { warehouseApi } from '../../../api/warehouseApi';

// Mock APIs
vi.mock('../../../api/inventoryApi', () => ({
  inventoryApi: {
    getAll: vi.fn(),
    getAllList: vi.fn(),
    getById: vi.fn(),
  },
}));

vi.mock('../../../api/warehouseApi', () => ({
  warehouseApi: {
    getAllList: vi.fn(),
  },
}));

/**
 * ============================================================================
 * MODULE 10: CORE INVENTORY ENGINE & 4-BUCKET LEDGER
 * COMPONENT TEST: InventoryDashboard (Tổng Quan Sổ Cái Tồn Kho 4 Ngăn)
 * ============================================================================
 */
describe('Module 10 - InventoryDashboard Component', () => {
  const mockWarehouses = [
    { id: 1, name: 'Tổng Kho Hà Nội', code: 'WH-HN-01' },
    { id: 2, name: 'Kho Nam Sài Gòn', code: 'WH-HCM-01' },
  ];

  const mockInventories = [
    {
      id: 1,
      warehouseId: 1,
      warehouseName: 'Tổng Kho Hà Nội',
      warehouseCode: 'WH-HN-01',
      variantId: 10,
      variantCode: 'SKU-DAUTAY-500G',
      variantName: 'Dâu Tây Đà Lạt Hộp 500g',
      baseUoMName: 'Hộp',
      batchId: 100,
      batchCode: 'BATCH-2026-001',
      manufactureDate: '2026-08-20T00:00:00Z',
      expiryDate: '2026-09-02T00:00:00Z',
      daysToExpiry: 3, // Cận date
      supplierName: 'Nông Trại Đà Lạt GAP',
      quantityAvailable: 150,
      quantityReserved: 30,
      quantityQC: 10,
      quantityDamaged: 5,
      totalQuantity: 195,
    },
    {
      id: 2,
      warehouseId: 1,
      warehouseName: 'Tổng Kho Hà Nội',
      warehouseCode: 'WH-HN-01',
      variantId: 11,
      variantCode: 'SKU-BO-01',
      variantName: 'Bơ Sáp 034 Đắk Lắk',
      baseUoMName: 'Kg',
      batchId: 101,
      batchCode: 'BATCH-2026-002',
      manufactureDate: '2026-08-25T00:00:00Z',
      expiryDate: '2026-09-25T00:00:00Z',
      daysToExpiry: 26,
      supplierName: 'HTX Nông Nghiệp Đắk Lắk',
      quantityAvailable: 0, // Hết hàng
      quantityReserved: 0,
      quantityQC: 0,
      quantityDamaged: 0,
      totalQuantity: 0,
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (warehouseApi.getAllList as any).mockResolvedValue(mockWarehouses);
    (inventoryApi.getAll as any).mockResolvedValue({
      items: mockInventories,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // TC01: RENDER TỔNG QUAN TỒN KHO
  it('TC01 - Render dashboard với danh sách mặt hàng tồn kho và các ngăn số dư (Available, Reserved, QC, Damaged)', async () => {
    render(
      <MemoryRouter>
        <InventoryDashboard />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(warehouseApi.getAllList).toHaveBeenCalled();
      expect(inventoryApi.getAll).toHaveBeenCalled();
    });

    // Kiểm tra hiển thị tên sản phẩm, mã SKU và mã Lô
    expect(await screen.findByText('Dâu Tây Đà Lạt Hộp 500g')).toBeInTheDocument();
    expect(screen.getByText('SKU-DAUTAY-500G')).toBeInTheDocument();
    expect(screen.getByText('BATCH-2026-001')).toBeInTheDocument();
    expect(screen.getByText('Bơ Sáp 034 Đắk Lắk')).toBeInTheDocument();

    // Kiểm tra số dư các ngăn (150 khả dụng, 30 giữ chỗ, + 10 QC, 5 hỏng)
    expect(screen.getByText('150')).toBeInTheDocument();
    expect(screen.getByText('30')).toBeInTheDocument();
    expect(screen.getByText(/\+ 10 QC/i)).toBeInTheDocument();
    expect(screen.getByText('5')).toBeInTheDocument();
  });

  // TC02: THAY ĐỔI KHO TRÊN SELECTOR
  it('TC02 - Tự động chọn kho mặc định đầu tiên và cho phép chuyển đổi kho xem tồn kho', async () => {
    render(
      <MemoryRouter>
        <InventoryDashboard />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(inventoryApi.getAll).toHaveBeenCalledWith(expect.objectContaining({ warehouseId: 1 }));
    });

    // Mở dropdown chọn kho và chuyển đổi sang Kho 2
    const whTrigger = screen.getByText(/Tổng Kho Hà Nội/i);
    fireEvent.click(whTrigger);

    const whOption = await screen.findByText(/Kho Nam Sài Gòn/i);
    fireEvent.click(whOption);

    await waitFor(() => {
      expect(inventoryApi.getAll).toHaveBeenCalledWith(expect.objectContaining({ warehouseId: 2 }));
    });
  });

  // TC03: LỌC HÀNG CẬN DATE & HÀNG HẾT
  it('TC03 - Kích hoạt bộ lọc nhanh nông sản (Hàng cận hạn sử dụng & Hàng hết tồn khả dụng)', async () => {
    render(
      <MemoryRouter>
        <InventoryDashboard />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Dâu Tây Đà Lạt Hộp 500g')).toBeInTheDocument();
    });

    // Bấm toggle nút "Hàng Sắp Hết Hạn"
    const expiringBtn = screen.getByRole('button', { name: /Hàng Sắp Hết Hạn/i });
    fireEvent.click(expiringBtn);

    await waitFor(() => {
      expect(inventoryApi.getAll).toHaveBeenCalledWith(
        expect.objectContaining({ isExpiringSoon: true })
      );
    });

    // Bấm toggle nút "Lọc Cạn Kho"
    const outOfStockBtn = screen.getByRole('button', { name: /Lọc Cạn Kho/i });
    fireEvent.click(outOfStockBtn);

    await waitFor(() => {
      expect(inventoryApi.getAll).toHaveBeenCalledWith(
        expect.objectContaining({ isOutOfStock: true })
      );
    });
  });

  // TC04: TÌM KIẾM THEO TÊN SẢN PHẨM HOẶC SKU
  it('TC04 - Nhập từ khóa tìm kiếm theo tên hoặc mã SKU kích hoạt debounce API call', async () => {
    render(
      <MemoryRouter>
        <InventoryDashboard />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Dâu Tây Đà Lạt Hộp 500g')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText(/Tìm Tên, Mã SKU, Mã Lô/i);
    fireEvent.change(searchInput, { target: { value: 'DAUTAY' } });

    await waitFor(
      () => {
        expect(inventoryApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({ search: 'DAUTAY' })
        );
      },
      { timeout: 1500 }
    );
  });
});
