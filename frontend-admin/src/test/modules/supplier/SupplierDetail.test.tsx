import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import SupplierDetail from '../../../pages/supplier/SupplierDetail';
import { supplierApi } from '../../../api/supplierApi';
import { supplierAddressApi } from '../../../api/supplierAddressApi';
import { supplierProductApi } from '../../../api/supplierProductApi';

// Mock APIs
vi.mock('../../../api/supplierApi', () => ({
  supplierApi: {
    getById: vi.fn(),
  },
}));

vi.mock('../../../api/supplierAddressApi', () => ({
  supplierAddressApi: {
    getBySupplierId: vi.fn(),
    setDefault: vi.fn(),
    delete: vi.fn(),
  },
}));

vi.mock('../../../api/supplierProductApi', () => ({
  supplierProductApi: {
    getBySupplierId: vi.fn(),
    delete: vi.fn(),
    toggleActive: vi.fn(),
  },
}));

/**
 * ============================================================================
 * 📦 MODULE 3: SUPPLIER
 * 🧪 COMPONENT TEST: SupplierDetail (Chi tiết Hồ sơ Nhà cung cấp đa Tab)
 * ============================================================================
 */
describe('Module 03 - SupplierDetail Component', () => {
  const mockSupplier = {
    id: 1,
    code: 'NCC01',
    name: 'Nông Trại Xanh Đà Lạt',
    phone: '0912345678',
    email: 'dalat@gmail.com',
    taxCode: '0312345678',
    bankAccount: '123456789',
    bankName: 'Vietcombank Chi Nhánh Đà Lạt',
    supplierTypeId: 1,
    supplierTypeName: 'Nhà vườn',
    isActive: true,
    createdAt: '2026-08-20T10:00:00Z',
    addresses: [
      {
        id: 10,
        supplierId: 1,
        contactName: 'Anh Ba Vườn',
        contactPhone: '0912345678',
        streetAddress: '123 Thung Lũng Tình Yêu',
        ward: 'Phường 8',
        district: 'TP. Đà Lạt',
        province: 'Lâm Đồng',
        fullAddress: '123 Thung Lũng Tình Yêu, Phường 8, TP. Đà Lạt, Lâm Đồng',
        isDefault: true,
        createdAt: '2026-08-20T10:00:00Z',
      },
      {
        id: 11,
        supplierId: 1,
        contactName: 'Kho Thu Mua Phụ',
        contactPhone: '0988776655',
        streetAddress: '456 Đèo Prenn',
        ward: 'Phường 3',
        district: 'TP. Đà Lạt',
        province: 'Lâm Đồng',
        fullAddress: '456 Đèo Prenn, Phường 3, TP. Đà Lạt, Lâm Đồng',
        isDefault: false,
        createdAt: '2026-08-21T10:00:00Z',
      },
    ],
  };

  const mockProducts = [
    {
      id: 100,
      supplierId: 1,
      variantId: 1,
      variantCode: 'BO_034',
      variantName: 'Bơ Sáp 034 Loại 1',
      purchaseUoMName: 'Kg',
      supplierSKU: 'SKU-BO-DALAT',
      lastImportPrice: 45000,
      minimumOrderQuantity: 50,
      leadTimeDays: 2,
      isActive: true,
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (supplierApi.getById as any).mockResolvedValue(mockSupplier);
    (supplierAddressApi.getBySupplierId as any).mockResolvedValue(mockSupplier.addresses);
    (supplierProductApi.getBySupplierId as any).mockResolvedValue(mockProducts);
  });

  // #region TC01: RENDER CHI TIẾT NHÀ CUNG CẤP
  it('TC01 - Render thông tin chi tiết nhà cung cấp trong Tab Thông Tin Cơ Bản', async () => {
    render(
      <MemoryRouter initialEntries={['/suppliers/1']}>
        <Routes>
          <Route path="/suppliers/:id" element={<SupplierDetail />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getAllByText('Nông Trại Xanh Đà Lạt').length).toBeGreaterThan(0);
      expect(screen.getAllByText('NCC01').length).toBeGreaterThan(0);
      expect(screen.getByText('0912345678')).toBeInTheDocument();
      expect(screen.getByText('Vietcombank Chi Nhánh Đà Lạt')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC02: ĐẶC THÙ - CHUYỂN TAB ĐA NĂNG
  it('TC02 - Chuyển đổi qua lại giữa các Tab (Chi tiết, Địa chỉ kho, Mặt hàng cung cấp)', async () => {
    render(
      <MemoryRouter initialEntries={['/suppliers/1']}>
        <Routes>
          <Route path="/suppliers/:id" element={<SupplierDetail />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getAllByText('Nông Trại Xanh Đà Lạt').length).toBeGreaterThan(0);
    });

    // Chuyển sang Tab "DANH SÁCH ĐỊA CHỈ KHO"
    const addressTabBtn = screen.getByRole('button', { name: /DANH SÁCH ĐỊA CHỈ KHO/i });
    fireEvent.click(addressTabBtn);

    await waitFor(() => {
      expect(screen.getByText('Anh Ba Vườn')).toBeInTheDocument();
      expect(screen.getByText('Kho Thu Mua Phụ')).toBeInTheDocument();
    });

    // Chuyển sang Tab "SẢN PHẨM & BẢNG GIÁ"
    const productTabBtn = screen.getByRole('button', { name: /SẢN PHẨM & BẢNG GIÁ/i });
    fireEvent.click(productTabBtn);

    await waitFor(() => {
      expect(screen.getByText('Bơ Sáp 034 Loại 1')).toBeInTheDocument();
      expect(screen.getByText('SKU-BO-DALAT')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC03: TAB ĐỊA CHỈ KHO - HIỂN THỊ ICON MẶC ĐỊNH
  it('TC03 - Hiển thị đúng biểu tượng kho mặc định cho địa chỉ mặc định của NCC', async () => {
    render(
      <MemoryRouter initialEntries={['/suppliers/1']}>
        <Routes>
          <Route path="/suppliers/:id" element={<SupplierDetail />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getAllByText('Nông Trại Xanh Đà Lạt').length).toBeGreaterThan(0);
    });

    // Chuyển sang Tab "DANH SÁCH ĐỊA CHỈ KHO"
    fireEvent.click(screen.getByRole('button', { name: /DANH SÁCH ĐỊA CHỈ KHO/i }));

    await waitFor(() => {
      expect(screen.getByTitle('Đang là địa chỉ kho mặc định')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC04: ĐẶC THÙ - THAO TÁC ĐẶT LÀM MẶC ĐỊNH
  it('TC04 - Đặt một địa chỉ phụ làm địa chỉ mặc định', async () => {
    (supplierAddressApi.setDefault as any).mockResolvedValue({});

    render(
      <MemoryRouter initialEntries={['/suppliers/1']}>
        <Routes>
          <Route path="/suppliers/:id" element={<SupplierDetail />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getAllByText('Nông Trại Xanh Đà Lạt').length).toBeGreaterThan(0);
    });

    fireEvent.click(screen.getByRole('button', { name: /DANH SÁCH ĐỊA CHỈ KHO/i }));

    await waitFor(() => {
      expect(screen.getByTitle('Đặt làm địa chỉ kho mặc định')).toBeInTheDocument();
    });

    // Click nút "Đặt làm địa chỉ kho mặc định" trên địa chỉ phụ
    const setDefaultBtn = screen.getByTitle('Đặt làm địa chỉ kho mặc định');
    fireEvent.click(setDefaultBtn);

    await waitFor(() => {
      expect(supplierAddressApi.setDefault).toHaveBeenCalledWith(11, 1);
    });
  });
  // #endregion

  // #region TC05: TAB MẶT HÀNG CUNG CẤP - HIỂN THỊ BẢNG GIÁ & MOQ
  it('TC05 - Hiển thị thông tin giá nhập và MOQ trong Tab Mặt hàng cung cấp', async () => {
    render(
      <MemoryRouter initialEntries={['/suppliers/1']}>
        <Routes>
          <Route path="/suppliers/:id" element={<SupplierDetail />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getAllByText('Nông Trại Xanh Đà Lạt').length).toBeGreaterThan(0);
    });

    fireEvent.click(screen.getByRole('button', { name: /SẢN PHẨM & BẢNG GIÁ/i }));

    await waitFor(() => {
      expect(screen.getByText('Bơ Sáp 034 Loại 1')).toBeInTheDocument();
      expect(screen.getByText('45.000 ₫')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC06: XÓA ĐỊA CHỈ KHO CÓ XÁC NHẬN MODAL
  it('TC06 - Mở modal xác nhận xóa khi xóa một địa chỉ kho', async () => {
    (supplierAddressApi.delete as any).mockResolvedValue({});

    render(
      <MemoryRouter initialEntries={['/suppliers/1']}>
        <Routes>
          <Route path="/suppliers/:id" element={<SupplierDetail />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getAllByText('Nông Trại Xanh Đà Lạt').length).toBeGreaterThan(0);
    });

    fireEvent.click(screen.getByRole('button', { name: /DANH SÁCH ĐỊA CHỈ KHO/i }));

    await waitFor(() => {
      expect(screen.getByText('Kho Thu Mua Phụ')).toBeInTheDocument();
    });

    // Click nút xóa của địa chỉ
    const deleteButtons = screen.getAllByTitle('Xóa');
    fireEvent.click(deleteButtons[0]);

    // Modal xuất hiện
    await waitFor(() => {
      expect(screen.getByText(/Bạn có chắc chắn muốn xóa/i)).toBeInTheDocument();
    });

    // Xác nhận xóa
    const confirmBtn = screen.getByRole('button', { name: /Xóa ngay/i });
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(supplierAddressApi.delete).toHaveBeenCalled();
    });
  });
  // #endregion
});
