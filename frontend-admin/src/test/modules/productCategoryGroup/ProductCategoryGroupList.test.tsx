import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import ProductCategoryGroupList from '../../../pages/productCategoryGroup/ProductCategoryGroupList';
import { productCategoryGroupApi } from '../../../api/productCategoryGroupApi';

// Mock API
vi.mock('../../../api/productCategoryGroupApi', () => ({
  productCategoryGroupApi: {
    getAll: vi.fn(),
    toggleActive: vi.fn(),
    delete: vi.fn(),
  },
}));

/**
 * ============================================================================
 * MODULE 4: PRODUCT CATEGORY GROUP
 * COMPONENT TEST: ProductCategoryGroupList (Danh sách Nhóm Ngành Hàng)
 * ============================================================================
 */
describe('Module 04 - ProductCategoryGroupList Component', () => {
  const mockGroups = [
    {
      id: 1,
      code: 'FRESH_FOOD',
      name: 'Thực phẩm tươi sống',
      description: 'Nông sản, thịt cá tươi hàng ngày',
      imagePath: 'https://example.com/fresh.jpg',
      isActive: true,
      createdAt: '2026-08-20T10:00:00Z',
      updatedAt: '2026-08-20T10:00:00Z',
    },
    {
      id: 2,
      code: 'DRY_FOOD',
      name: 'Thực phẩm khô',
      description: 'Các loại hạt, gia vị khô',
      imagePath: null,
      isActive: false,
      createdAt: '2026-08-21T10:00:00Z',
      updatedAt: '2026-08-21T10:00:00Z',
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (productCategoryGroupApi.getAll as any).mockResolvedValue({
      items: mockGroups,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // #region TC01: RENDER DANH SÁCH NHÓM NGÀNH HÀNG
  it('TC01 - Render danh sách nhóm ngành hàng với đầy đủ thông tin', async () => {
    render(
      <MemoryRouter>
        <ProductCategoryGroupList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thực phẩm tươi sống')).toBeInTheDocument();
      expect(screen.getByText('FRESH_FOOD')).toBeInTheDocument();
      expect(screen.getByText('Thực phẩm khô')).toBeInTheDocument();
      expect(screen.getByText('DRY_FOOD')).toBeInTheDocument();
    });

    expect(productCategoryGroupApi.getAll).toHaveBeenCalled();
  });
  // #endregion

  // #region TC02: TÌM KIẾM THEO TỪ KHÓA
  it('TC02 - Tìm kiếm theo từ khóa mã hoặc tên nhóm ngành hàng', async () => {
    render(
      <MemoryRouter>
        <ProductCategoryGroupList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('FRESH_FOOD')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText(/Tìm kiếm theo mã, tên nhóm.../i);
    fireEvent.change(searchInput, { target: { value: 'tươi sống' } });

    await waitFor(
      () => {
        expect(productCategoryGroupApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({
            search: 'tươi sống',
          })
        );
      },
      { timeout: 1000 }
    );
  });
  // #endregion

  // #region TC03: LỌC THEO TRẠNG THÁI HOẠT ĐỘNG
  it('TC03 - Lọc danh sách theo trạng thái Hoạt động / Tạm khóa', async () => {
    render(
      <MemoryRouter>
        <ProductCategoryGroupList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('FRESH_FOOD')).toBeInTheDocument();
    });

    const filterButtons = screen.getAllByRole('button');
    const statusFilterBtn = filterButtons.find((btn) => btn.textContent?.includes('TRẠNG THÁI'));
    if (statusFilterBtn) {
      fireEvent.click(statusFilterBtn);
    }

    await waitFor(() => {
      expect(productCategoryGroupApi.getAll).toHaveBeenCalled();
    });
  });
  // #endregion

  // #region TC04: THAY ĐỔI TRẠNG THÁI (TOGGLE ACTIVE)
  it('TC04 - Đổi trạng thái hoạt động của nhóm ngành hàng', async () => {
    (productCategoryGroupApi.toggleActive as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <ProductCategoryGroupList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('FRESH_FOOD')).toBeInTheDocument();
    });

    const activeToggle = screen.getAllByRole('button', { name: /Hoạt động/i })[0];
    fireEvent.click(activeToggle);

    await waitFor(() => {
      expect(productCategoryGroupApi.toggleActive).toHaveBeenCalledWith(1);
    });
  });
  // #endregion

  // #region TC05: XÓA NHÓM NGÀNH HÀNG CÓ XÁC NHẬN MODAL
  it('TC05 - Mở modal xác nhận xóa và gọi API delete', async () => {
    (productCategoryGroupApi.delete as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <ProductCategoryGroupList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('FRESH_FOOD')).toBeInTheDocument();
    });

    const deleteButtons = screen.getAllByTitle('Xóa');
    fireEvent.click(deleteButtons[0]);

    await waitFor(() => {
      expect(screen.getByText(/Bạn có chắc chắn muốn xóa/i)).toBeInTheDocument();
    });

    const confirmBtn = screen.getByRole('button', { name: /Xóa ngay/i });
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(productCategoryGroupApi.delete).toHaveBeenCalledWith(1);
    });
  });
  // #endregion
});
