import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import UoMList from '../../../pages/uom/UoMList';
import { uomApi } from '../../../api/uomApi';
import { uomCategoryApi } from '../../../api/uomCategoryApi';

// Mock API
vi.mock('../../../api/uomApi', () => ({
  uomApi: {
    getAll: vi.fn(),
    toggleActive: vi.fn(),
    delete: vi.fn(),
  },
}));

vi.mock('../../../api/uomCategoryApi', () => ({
  uomCategoryApi: {
    getAllList: vi.fn(),
  },
}));

/**
 * ============================================================================
 * MODULE 2: UNIT OF MEASURE (UoM)
 * COMPONENT TEST: UoMList (Danh sách Đơn vị tính)
 * ============================================================================
 */
describe('Module 02 - UoMList Component', () => {
  const mockUoMs = [
    {
      id: 1,
      code: 'KG',
      name: 'Kilogram',
      synonyms: 'ký, cân, kilogam',
      categoryId: 1,
      categoryCode: 'WEIGHT',
      categoryName: 'Khối lượng',
      isActive: true,
      createdAt: '2026-08-20T10:00:00Z',
    },
    {
      id: 2,
      code: 'TON',
      name: 'Tấn',
      synonyms: 'tan, tấn',
      categoryId: 1,
      categoryCode: 'WEIGHT',
      categoryName: 'Khối lượng',
      isActive: false,
      createdAt: '2026-08-21T10:00:00Z',
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (uomCategoryApi.getAllList as any).mockResolvedValue([
      { id: 1, code: 'WEIGHT', name: 'Khối lượng' },
    ]);
    (uomApi.getAll as any).mockResolvedValue({
      items: mockUoMs,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // #region TC01: RENDER DANH SÁCH ĐVT
  /**
   * TC01: Render tiêu đề trang "Đơn Vị Tính" và bảng danh sách ĐVT kèm từ đồng nghĩa AI.
   */
  it('TC01 - Render tiêu đề trang và danh sách ĐVT từ API', async () => {
    render(
      <MemoryRouter>
        <UoMList />
      </MemoryRouter>
    );

    expect(screen.getByText('Đơn Vị Tính')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('KG')).toBeInTheDocument();
      expect(screen.getByText('Kilogram')).toBeInTheDocument();
      expect(screen.getByText('TON')).toBeInTheDocument();
      expect(screen.getByText('Tấn')).toBeInTheDocument();
      expect(screen.getByText('ký')).toBeInTheDocument();
      expect(screen.getByText('cân')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC02: TÌM KIẾM ĐVT THEO TỪ KHÓA
  /**
   * TC02: Nhập từ khóa tìm kiếm theo từ đồng nghĩa hoặc tên ĐVT.
   */
  it('TC02 - Nhập từ khóa tìm kiếm ĐVT theo tên/mã/từ đồng nghĩa', async () => {
    render(
      <MemoryRouter>
        <UoMList />
      </MemoryRouter>
    );

    const searchInput = screen.getByPlaceholderText('Tìm theo mã, tên hoặc từ khóa...');
    fireEvent.change(searchInput, { target: { value: 'cân' } });

    await waitFor(
      () => {
        expect(uomApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({
            search: 'cân',
          })
        );
      },
      { timeout: 1000 }
    );
  });
  // #endregion

  // #region TC03: ĐỔI TRẠNG THÁI HOẠT ĐỘNG
  /**
   * TC03: Click badge đổi trạng thái hoạt động của ĐVT.
   */
  it('TC03 - Đổi trạng thái hoạt động khi click vào Badge trạng thái', async () => {
    (uomApi.toggleActive as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <UoMList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('KG')).toBeInTheDocument();
    });

    const activeBadge = screen.getByText('Hoạt động');
    fireEvent.click(activeBadge);

    await waitFor(() => {
      expect(uomApi.toggleActive).toHaveBeenCalledWith(1);
    });
  });
  // #endregion

  // #region TC04: MỞ MODAL VÀ XÓA ĐVT
  /**
   * TC04: Click nút xóa mở modal xác nhận và gọi API xóa ĐVT.
   */
  it('TC04 - Mở modal xác nhận và xóa ĐVT thành công', async () => {
    (uomApi.delete as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <UoMList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('KG')).toBeInTheDocument();
    });

    const deleteButtons = screen.getAllByTitle('Xóa');
    fireEvent.click(deleteButtons[0]);

    await waitFor(() => {
      expect(screen.getByText('Xác nhận xóa dữ liệu')).toBeInTheDocument();
    });

    const confirmBtn = screen.getByText('Xóa ngay');
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(uomApi.delete).toHaveBeenCalledWith(1);
    });
  });
  // #endregion
});
