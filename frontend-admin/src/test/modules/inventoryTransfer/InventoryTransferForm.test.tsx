import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import InventoryTransferForm from '../../../pages/inventoryTransfer/InventoryTransferForm';
import { inventoryTransferApi } from '../../../api/inventoryTransferApi';
import { inventoryIssueApi } from '../../../api/inventoryIssueApi';
import { warehouseApi } from '../../../api/warehouseApi';
import { productVariantApi } from '../../../api/productVariantApi';
import { uomApi } from '../../../api/uomApi';
import { uomConversionApi } from '../../../api/uomConversionApi';

// Mock APIs
vi.mock('../../../api/uomConversionApi', () => ({
  uomConversionApi: {
    getValidUoMs: vi.fn().mockResolvedValue([]),
  },
}));

vi.mock('../../../api/inventoryTransferApi', () => ({
  inventoryTransferApi: {
    create: vi.fn(),
  },
}));

vi.mock('../../../api/inventoryIssueApi', () => ({
  inventoryIssueApi: {
    getSuggestedBatches: vi.fn(),
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

vi.mock('../../../api/uomApi', () => ({
  uomApi: {
    getAllList: vi.fn(),
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
 * MODULE 10: INVENTORY TRANSFERS (2-STEP DISPATCH & RECEIVE)
 * COMPONENT TEST: InventoryTransferForm (Lập Phiếu Điều Chuyển Liên Kho)
 * ============================================================================
 */
describe('Module 10 - InventoryTransferForm Component', () => {
  const mockWarehouses = [
    { id: 1, name: 'Tổng Kho Hà Nội' },
    { id: 2, name: 'Kho Nam Sài Gòn' },
  ];
  const mockVariants = [{ id: 1, name: 'Dâu Tây Đà Lạt Hộp 500g', code: 'SKU-DAUTAY-500G', baseUoMId: 1 }];
  const mockUoms = [{ id: 1, name: 'Hộp 500g' }];
  const mockBatches = [
    { batchId: 1, batchCode: 'BATCH-2026-001', expiryDate: '2026-12-31', quantityAvailable: 100 },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (warehouseApi.getAllList as any).mockResolvedValue(mockWarehouses);
    (productVariantApi.getAllList as any).mockResolvedValue(mockVariants);
    (uomApi.getAllList as any).mockResolvedValue(mockUoms);
    (inventoryIssueApi.getSuggestedBatches as any).mockResolvedValue(mockBatches);
  });

  // TC01: RENDER FORM LẬP PHIẾU ĐIỀU CHUYỂN
  it('TC01 - Render form lập phiếu điều chuyển với kho xuất, kho nhận và bảng hàng hóa', async () => {
    render(
      <MemoryRouter>
        <InventoryTransferForm />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(warehouseApi.getAllList).toHaveBeenCalled();
      expect(productVariantApi.getAllList).toHaveBeenCalled();
    });

    expect(screen.getByText('Tạo Lệnh Chuyển Kho')).toBeInTheDocument();
    expect(screen.getByText('1. Thông Tin Tuyến Chuyển Kho')).toBeInTheDocument();
    expect(screen.getByText('2. Danh Sách Mặt Hàng & Lô Hàng Chuyển Đi')).toBeInTheDocument();
  });

  // TC02: VALIDATION TRÙNG KHO NGUỒN VÀ KHO ĐÍCH
  it('TC02 - Báo lỗi validation khi chọn kho xuất và kho nhận trùng nhau', async () => {
    render(
      <MemoryRouter>
        <InventoryTransferForm />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(warehouseApi.getAllList).toHaveBeenCalled();
    });

    // Chọn Kho nguồn: Tổng Kho Hà Nội
    const fromLabel = screen.getByText('Kho nguồn (Xuất phát)');
    const fromTrigger = fromLabel.nextElementSibling as HTMLElement;
    fireEvent.click(fromTrigger);
    const fromOptions = await screen.findAllByText('Tổng Kho Hà Nội');
    fireEvent.click(fromOptions[0]);

    // Chọn Kho đích: Tổng Kho Hà Nội (trùng)
    const toLabel = screen.getByText('Kho đích (Tiếp nhận)');
    const toTrigger = toLabel.nextElementSibling as HTMLElement;
    fireEvent.click(toTrigger);
    const toOptions = await screen.findAllByText('Tổng Kho Hà Nội');
    fireEvent.click(toOptions[toOptions.length - 1]);

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    expect(await screen.findByText('Kho đích không được trùng kho nguồn')).toBeInTheDocument();
    expect(inventoryTransferApi.create).not.toHaveBeenCalled();
  });

  // TC03: SUBMIT FORM HỢP LỆ VỚI ĐVT TỰ ĐỘNG KHÓA THEO SKU
  it('TC03 - Submit form hợp lệ: ĐVT tự động khóa theo Base UoM, gọi API create và điều hướng', async () => {
    (inventoryTransferApi.create as any).mockResolvedValue({ id: 30, message: 'Thành công' });

    render(
      <MemoryRouter>
        <InventoryTransferForm />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(warehouseApi.getAllList).toHaveBeenCalled();
      expect(productVariantApi.getAllList).toHaveBeenCalled();
      expect(uomApi.getAllList).toHaveBeenCalled();
    });

    // 1. Chọn Kho nguồn: Tổng Kho Hà Nội
    const fromLabel = screen.getByText('Kho nguồn (Xuất phát)');
    const fromTrigger = fromLabel.nextElementSibling as HTMLElement;
    fireEvent.click(fromTrigger);
    const fromOption = await screen.findByText('Tổng Kho Hà Nội');
    fireEvent.click(fromOption);

    // 2. Chọn Kho đích: Kho Nam Sài Gòn
    const toLabel = screen.getByText('Kho đích (Tiếp nhận)');
    const toTrigger = toLabel.nextElementSibling as HTMLElement;
    fireEvent.click(toTrigger);
    const toOption = await screen.findByText('Kho Nam Sài Gòn');
    fireEvent.click(toOption);

    // 3. Chọn Sản phẩm -> Tự động điền ĐVT và khóa dropdown ĐVT
    const spTrigger = screen.getByText('Chọn sản phẩm...');
    fireEvent.click(spTrigger);
    const spOption = await screen.findByText('SKU-DAUTAY-500G - Dâu Tây Đà Lạt Hộp 500g');
    fireEvent.click(spOption);

    // Chờ hệ thống tự động phân bổ lô FEFO cho mặt hàng
    await waitFor(() => {
      expect(screen.getByText(/BATCH-2026-001/i)).toBeInTheDocument();
    });

    // 4. Nhập số lượng hợp lệ (40 <= 100 tồn khả dụng)
    const qtyInput = screen.getByRole('spinbutton');
    fireEvent.change(qtyInput, { target: { value: '40' } });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(inventoryTransferApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          fromWarehouseId: 1,
          toWarehouseId: 2,
          details: expect.arrayContaining([
            expect.objectContaining({
              variantId: 1,
              batchId: 1,
              uoMId: 1,
              quantity: 40,
            }),
          ]),
        })
      );
    });

    await waitFor(
      () => {
        expect(mockNavigate).toHaveBeenCalledWith('/inventory-transfers/30');
      },
      { timeout: 2500 }
    );
  });

  // TC04: CHẶN ĐẶT QUÁ SỐ LƯỢNG TỒN KHO KHẢ DỤNG
  it('TC04 - Báo lỗi và chặn submit khi số lượng chuyển vượt quá tồn kho khả dụng của lô hàng', async () => {
    (inventoryIssueApi.getSuggestedBatches as any).mockResolvedValue([
      { batchId: 1, batchCode: 'BATCH-2026-001', expiryDate: '2026-12-31', quantityAvailable: 25 },
    ]);

    render(
      <MemoryRouter>
        <InventoryTransferForm />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(warehouseApi.getAllList).toHaveBeenCalled();
      expect(productVariantApi.getAllList).toHaveBeenCalled();
    });

    // Chọn kho nguồn & kho đích
    const fromLabel = screen.getByText('Kho nguồn (Xuất phát)');
    fireEvent.click(fromLabel.nextElementSibling as HTMLElement);
    fireEvent.click(await screen.findByText('Tổng Kho Hà Nội'));

    const toLabel = screen.getByText('Kho đích (Tiếp nhận)');
    fireEvent.click(toLabel.nextElementSibling as HTMLElement);
    fireEvent.click(await screen.findByText('Kho Nam Sài Gòn'));

    // Chọn sản phẩm
    fireEvent.click(screen.getByText('Chọn sản phẩm...'));
    fireEvent.click(await screen.findByText('SKU-DAUTAY-500G - Dâu Tây Đà Lạt Hộp 500g'));

    // Chờ hệ thống tự động phân bổ lô FEFO cho mặt hàng
    await waitFor(() => {
      expect(screen.getByText(/BATCH-2026-001/i)).toBeInTheDocument();
    });

    // Nhập số lượng 50 (vượt quá tồn khả dụng là 25)
    const qtyInput = screen.getByRole('spinbutton');
    fireEvent.change(qtyInput, { target: { value: '50' } });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    // Báo lỗi vượt tồn kho và chặn gọi API create
    expect(await screen.findByText(/Tối đa 25/i)).toBeInTheDocument();
    expect(inventoryTransferApi.create).not.toHaveBeenCalled();
  });
});
