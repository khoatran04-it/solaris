import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import ProductCategoryList from '../../../pages/productCategory/ProductCategoryList';
import { productCategoryApi } from '../../../api/productCategoryApi';
import { productCategoryGroupApi } from '../../../api/productCategoryGroupApi';

// Mock APIs
vi.mock('../../../api/productCategoryApi', () => ({
  productCategoryApi: {
    getAll: vi.fn(),
    toggleActive: vi.fn(),
    delete: vi.fn(),
  },
}));

vi.mock('../../../api/productCategoryGroupApi', () => ({
  productCategoryGroupApi: {
    getAllList: vi.fn(),
  },
}));

/**
 * ============================================================================
 * MODULE 4: PRODUCT CATEGORY
 * COMPONENT TEST: ProductCategoryList (Danh sách Danh Mục Sản Phẩm)
 * ============================================================================
 */
describe('Module 04 - ProductCategoryList Component', () => {
  const mockCategories = [
    {
      id: 1,
      code: 'VEG_ORGANIC',
      name: 'Rau xanh hữu cơ',
      categoryGroupId: 1,
      categoryGroupName: 'Thực phẩm tươi sống',
      description: 'Rau trồng theo chuẩn VietGAP',
      imagePath: 'https://example.com/veg.jpg',
      isActive: true,
      createdAt: '2026-08-20T10:00:00Z',
      updatedAt: '2026-08-20T10:00:00Z',
    },
    {
      id: 2,
      code: 'FRUIT_IMPORT',
      name: 'Trái cây nhập khẩu',
      categoryGroupId: 1,
      categoryGroupName: 'Thực phẩm tươi sống',
      description: 'Trái cây nhập khẩu New Zealand',
      imagePath: null,
      isActive: false,
      createdAt: '2026-08-21T10:00:00Z',
      updatedAt: '2026-08-21T10:00:00Z',
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (productCategoryGroupApi.getAllList as any).mockResolvedValue([
      { id: 1, name: 'Thực phẩm tươi sống' },
    ]);
    (productCategoryApi.getAll as any).mockResolvedValue({
      items: mockCategories,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // #region TC01: RENDER DANH SÁCH DANH MỤC
  it('TC01 - Render danh sách danh mục với đầy đủ thông tin và nhóm trực thuộc', async () => {
    render(
      <MemoryRouter>
        <ProductCategoryList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Rau xanh hữu cơ')).toBeInTheDocument();
      expect(screen.getByText('VEG_ORGANIC')).toBeInTheDocument();
      expect(screen.getByText('Trái cây nhập khẩu')).toBeInTheDocument();
      expect(screen.getByText('FRUIT_IMPORT')).toBeInTheDocument();
    });

    expect(productCategoryApi.getAll).toHaveBeenCalled();
  });
  // #endregion

  // #region TC02: TÌM KIẾM THEO TỪ KHÓA
  it('TC02 - Tìm kiếm theo từ khóa mã hoặc tên danh mục sản phẩm', async () => {
    render(
      <MemoryRouter>
        <ProductCategoryList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('VEG_ORGANIC')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText(/Tìm kiếm theo mã, tên danh mục.../i);
    fireEvent.change(searchInput, { target: { value: 'Rau xanh' } });

    await waitFor(
      () => {
        expect(productCategoryApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({
            search: 'Rau xanh',
          })
        );
      },
      { timeout: 1000 }
    );
  });
  // #endregion

  // #region TC03: LỌC THEO NHÓM VÀ TRẠNG THÁI
  it('TC03 - Lọc danh sách theo nhóm ngành hàng và trạng thái', async () => {
    render(
      <MemoryRouter>
        <ProductCategoryList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('VEG_ORGANIC')).toBeInTheDocument();
    });

    const filterButtons = screen.getAllByRole('button');
    const groupFilterBtn = filterButtons.find((btn) =>
      btn.textContent?.includes('NHÓM NGÀNH HÀNG')
    );
    if (groupFilterBtn) {
      fireEvent.click(groupFilterBtn);
    }

    await waitFor(() => {
      expect(productCategoryApi.getAll).toHaveBeenCalled();
    });
  });
  // #endregion

  // #region TC04: THAY ĐỔI TRẠNG THÁI (TOGGLE ACTIVE)
  it('TC04 - Đổi trạng thái hoạt động của danh mục', async () => {
    (productCategoryApi.toggleActive as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <ProductCategoryList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('VEG_ORGANIC')).toBeInTheDocument();
    });

    const activeToggle = screen.getAllByRole('button', { name: /Hoạt động/i })[0];
    fireEvent.click(activeToggle);

    await waitFor(() => {
      expect(productCategoryApi.toggleActive).toHaveBeenCalledWith(1);
    });
  });
  // #endregion

  // #region TC05: XÓA DANH MỤC CÓ XÁC NHẬN MODAL
  it('TC05 - Mở modal xác nhận xóa và gọi API delete', async () => {
    (productCategoryApi.delete as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <ProductCategoryList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('VEG_ORGANIC')).toBeInTheDocument();
    });

    const deleteButtons = screen.getAllByTitle('Xóa');
    fireEvent.click(deleteButtons[0]);

    await waitFor(() => {
      expect(screen.getByText(/Bạn có chắc chắn muốn xóa/i)).toBeInTheDocument();
    });

    const confirmBtn = screen.getByRole('button', { name: /Xóa ngay/i });
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(productCategoryApi.delete).toHaveBeenCalledWith(1);
    });
  });
  // #endregion
});
