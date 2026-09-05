import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import UoMCategoryList from '../../../pages/uomCategory/UoMCategoryList';
import { uomCategoryApi } from '../../../api/uomCategoryApi';

// Mock uomCategoryApi
vi.mock('../../../api/uomCategoryApi', () => ({
  uomCategoryApi: {
    getAll: vi.fn(),
    toggleActive: vi.fn(),
    delete: vi.fn(),
  },
}));

/**
 * ============================================================================
 * MODULE 2: UNIT OF MEASURE (UoM)
 * COMPONENT TEST: UoMCategoryList (Danh sách Nhóm Đơn vị tính)
 * ============================================================================
 */
describe('Module 02 - UoMCategoryList Component', () => {
  const mockCategories = [
    {
      id: 1,
      code: 'WEIGHT',
      name: 'Khối lượng',
      baseUoMId: 10,
      baseUoMCode: 'KG',
      baseUoMName: 'Kilogram',
      isActive: true,
      createdAt: '2026-08-20T10:00:00Z',
    },
    {
      id: 2,
      code: 'VOLUME',
      name: 'Thể tích',
      baseUoMId: 20,
      baseUoMCode: 'L',
      baseUoMName: 'Lít',
      isActive: false,
      createdAt: '2026-08-21T10:00:00Z',
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (uomCategoryApi.getAll as any).mockResolvedValue({
      items: mockCategories,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // #region TC01: RENDER DANH SÁCH NHÓM ĐVT
  /**
   * TC01: Khi trang được tải, hệ thống gọi API lấy danh sách nhóm ĐVT
   * và render bảng dữ liệu với đầy đủ Mã nhóm, Tên nhóm, Trạng thái.
   */
  it('TC01 - Render tiêu đề trang và danh sách nhóm ĐVT từ API', async () => {
    render(
      <MemoryRouter>
        <UoMCategoryList />
      </MemoryRouter>
    );

    // Kiểm tra tiêu đề trang
    expect(screen.getByText('Nhóm Đơn Vị Tính')).toBeInTheDocument();

    // Chờ dữ liệu load xong và xuất hiện trên màn hình
    await waitFor(() => {
      expect(screen.getByText('WEIGHT')).toBeInTheDocument();
      expect(screen.getByText('Khối lượng')).toBeInTheDocument();
      expect(screen.getByText('VOLUME')).toBeInTheDocument();
      expect(screen.getByText('Thể tích')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC02: TÌM KIẾM THEO TỪ KHÓA
  /**
   * TC02: Khi người dùng gõ từ khóa vào ô tìm kiếm, hệ thống debounce
   * và gọi lại API với tham số `search`.
   */
  it('TC02 - Nhập từ khóa tìm kiếm nhóm ĐVT', async () => {
    render(
      <MemoryRouter>
        <UoMCategoryList />
      </MemoryRouter>
    );

    const searchInput = screen.getByPlaceholderText('Tìm theo mã hoặc tên nhóm...');
    fireEvent.change(searchInput, { target: { value: 'Khối lượng' } });

    // Chờ debounce 500ms
    await waitFor(
      () => {
        expect(uomCategoryApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({
            search: 'Khối lượng',
          })
        );
      },
      { timeout: 1000 }
    );
  });
  // #endregion

  // #region TC03: THAY ĐỔI TRẠNG THÁI (TOGGLE ACTIVE)
  /**
   * TC03: Click vào badge trạng thái hoạt động để kích hoạt hoặc tạm khóa nhóm ĐVT.
   */
  it('TC03 - Đổi trạng thái hoạt động khi click vào Badge trạng thái', async () => {
    (uomCategoryApi.toggleActive as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <UoMCategoryList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('WEIGHT')).toBeInTheDocument();
    });

    // Tìm badge "Hoạt động" của nhóm WEIGHT
    const activeBadge = screen.getByText('Hoạt động');
    fireEvent.click(activeBadge);

    await waitFor(() => {
      expect(uomCategoryApi.toggleActive).toHaveBeenCalledWith(1);
    });
  });
  // #endregion

  // #region TC04: MỞ MODAL VÀ XÓA NHÓM ĐVT
  /**
   * TC04: Click vào icon Xóa sẽ mở modal xác nhận, bấm "Xác nhận xóa" sẽ gọi API delete.
   */
  it('TC04 - Mở modal xác nhận và xóa nhóm ĐVT thành công', async () => {
    (uomCategoryApi.delete as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <UoMCategoryList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('WEIGHT')).toBeInTheDocument();
    });

    // Tìm tất cả các nút có title hoặc icon xóa
    const deleteButtons = screen.getAllByTitle('Xóa');
    fireEvent.click(deleteButtons[0]);

    // Modal xuất hiện
    await waitFor(() => {
      expect(screen.getByText('Xác nhận xóa dữ liệu')).toBeInTheDocument();
    });

    // Bấm nút "Xóa ngay" trong modal
    const confirmBtn = screen.getByText('Xóa ngay');
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(uomCategoryApi.delete).toHaveBeenCalledWith(1);
    });
  });
  // #endregion
});
