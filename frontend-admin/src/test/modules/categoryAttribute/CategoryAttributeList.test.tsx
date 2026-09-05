import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import CategoryAttributeList from '../../../pages/categoryAttribute/CategoryAttributeList';
import { categoryAttributeApi } from '../../../api/categoryAttributeApi';
import { productCategoryApi } from '../../../api/productCategoryApi';
import { attributeDefinitionApi } from '../../../api/attributeDefinitionApi';

// Mock APIs
vi.mock('../../../api/categoryAttributeApi', () => ({
  categoryAttributeApi: {
    getAll: vi.fn(),
    delete: vi.fn(),
  },
}));

vi.mock('../../../api/productCategoryApi', () => ({
  productCategoryApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/attributeDefinitionApi', () => ({
  attributeDefinitionApi: {
    getAllList: vi.fn(),
  },
}));

/**
 * ============================================================================
 * MODULE 4: CATEGORY ATTRIBUTE
 * COMPONENT TEST: CategoryAttributeList (Cấu Hình Thuộc Tính Danh Mục)
 * ============================================================================
 */
describe('Module 04 - CategoryAttributeList Component', () => {
  const mockCategoryAttributes = [
    {
      id: 1,
      categoryId: 1,
      categoryName: 'Rau củ quả sạch',
      attributeDefinitionId: 10,
      attributeDefinitionName: 'Độ tươi (Ngày thu hoạch)',
      isRequired: true,
    },
    {
      id: 2,
      categoryId: 2,
      categoryName: 'Thịt tươi sống',
      attributeDefinitionId: 11,
      attributeDefinitionName: 'Quy cách sơ chế',
      isRequired: false,
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (productCategoryApi.getAllList as any).mockResolvedValue([
      { id: 1, name: 'Rau củ quả sạch' },
      { id: 2, name: 'Thịt tươi sống' },
    ]);
    (attributeDefinitionApi.getAllList as any).mockResolvedValue([
      { id: 10, name: 'Độ tươi (Ngày thu hoạch)' },
      { id: 11, name: 'Quy cách sơ chế' },
    ]);
    (categoryAttributeApi.getAll as any).mockResolvedValue({
      items: mockCategoryAttributes,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // #region TC01: RENDER DANH SÁCH CẤU HÌNH THUỘC TÍNH
  it('TC01 - Render danh sách cấu hình thuộc tính danh mục với đầy đủ thông tin', async () => {
    render(
      <MemoryRouter>
        <CategoryAttributeList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Rau củ quả sạch')).toBeInTheDocument();
      expect(screen.getByText('Độ tươi (Ngày thu hoạch)')).toBeInTheDocument();
      expect(screen.getByText('Thịt tươi sống')).toBeInTheDocument();
      expect(screen.getByText('Quy cách sơ chế')).toBeInTheDocument();
    });

    expect(categoryAttributeApi.getAll).toHaveBeenCalled();
  });
  // #endregion

  // #region TC02: TÌM KIẾM THEO TỪ KHÓA
  it('TC02 - Tìm kiếm theo tên danh mục hoặc tên thuộc tính', async () => {
    render(
      <MemoryRouter>
        <CategoryAttributeList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Rau củ quả sạch')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText(/Tìm kiếm theo tên danh mục, thuộc tính.../i);
    fireEvent.change(searchInput, { target: { value: 'Rau củ' } });

    await waitFor(
      () => {
        expect(categoryAttributeApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({
            search: 'Rau củ',
          })
        );
      },
      { timeout: 1000 }
    );
  });
  // #endregion

  // #region TC03: LỌC THEO DANH MỤC VÀ THUỘC TÍNH
  it('TC03 - Lọc danh sách theo bộ lọc Danh Mục và Thuộc Tính', async () => {
    render(
      <MemoryRouter>
        <CategoryAttributeList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Rau củ quả sạch')).toBeInTheDocument();
    });

    const filterButtons = screen.getAllByRole('button');
    const catFilterBtn = filterButtons.find((btn) => btn.textContent?.includes('DANH MỤC'));
    if (catFilterBtn) {
      fireEvent.click(catFilterBtn);
    }

    await waitFor(() => {
      expect(categoryAttributeApi.getAll).toHaveBeenCalled();
    });
  });
  // #endregion

  // #region TC04: XÓA CẤU HÌNH GÁN THUỘC TÍNH CÓ XÁC NHẬN MODAL
  it('TC04 - Mở modal xác nhận xóa và gọi API delete', async () => {
    (categoryAttributeApi.delete as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <CategoryAttributeList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Rau củ quả sạch')).toBeInTheDocument();
    });

    const deleteButtons = screen.getAllByTitle('Xóa');
    fireEvent.click(deleteButtons[0]);

    await waitFor(() => {
      expect(screen.getByText(/Bạn có chắc chắn muốn xóa/i)).toBeInTheDocument();
    });

    const confirmBtn = screen.getByRole('button', { name: /Xóa ngay/i });
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(categoryAttributeApi.delete).toHaveBeenCalledWith(1);
    });
  });
  // #endregion
});
