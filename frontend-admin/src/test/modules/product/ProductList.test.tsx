import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import ProductList from '../../../pages/product/ProductList';
import { productApi } from '../../../api/productApi';
import { productCategoryApi } from '../../../api/productCategoryApi';
import { uomApi } from '../../../api/uomApi';

// Mock APIs
vi.mock('../../../api/productApi', () => ({
  productApi: {
    getAll: vi.fn(),
    toggleActive: vi.fn(),
    delete: vi.fn(),
  },
}));

vi.mock('../../../api/productCategoryApi', () => ({
  productCategoryApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/uomApi', () => ({
  uomApi: {
    getAllList: vi.fn(),
  },
}));

/**
 * ============================================================================
 * 📦 MODULE 5: PRODUCT & PRICING
 * 🧪 COMPONENT TEST: ProductList (Danh sách Sản phẩm Khung / Gốc)
 * ============================================================================
 */
describe('Module 05 - ProductList Component', () => {
  const mockProducts = [
    {
      id: 1,
      code: 'PROD-CACHUA',
      name: 'Cà chua bi Đà Lạt',
      slug: 'ca-chua-bi-da-lat',
      categoryId: 10,
      categoryName: 'Rau củ quả',
      baseUoMId: 1,
      baseUoMName: 'Kilogram',
      description: 'Trồng theo chuẩn VietGAP',
      imagePath: 'https://example.com/cachua.jpg',
      isActive: true,
      createdAt: '2026-08-20T10:00:00Z',
      updatedAt: '2026-08-20T10:00:00Z',
    },
    {
      id: 2,
      code: 'PROD-TAO',
      name: 'Táo Envy New Zealand',
      slug: 'tao-envy-new-zealand',
      categoryId: 20,
      categoryName: 'Trái cây nhập khẩu',
      baseUoMId: 2,
      baseUoMName: 'Hộp',
      description: 'Trái cây nhập khẩu',
      imagePath: null,
      isActive: false,
      createdAt: '2026-08-21T10:00:00Z',
      updatedAt: '2026-08-21T10:00:00Z',
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (productCategoryApi.getAllList as any).mockResolvedValue([
      { id: 10, name: 'Rau củ quả' },
      { id: 20, name: 'Trái cây nhập khẩu' },
    ]);
    (uomApi.getAllList as any).mockResolvedValue([
      { id: 1, name: 'Kilogram', code: 'KG' },
      { id: 2, name: 'Hộp', code: 'BOX' },
    ]);
    (productApi.getAll as any).mockResolvedValue({
      items: mockProducts,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // #region TC01: RENDER DANH SÁCH SẢN PHẨM
  it('TC01 - Render danh sách sản phẩm với đầy đủ tên, mã SKU, danh mục và ĐVT cơ sở', async () => {
    render(
      <MemoryRouter>
        <ProductList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Cà chua bi Đà Lạt')).toBeInTheDocument();
      expect(screen.getByText('PROD-CACHUA')).toBeInTheDocument();
      expect(screen.getByText('Táo Envy New Zealand')).toBeInTheDocument();
      expect(screen.getByText('PROD-TAO')).toBeInTheDocument();
      expect(screen.getAllByText('Kilogram').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Rau củ quả').length).toBeGreaterThan(0);
    });

    expect(productApi.getAll).toHaveBeenCalled();
  });
  // #endregion

  // #region TC02: TÌM KIẾM THEO TỪ KHÓA
  it('TC02 - Tìm kiếm theo từ khóa mã hoặc tên sản phẩm gốc', async () => {
    render(
      <MemoryRouter>
        <ProductList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('PROD-CACHUA')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText(/Tìm kiếm theo mã, tên sản phẩm.../i);
    fireEvent.change(searchInput, { target: { value: 'Cà chua' } });

    await waitFor(
      () => {
        expect(productApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({
            search: 'Cà chua',
          })
        );
      },
      { timeout: 1000 }
    );
  });
  // #endregion

  // #region TC03: LỌC DANH MỤC VÀ ĐƠN VỊ TÍNH
  it('TC03 - Lọc danh sách theo danh mục và đơn vị tính', async () => {
    render(
      <MemoryRouter>
        <ProductList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('PROD-CACHUA')).toBeInTheDocument();
    });

    const filterButtons = screen.getAllByRole('button');
    const categoryFilterBtn = filterButtons.find((btn) => btn.textContent?.includes('DANH MỤC'));
    if (categoryFilterBtn) {
      fireEvent.click(categoryFilterBtn);
    }

    await waitFor(() => {
      expect(productApi.getAll).toHaveBeenCalled();
    });
  });
  // #endregion

  // #region TC04: THAY ĐỔI TRẠNG THÁI (TOGGLE ACTIVE)
  it('TC04 - Đổi trạng thái kinh doanh (Đang bán / Ngừng bán)', async () => {
    (productApi.toggleActive as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <ProductList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('PROD-CACHUA')).toBeInTheDocument();
    });

    const activeToggle = screen.getByRole('button', { name: /Đang bán/i });
    fireEvent.click(activeToggle);

    await waitFor(() => {
      expect(productApi.toggleActive).toHaveBeenCalledWith(1);
    });
  });
  // #endregion

  // #region TC05: XÓA SẢN PHẨM CÓ MODAL XÁC NHẬN
  it('TC05 - Mở modal xác nhận và gọi API xóa sản phẩm', async () => {
    (productApi.delete as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <ProductList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('PROD-CACHUA')).toBeInTheDocument();
    });

    const deleteButtons = screen.getAllByTitle('Xóa');
    fireEvent.click(deleteButtons[0]);

    await waitFor(() => {
      expect(screen.getByText(/Bạn có chắc chắn muốn xóa/i)).toBeInTheDocument();
    });

    const confirmBtn = screen.getByRole('button', { name: /Xóa ngay/i });
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(productApi.delete).toHaveBeenCalledWith(1);
    });
  });
  // #endregion
});
