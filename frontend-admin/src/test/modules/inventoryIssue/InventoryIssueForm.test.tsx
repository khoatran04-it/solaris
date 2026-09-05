import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import InventoryIssueForm from '../../../pages/inventoryIssue/InventoryIssueForm';
import { inventoryIssueApi } from '../../../api/inventoryIssueApi';
import { warehouseApi } from '../../../api/warehouseApi';
import { productVariantApi } from '../../../api/productVariantApi';
import { uomApi } from '../../../api/uomApi';
import { productBatchApi } from '../../../api/productBatchApi';
import { orderApi } from '../../../api/orderApi';

// Mock APIs
vi.mock('../../../api/inventoryIssueApi', () => ({
  inventoryIssueApi: {
    create: vi.fn(),
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

vi.mock('../../../api/productBatchApi', () => ({
  productBatchApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/orderApi', () => ({
  orderApi: {
    getAll: vi.fn(),
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
 * 📤 MODULE 10: INVENTORY ISSUES & FEFO SMART PICKER
 * 🧪 COMPONENT TEST: InventoryIssueForm (Tạo Mới Phiếu Xuất Kho & FEFO)
 * ============================================================================
 */
describe('Module 10 - InventoryIssueForm Component', () => {
  const mockWarehouses = [{ id: 1, name: 'Tổng Kho Hà Nội' }];
  const mockVariants = [
    {
      id: 1,
      name: 'Dâu Tây Đà Lạt Hộp 500g',
      code: 'SKU-DAUTAY-500G',
      prices: [{ uoMId: 1, price: 50000 }],
    },
  ];
  const mockUoms = [{ id: 1, name: 'Hộp 500g' }];
  const mockBatches = [{ id: 1, variantId: 1, batchCode: 'BATCH-2026-001' }];

  beforeEach(() => {
    vi.clearAllMocks();
    (warehouseApi.getAllList as any).mockResolvedValue(mockWarehouses);
    (productVariantApi.getAllList as any).mockResolvedValue(mockVariants);
    (uomApi.getAllList as any).mockResolvedValue(mockUoms);
    (productBatchApi.getAllList as any).mockResolvedValue(mockBatches);
    (orderApi.getAll as any).mockResolvedValue({ items: [], totalRecords: 0 });
  });

  // TC01: RENDER FORM TẠO MỚI PHIẾU XUẤT
  it('TC01 - Render form tạo phiếu xuất kho với các trường thông tin và bảng chi tiết hàng xuất', async () => {
    render(
      <MemoryRouter>
        <InventoryIssueForm />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(warehouseApi.getAllList).toHaveBeenCalled();
      expect(productVariantApi.getAllList).toHaveBeenCalled();
    });

    expect(screen.getByText('Tạo Phiếu Xuất Kho')).toBeInTheDocument();
    expect(screen.getByText('1. Thông Tin Phiếu Xuất')).toBeInTheDocument();
    expect(screen.getByText('2. Chi Tiết Đóng Gói Theo Lô Hàng')).toBeInTheDocument();
  });

  // TC02: VALIDATION TRƯỜNG BẮT BUỘC
  it('TC02 - Báo lỗi validation khi chưa chọn kho hàng xuất hoặc chưa chọn mặt hàng', async () => {
    render(
      <MemoryRouter>
        <InventoryIssueForm />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(warehouseApi.getAllList).toHaveBeenCalled();
    });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    expect(await screen.findByText('Vui lòng chọn kho xuất')).toBeInTheDocument();
    expect(inventoryIssueApi.create).not.toHaveBeenCalled();
  });

  // TC03: GỢI Ý LẤY HÀNG THEO LÔ FEFO
  it('TC03 - Bấm nút FEFO gọi API getSuggestedBatches', async () => {
    (inventoryIssueApi.getSuggestedBatches as any).mockResolvedValue([
      {
        batchId: 1,
        batchCode: 'BATCH-2026-001',
        expiryDate: '2026-09-02T00:00:00Z',
        quantityAvailable: 30,
        suggestedPickQuantity: 30,
      },
    ]);

    render(
      <MemoryRouter>
        <InventoryIssueForm />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(warehouseApi.getAllList).toHaveBeenCalled();
      expect(productVariantApi.getAllList).toHaveBeenCalled();
    });

    // Chọn kho xuất
    const whLabel = screen.getByText('Kho xuất hàng');
    const whSelectTrigger = whLabel.nextElementSibling as HTMLElement;
    fireEvent.click(whSelectTrigger);
    const whOption = await screen.findByText('Tổng Kho Hà Nội');
    fireEvent.click(whOption);

    // Chọn variant
    const spTrigger = screen.getByText('Chọn sản phẩm...');
    fireEvent.click(spTrigger);
    const spOption = await screen.findByText('SKU-DAUTAY-500G - Dâu Tây Đà Lạt Hộp 500g');
    fireEvent.click(spOption);

    const suggestBtn = screen.getByTitle(/Tự động chọn Lô hết hạn trước/i);
    fireEvent.click(suggestBtn);

    await waitFor(() => {
      expect(inventoryIssueApi.getSuggestedBatches).toHaveBeenCalledWith(1, 1, expect.any(Number));
    });
  });

  // TC04: SUBMIT FORM HỢP LỆ
  it('TC04 - Submit form hợp lệ gọi API create và điều hướng trang chi tiết', async () => {
    (inventoryIssueApi.create as any).mockResolvedValue({ id: 20, message: 'Thành công' });
    (inventoryIssueApi.getSuggestedBatches as any).mockResolvedValue([
      {
        batchId: 1,
        batchCode: 'BATCH-2026-001',
        expiryDate: '2026-09-02T00:00:00Z',
        quantityAvailable: 30,
        suggestedPickQuantity: 25,
      },
    ]);

    render(
      <MemoryRouter>
        <InventoryIssueForm />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(warehouseApi.getAllList).toHaveBeenCalled();
      expect(productVariantApi.getAllList).toHaveBeenCalled();
      expect(uomApi.getAllList).toHaveBeenCalled();
    });

    // 1. Chọn kho xuất
    const whLabel = screen.getByText('Kho xuất hàng');
    const whSelectTrigger = whLabel.nextElementSibling as HTMLElement;
    fireEvent.click(whSelectTrigger);
    const whOption = await screen.findByText('Tổng Kho Hà Nội');
    fireEvent.click(whOption);

    // 2. Nhập thông tin người nhận và địa chỉ giao hàng
    const nameLabel = screen.getByText('Người nhận hàng');
    const nameInput = nameLabel.nextElementSibling as HTMLInputElement;
    fireEvent.change(nameInput, { target: { value: 'Nguyễn Văn A' } });

    const phoneLabel = screen.getByText('Số điện thoại nhận');
    const phoneInput = phoneLabel.nextElementSibling as HTMLInputElement;
    fireEvent.change(phoneInput, { target: { value: '0901234567' } });

    const addrLabel = screen.getByText('Địa chỉ giao hàng');
    const addrInput = addrLabel.nextElementSibling as HTMLInputElement;
    fireEvent.change(addrInput, { target: { value: 'Số 10 Phố Huế, Hoàn Kiếm, Hà Nội' } });

    // 3. Chọn sản phẩm (dòng 1)
    const spTrigger = screen.getByText('Chọn sản phẩm...');
    fireEvent.click(spTrigger);
    const spOption = await screen.findByText('SKU-DAUTAY-500G - Dâu Tây Đà Lạt Hộp 500g');
    fireEvent.click(spOption);

    // 4. Chọn ĐVT (dòng 1)
    const uomTrigger = screen.getByText('ĐVT');
    fireEvent.click(uomTrigger);
    const uomOption = await screen.findByText('Hộp 500g');
    fireEvent.click(uomOption);

    // 6. Nhập số lượng và đơn giá
    const numberInputs = screen.getAllByRole('spinbutton');
    fireEvent.change(numberInputs[0], { target: { value: '25' } }); // Quantity
    fireEvent.change(numberInputs[1], { target: { value: '50000' } }); // UnitPrice

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(inventoryIssueApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          warehouseId: 1,
          details: expect.arrayContaining([
            expect.objectContaining({
              variantId: 1,
              batchId: 1,
              uoMId: 1,
              quantity: 25,
            }),
          ]),
        })
      );
    });

    await waitFor(
      () => {
        expect(mockNavigate).toHaveBeenCalledWith('/inventory-issues/20');
      },
      { timeout: 2500 }
    );
  });
});
