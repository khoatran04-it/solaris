import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import SupplierProductList from '../../../pages/supplierProduct/SupplierProductList';
import { supplierProductApi } from '../../../api/supplierProductApi';
import { supplierApi } from '../../../api/supplierApi';

// Mock APIs
vi.mock('../../../api/supplierProductApi', () => ({
  supplierProductApi: {
    getAll: vi.fn(),
    toggleActive: vi.fn(),
    delete: vi.fn(),
  },
}));

vi.mock('../../../api/supplierApi', () => ({
  supplierApi: {
    getAllList: vi.fn(),
  },
}));

/**
 * ============================================================================
 * 📦 MODULE 3: SUPPLIER
 * 🧪 COMPONENT TEST: SupplierProductList (Bảng giá & Mặt hàng cung cấp)
 * ============================================================================
 */
describe('Module 03 - SupplierProductList Component', () => {
  const mockSuppliers = [
    { id: 1, code: 'NCC01', name: 'Nông trại Xanh Đà Lạt' },
  ];

  const mockSupplierProducts = [
    {
      id: 1,
      supplierId: 1,
      supplierCode: 'NCC01',
      supplierName: 'Nông trại Xanh Đà Lạt',
      variantId: 10,
      variantCode: 'BO_034',
      variantName: 'Bơ Sáp 034',
      purchaseUoMName: 'Kg',
      supplierSKU: 'SKU-BO-034',
      lastImportPrice: 35000,
      minimumOrderQuantity: 50,
      leadTimeDays: 2,
      isActive: true,
    },
    {
      id: 2,
      supplierId: 1,
      supplierCode: 'NCC01',
      supplierName: 'Nông trại Xanh Đà Lạt',
      variantId: 11,
      variantCode: 'DAU_TAY',
      variantName: 'Dâu Tây New Zealand',
      purchaseUoMName: 'Hộp 500g',
      supplierSKU: 'SKU-DAU-500G',
      lastImportPrice: 120000,
      minimumOrderQuantity: 20,
      leadTimeDays: 1,
      isActive: false,
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (supplierApi.getAllList as any).mockResolvedValue(mockSuppliers);
    (supplierProductApi.getAll as any).mockResolvedValue({
      items: mockSupplierProducts,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // #region TC01: RENDER DANH MỤC MẶT HÀNG NCC
  it('TC01 - Render danh mục mặt hàng NCC với đầy đủ thông tin giá và MOQ', async () => {
    render(
      <MemoryRouter>
        <SupplierProductList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Bơ Sáp 034')).toBeInTheDocument();
      expect(screen.getByText('SKU-BO-034')).toBeInTheDocument();
      expect(screen.getByText('Dâu Tây New Zealand')).toBeInTheDocument();
      expect(screen.getByText('SKU-DAU-500G')).toBeInTheDocument();
    });

    expect(supplierProductApi.getAll).toHaveBeenCalled();
  });
  // #endregion

  // #region TC02: TÌM KIẾM THEO SKU HOẶC TÊN BIẾN THỂ
  it('TC02 - Tìm kiếm mặt hàng theo mã SKU hoặc tên biến thể', async () => {
    render(
      <MemoryRouter>
        <SupplierProductList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Bơ Sáp 034')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText(/Tìm kiếm theo mã SKU, tên sản phẩm, tên NCC.../i);
    fireEvent.change(searchInput, { target: { value: 'Bơ Sáp' } });

    await waitFor(
      () => {
        expect(supplierProductApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({
            search: 'Bơ Sáp',
          })
        );
      },
      { timeout: 1000 }
    );
  });
  // #endregion

  // #region TC03: LỌC THEO NHÀ CUNG CẤP
  it('TC03 - Lọc danh sách theo nhà cung cấp cụ thể', async () => {
    render(
      <MemoryRouter>
        <SupplierProductList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Bơ Sáp 034')).toBeInTheDocument();
    });

    const filterButtons = screen.getAllByRole('button');
    const supplierFilterBtn = filterButtons.find((btn) => btn.textContent?.includes('Nhà cung cấp'));
    if (supplierFilterBtn) {
      fireEvent.click(supplierFilterBtn);
    }

    await waitFor(() => {
      expect(supplierApi.getAllList).toHaveBeenCalled();
    });
  });
  // #endregion

  // #region TC04: THAY ĐỔI TRẠNG THÁI HOẠT ĐỘNG
  it('TC04 - Đổi trạng thái hoạt động của mặt hàng cung ứng', async () => {
    (supplierProductApi.toggleActive as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <SupplierProductList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Bơ Sáp 034')).toBeInTheDocument();
    });

    const activeToggle = screen.getAllByRole('button', { name: /Đang cung ứng/i })[0];
    fireEvent.click(activeToggle);

    await waitFor(() => {
      expect(supplierProductApi.toggleActive).toHaveBeenCalledWith(1);
    });
  });
  // #endregion

  // #region TC05: XÓA CẤU HÌNH MẶT HÀNG CÓ XÁC NHẬN MODAL
  it('TC05 - Mở modal xác nhận xóa và gọi API delete cấu hình mặt hàng', async () => {
    (supplierProductApi.delete as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <SupplierProductList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Bơ Sáp 034')).toBeInTheDocument();
    });

    // Click nút xóa
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
      expect(supplierProductApi.delete).toHaveBeenCalledWith(1);
    });
  });
  // #endregion
});
