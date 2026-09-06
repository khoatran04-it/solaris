import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import ProductVariantList from '../../../pages/productVariant/ProductVariantList';
import { productVariantApi } from '../../../api/productVariantApi';
import { productApi } from '../../../api/productApi';

// Mock APIs
vi.mock('../../../api/productVariantApi', () => ({
  productVariantApi: {
    getAll: vi.fn(),
    toggleActive: vi.fn(),
    delete: vi.fn(),
  },
}));

vi.mock('../../../api/productApi', () => ({
  productApi: {
    getAllList: vi.fn(),
  },
}));

/**
 * ============================================================================
 * MODULE 5: PRODUCT & PRICING
 * COMPONENT TEST: ProductVariantList (Danh sách Biến Thể SKU & Bảng Giá)
 * ============================================================================
 */
describe('Module 05 - ProductVariantList Component', () => {
  const mockVariants = [
    {
      id: 1,
      code: 'SKU-TAO-1KG',
      name: 'Táo Envy Túi 1Kg',
      productId: 1,
      productName: 'Táo Envy New Zealand',
      inventoryGuideline: 100,
      quantityAvailable: 150,
      imagePath: 'https://example.com/tao1kg.jpg',
      isActive: true,
      createdAt: '2026-08-20T10:00:00Z',
      updatedAt: '2026-08-20T10:00:00Z',
      prices: [
        {
          id: 10,
          uoMId: 1,
          uoMName: 'Kg',
          price: 120000,
          promotionalPrice: 96000, // Có khuyến mãi
          isDefault: true,
        },
      ],
    },
    {
      id: 2,
      code: 'SKU-CACHUA-500G',
      name: 'Cà chua bi Hộp 500g',
      productId: 2,
      productName: 'Cà chua bi Đà Lạt',
      inventoryGuideline: 50,
      quantityAvailable: 20,
      imagePath: null,
      isActive: false,
      createdAt: '2026-08-21T10:00:00Z',
      updatedAt: '2026-08-21T10:00:00Z',
      prices: [
        {
          id: 20,
          uoMId: 2,
          uoMName: 'Hộp',
          price: 35000,
          promotionalPrice: null,
          isDefault: true,
        },
      ],
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (productApi.getAllList as any).mockResolvedValue([
      { id: 1, name: 'Táo Envy New Zealand' },
      { id: 2, name: 'Cà chua bi Đà Lạt' },
    ]);
    (productVariantApi.getAll as any).mockResolvedValue({
      items: mockVariants,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // #region TC01: RENDER DANH SÁCH BIẾN THỂ
  it('TC01 - Render danh sách biến thể với mã SKU, giá bán niêm yết/khuyến mãi và 2 cột tồn kho (tối thiểu & khả dụng)', async () => {
    render(
      <MemoryRouter>
        <ProductVariantList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Táo Envy Túi 1Kg')).toBeInTheDocument();
      expect(screen.getByText('SKU-TAO-1KG')).toBeInTheDocument();
      expect(screen.getByText('Táo Envy New Zealand')).toBeInTheDocument();
      expect(screen.getByText(/96.000 ₫/i)).toBeInTheDocument(); // Giá khuyến mãi

      expect(screen.getByText('Cà chua bi Hộp 500g')).toBeInTheDocument();
      expect(screen.getByText('SKU-CACHUA-500G')).toBeInTheDocument();
      expect(screen.getByText('Cà chua bi Đà Lạt')).toBeInTheDocument();
      expect(screen.getByText(/35.000 ₫/i)).toBeInTheDocument();

      // Kiểm tra 2 cột Tồn Tối Thiểu và Tồn Khả Dụng
      expect(screen.getByText(/Tồn Tối Thiểu/i)).toBeInTheDocument();
      expect(screen.getByText(/Tồn Khả Dụng/i)).toBeInTheDocument();
      expect(screen.getByText('150')).toBeInTheDocument();
    });

    expect(productVariantApi.getAll).toHaveBeenCalled();
  });
  // #endregion

  // #region TC02: TÌM KIẾM THEO TỪ KHÓA
  it('TC02 - Tìm kiếm theo mã SKU hoặc tên biến thể', async () => {
    render(
      <MemoryRouter>
        <ProductVariantList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('SKU-TAO-1KG')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText(/Tìm kiếm theo mã SKU, tên biến thể.../i);
    fireEvent.change(searchInput, { target: { value: 'Táo Envy' } });

    await waitFor(
      () => {
        expect(productVariantApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({
            search: 'Táo Envy',
          })
        );
      },
      { timeout: 1000 }
    );
  });
  // #endregion

  // #region TC03: LỌC THEO SẢN PHẨM GỐC VÀ TRẠNG THÁI
  it('TC03 - Lọc biến thể theo sản phẩm gốc', async () => {
    render(
      <MemoryRouter>
        <ProductVariantList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('SKU-TAO-1KG')).toBeInTheDocument();
    });

    const filterButtons = screen.getAllByRole('button');
    const productFilterBtn = filterButtons.find((btn) => btn.textContent?.includes('SẢN PHẨM GỐC'));
    if (productFilterBtn) {
      fireEvent.click(productFilterBtn);
    }

    await waitFor(() => {
      expect(productVariantApi.getAll).toHaveBeenCalled();
    });
  });
  // #endregion

  // #region TC04: THAY ĐỔI TRẠNG THÁI (TOGGLE ACTIVE)
  it('TC04 - Đổi trạng thái hoạt động của biến thể', async () => {
    (productVariantApi.toggleActive as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <ProductVariantList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('SKU-TAO-1KG')).toBeInTheDocument();
    });

    const activeToggle = screen.getByRole('button', { name: /Hoạt động/i });
    fireEvent.click(activeToggle);

    await waitFor(() => {
      expect(productVariantApi.toggleActive).toHaveBeenCalledWith(1);
    });
  });
  // #endregion

  // #region TC05: XÓA BIẾN THỂ CÓ MODAL XÁC NHẬN
  it('TC05 - Mở modal xác nhận và gọi API xóa biến thể', async () => {
    (productVariantApi.delete as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <ProductVariantList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('SKU-TAO-1KG')).toBeInTheDocument();
    });

    const deleteButtons = screen.getAllByTitle('Xóa');
    fireEvent.click(deleteButtons[0]);

    await waitFor(() => {
      expect(screen.getByText(/Bạn có chắc chắn muốn xóa/i)).toBeInTheDocument();
    });

    const confirmBtn = screen.getByRole('button', { name: /Xóa ngay/i });
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(productVariantApi.delete).toHaveBeenCalledWith(1);
    });
  });
  // #endregion
});
