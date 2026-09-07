import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import InventoryAdjustmentForm from '../../../pages/inventoryAdjustment/InventoryAdjustmentForm';
import { inventoryAdjustmentApi } from '../../../api/inventoryAdjustmentApi';
import { warehouseApi } from '../../../api/warehouseApi';
import { productVariantApi } from '../../../api/productVariantApi';
import { productBatchApi } from '../../../api/productBatchApi';
import { uomApi } from '../../../api/uomApi';

// Mock APIs
vi.mock('../../../api/inventoryAdjustmentApi', () => ({
  inventoryAdjustmentApi: {
    create: vi.fn(),
  },
}));

vi.mock('../../../api/warehouseApi', () => ({
  warehouseApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/productVariantApi', () => ({
  productVariantApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/productBatchApi', () => ({
  productBatchApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/uomApi', () => ({
  uomApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/inventoryIssueApi', () => ({
  inventoryIssueApi: {
    getSuggestedBatches: vi.fn().mockResolvedValue([]),
  },
}));

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

/**
 * ============================================================================
 * ️ MODULE 11: INVENTORY ADJUSTMENT & WRITE-OFF
 * COMPONENT TEST: InventoryAdjustmentForm (Tạo Mới Đề Xuất Điều Chỉnh)
 * ============================================================================
 */
describe('Module 11 - InventoryAdjustmentForm Component', () => {
  const mockWarehouses = [{ id: 1, name: 'Tổng Kho Hà Nội', code: 'WH-HN-01' }];
  const mockVariants = [
    {
      id: 1,
      code: 'SKU-DAUTAY-500G',
      name: 'Dâu Tây Hộp 500g',
      prices: [{ id: 10, uoMId: 1, price: 50000, isDefault: true }],
    },
  ];
  const mockBatches = [
    {
      id: 1,
      batchCode: 'BATCH-2026-001',
      variantId: 1,
      expiryDate: '2026-09-15T00:00:00Z',
    },
  ];
  const mockUoms = [{ id: 1, name: 'Hộp 500g', code: 'BOX' }];

  beforeEach(() => {
    vi.clearAllMocks();
    (warehouseApi.getAllList as any).mockResolvedValue(mockWarehouses);
    (productVariantApi.getAllList as any).mockResolvedValue(mockVariants);
    (productBatchApi.getAllList as any).mockResolvedValue(mockBatches);
    (uomApi.getAllList as any).mockResolvedValue(mockUoms);
    (inventoryAdjustmentApi.create as any).mockResolvedValue({ id: 105 });
  });

  // TC01: RENDER FORM TẠO PHIẾU ĐIỀU CHỈNH
  it('TC01 - Render form tạo phiếu điều chỉnh với dropdown kho, lý do và bảng chi tiết', async () => {
    render(
      <MemoryRouter>
        <InventoryAdjustmentForm />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(warehouseApi.getAllList).toHaveBeenCalled();
      expect(productVariantApi.getAllList).toHaveBeenCalled();
    });

    expect(screen.getByText('Tạo Phiếu Điều Chỉnh & Xuất Hủy Tồn Kho')).toBeInTheDocument();
    expect(screen.getByText('Kho hàng xảy ra biến động')).toBeInTheDocument();
    expect(screen.getByText('Lý do điều chỉnh')).toBeInTheDocument();
    expect(screen.getByText('2. Danh Sách Mặt Hàng Cần Điều Chỉnh')).toBeInTheDocument();
  });

  // TC02: THÊM DÒNG CHI TIẾT
  it('TC02 - Nhấn THÊM MẶT HÀNG thêm thành công một dòng mới vào bảng', async () => {
    render(
      <MemoryRouter>
        <InventoryAdjustmentForm />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(warehouseApi.getAllList).toHaveBeenCalled();
    });

    const addRowBtn = screen.getByRole('button', { name: /THÊM MẶT HÀNG/i });
    fireEvent.click(addRowBtn);

    // Should now have multiple options for "-- Chọn sản phẩm --"
    expect(screen.getAllByText('-- Chọn sản phẩm --')).toHaveLength(2);
  });

  // TC03: KHÓA CỨNG ĐVT KHI CHỌN SẢN PHẨM
  it('TC03 - Khi chọn Sản phẩm thì ĐVT tự động gán và bị khóa cứng (disabled)', async () => {
    render(
      <MemoryRouter>
        <InventoryAdjustmentForm />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(productVariantApi.getAllList).toHaveBeenCalled();
    });

    // Chọn sản phẩm
    const productSelectTrigger = screen.getByText('-- Chọn sản phẩm --');
    fireEvent.click(productSelectTrigger);

    const productOption = await screen.findByText('SKU-DAUTAY-500G - Dâu Tây Hộp 500g');
    fireEvent.click(productOption);

    // ĐVT tự động chọn Hộp 500g và bị khóa cứng (disabled)
    await waitFor(() => {
      const uomElement = screen.getByText('Hộp 500g');
      expect(uomElement).toBeInTheDocument();
      // Kiểm tra container cha có class disabled
      const uomContainer = uomElement.closest('div');
      expect(uomContainer?.className).toContain('cursor-not-allowed');
    });
  });
});
