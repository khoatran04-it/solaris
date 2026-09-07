import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import PurchaseOrderForm from '../../../pages/purchaseOrder/PurchaseOrderForm';
import { purchaseOrderApi } from '../../../api/purchaseOrderApi';
import { supplierApi } from '../../../api/supplierApi';
import { supplierProductApi } from '../../../api/supplierProductApi';
import { productVariantApi } from '../../../api/productVariantApi';
import { uomApi } from '../../../api/uomApi';
import { PurchaseOrderStatus } from '../../../types/purchaseOrder';

// Mock APIs
vi.mock('../../../api/purchaseOrderApi', () => ({
  purchaseOrderApi: {
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
  },
}));

vi.mock('../../../api/supplierApi', () => ({
  supplierApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/supplierProductApi', () => ({
  supplierProductApi: {
    getBySupplierId: vi.fn(),
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

vi.mock('../../../api/uomConversionApi', () => ({
  uomConversionApi: {
    getValidUoMs: vi.fn(),
  },
}));

import { uomConversionApi } from '../../../api/uomConversionApi';

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
 * MODULE 09: PURCHASING & PURCHASE ORDER MANAGEMENT
 * COMPONENT TEST: PurchaseOrderForm (Biểu Mẫu Tạo Mới & Chỉnh Sửa PO)
 * ============================================================================
 */
describe('Module 09 - PurchaseOrderForm Component', () => {
  const mockSuppliers = [{ id: 1, name: 'Nông Trại Đà Lạt GAP', code: 'SUP-DALAT' }];

  const mockVariants = [{ id: 10, name: 'Dâu Tây Hộp 500g', code: 'SKU-DAUTAY' }];

  const mockUoms = [
    { id: 100, name: 'Hộp' },
    { id: 101, name: 'Kilogram' },
  ];

  const mockSupplierProducts = [
    {
      id: 1,
      supplierId: 1,
      variantId: 10,
      variantName: 'Dâu Tây Hộp 500g',
      variantSKU: 'SKU-DAUTAY',
      purchaseUoMId: 100,
      purchaseUoMName: 'Hộp',
      lastImportPrice: 45000,
      minimumOrderQuantity: 10,
      leadTimeDays: 2,
      isPreferred: true,
      isActive: true,
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (supplierApi.getAllList as any).mockResolvedValue(mockSuppliers);
    (productVariantApi.getAllList as any).mockResolvedValue(mockVariants);
    (uomApi.getAllList as any).mockResolvedValue(mockUoms);
    (supplierProductApi.getBySupplierId as any).mockResolvedValue(mockSupplierProducts);
    (uomConversionApi.getValidUoMs as any).mockResolvedValue([
      {
        uoMId: 100,
        uoMName: 'Hộp',
        uoMCode: 'BOX',
        conversionFactorToBase: 1,
        isBaseUoM: true,
        description: 'Đơn vị cơ sở',
      },
      {
        uoMId: 102,
        uoMName: 'Thùng',
        uoMCode: 'CTN',
        conversionFactorToBase: 12,
        isBaseUoM: false,
        description: '1 Thùng = 12 Hộp',
      },
    ]);
  });

  // #region TC01: RENDER FORM TẠO MỚI
  it('TC01 - Render form tạo mới đơn mua hàng với các trường thông tin chung và bảng mặt hàng', async () => {
    render(
      <MemoryRouter initialEntries={['/purchase-orders/create']}>
        <Routes>
          <Route path="/purchase-orders/create" element={<PurchaseOrderForm />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Tạo Đơn Mua Hàng Mới')).toBeInTheDocument();
    expect(screen.getByText('1. Thông Tin Chung')).toBeInTheDocument();
    expect(screen.getByText('2. Chi Tiết Đặt Hàng')).toBeInTheDocument();

    await waitFor(() => {
      expect(
        screen.getByText('Chưa có mặt hàng nào. Vui lòng bấm "Thêm Mặt Hàng".')
      ).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC02: THÊM DÒNG MẶT HÀNG VÀ TÍNH TỔNG TIỀN
  it('TC02 - Thêm dòng mặt hàng mới, nhập số lượng, đơn giá và kiểm tra tính toán thành tiền', async () => {
    render(
      <MemoryRouter initialEntries={['/purchase-orders/create']}>
        <Routes>
          <Route path="/purchase-orders/create" element={<PurchaseOrderForm />} />
        </Routes>
      </MemoryRouter>
    );

    const addRowBtn = await screen.findByRole('button', { name: /THÊM MẶT HÀNG/i });
    fireEvent.click(addRowBtn);

    await waitFor(() => {
      const numberInputs = screen.getAllByRole('spinbutton');
      expect(numberInputs.length).toBeGreaterThanOrEqual(2);
    });

    const inputs = screen.getAllByRole('spinbutton');
    // inputs[0]: orderQuantity, inputs[1]: unitPrice
    fireEvent.change(inputs[0], { target: { value: '20' } });
    fireEvent.change(inputs[1], { target: { value: '50000' } });

    await waitFor(() => {
      expect(screen.getByText(/Tổng Tiền Đơn Hàng:/i)).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC03: VALIDATION BẮT LỖI KHI BỎ TRỐNG TRƯỜNG BẮT BUỘC
  it('TC03 - Báo lỗi validation khi submit form mà chưa chọn nhà cung cấp hoặc chưa có mặt hàng', async () => {
    render(
      <MemoryRouter initialEntries={['/purchase-orders/create']}>
        <Routes>
          <Route path="/purchase-orders/create" element={<PurchaseOrderForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /TẠO MỚI/i })).toBeInTheDocument();
    });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Vui lòng chọn nhà cung cấp')).toBeInTheDocument();
      expect(screen.getByText('Cần ít nhất 1 mặt hàng trong đơn!')).toBeInTheDocument();
    });

    expect(purchaseOrderApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC04: CHỈNH SỬA ĐƠN HÀNG Ở TRẠNG THÁI DRAFT
  it('TC04 - Load dữ liệu đơn mua hàng khi ở chế độ chỉnh sửa (Draft mode)', async () => {
    (purchaseOrderApi.getById as any).mockResolvedValue({
      id: 5,
      orderCode: 'PO-20260830-005',
      orderDate: '2026-08-30T00:00:00Z',
      expectedDeliveryDate: '2026-09-02T00:00:00Z',
      status: PurchaseOrderStatus.Draft,
      supplierId: 1,
      supplierName: 'Nông Trại Đà Lạt GAP',
      note: 'Giao hàng gấp',
      details: [
        {
          id: 1,
          variantId: 10,
          uoMId: 100,
          orderQuantity: 30,
          unitPrice: 40000,
          totalPrice: 1200000,
        },
      ],
    });

    render(
      <MemoryRouter initialEntries={['/purchase-orders/edit/5']}>
        <Routes>
          <Route path="/purchase-orders/edit/:id" element={<PurchaseOrderForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Chỉnh Sửa Đơn Mua Hàng (Nháp)')).toBeInTheDocument();
      expect(screen.getByDisplayValue('Giao hàng gấp')).toBeInTheDocument();
      expect(screen.getByDisplayValue('30')).toBeInTheDocument();
      expect(screen.getByDisplayValue('40000')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC05: CẢNH BÁO VÀ CHUYỂN HƯỚNG KHI SỬA ĐƠN KHÔNG PHẢI DRAFT
  it('TC05 - Hiển thị cảnh báo khi cố gắng chỉnh sửa đơn hàng đã được Duyệt (Approved)', async () => {
    (purchaseOrderApi.getById as any).mockResolvedValue({
      id: 6,
      orderCode: 'PO-APPROVED-006',
      status: PurchaseOrderStatus.Approved,
      supplierId: 1,
      details: [],
    });

    render(
      <MemoryRouter initialEntries={['/purchase-orders/edit/6']}>
        <Routes>
          <Route path="/purchase-orders/edit/:id" element={<PurchaseOrderForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(
        screen.getByText(/Chỉ được phép chỉnh sửa đơn hàng đang ở trạng thái Nháp!/i)
      ).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC06: TỰ ĐỘNG QUY ĐỔI ĐƠN GIÁ THEO ĐƠN VỊ TÍNH QUY ĐỔI (THÙNG / HỘP)
  it('TC06 - Tự động quy đổi đơn giá và tính lại thành tiền khi đổi ĐVT sang ĐVT quy đổi lớn hơn', async () => {
    render(
      <MemoryRouter initialEntries={['/purchase-orders/create']}>
        <Routes>
          <Route path="/purchase-orders/create" element={<PurchaseOrderForm />} />
        </Routes>
      </MemoryRouter>
    );

    // 1. Chọn NCC = 1
    const nccSelect = screen.getByText('-- Chọn Nhà cung cấp --');
    fireEvent.click(nccSelect);
    const nccOption = await screen.findByText(/Nông Trại Đà Lạt GAP/i);
    fireEvent.click(nccOption);

    // Chờ tải xong bảng giá NCC
    await screen.findByText(/1 mặt hàng đã liên kết/i);

    // 2. Bấm thêm mặt hàng
    const addRowBtn = await screen.findByRole('button', { name: /THÊM MẶT HÀNG/i });
    fireEvent.click(addRowBtn);

    // 3. Chọn sản phẩm = 10 (Dâu Tây Hộp 500g)
    const productSelect = await screen.findByText('Chọn sản phẩm...');
    fireEvent.click(productSelect);
    const prodOption = await screen.findByText(/Dâu Tây Hộp 500g/i);
    fireEvent.click(prodOption);

    // Đơn giá mặc định của Hộp từ NCC: 45000, MOQ = 10
    await waitFor(() => {
      expect(screen.getByDisplayValue('45000')).toBeInTheDocument();
    });

    // 4. Đổi ĐVT sang "Thùng" (hệ số = 12)
    const uomSelect = screen.getByText('Hộp (Đơn vị cơ sở)');
    fireEvent.click(uomSelect);
    const thungOption = await screen.findByText(/Thùng \(1 Thùng = 12 Hộp\)/i);
    fireEvent.click(thungOption);

    // Đơn giá phải tự động nhân 12: 45.000 * 12 = 540.000
    await waitFor(() => {
      expect(screen.getByDisplayValue('540000')).toBeInTheDocument();
    });

    const qtyInput = screen.getAllByRole('spinbutton')[0] as HTMLInputElement;
    expect(qtyInput.value).toBe('10');

    const expectedTotalStr = (5400000).toLocaleString('vi-VN');
    await waitFor(() => {
      expect(screen.getAllByText(new RegExp(expectedTotalStr)).length).toBeGreaterThanOrEqual(1);
    });
  });
  // #endregion
});
