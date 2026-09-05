import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import UoMConversionList from '../../../pages/uomConversion/UoMConversionList';
import { uomConversionApi } from '../../../api/uomConversionApi';
import { productApi } from '../../../api/productApi';

// Mock API
vi.mock('../../../api/uomConversionApi', () => ({
  uomConversionApi: {
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
 * MODULE 2: UNIT OF MEASURE (UoM)
 * COMPONENT TEST: UoMConversionList (Danh sách Quy tắc Quy đổi Đơn vị tính)
 * ============================================================================
 */
describe('Module 02 - UoMConversionList Component', () => {
  const mockConversions = [
    {
      id: 1,
      fromUoMId: 1,
      fromUoMCode: 'TON',
      fromUoMName: 'Tấn',
      toUoMId: 2,
      toUoMCode: 'KG',
      toUoMName: 'Kilogram',
      conversionFactor: 1000,
      productId: null,
      productCode: null,
      productName: null,
      isStandard: true,
      semanticDescription: '1 Tấn = 1000 Kilogram',
      isActive: true,
      createdAt: '2026-08-20T10:00:00Z',
    },
    {
      id: 2,
      fromUoMId: 3,
      fromUoMCode: 'BOX',
      fromUoMName: 'Thùng',
      toUoMId: 4,
      toUoMCode: 'BTL',
      toUoMName: 'Chai',
      conversionFactor: 24,
      productId: 10,
      productCode: 'WATER',
      productName: 'Nước khoáng Lavie',
      isStandard: false,
      semanticDescription: '1 Thùng = 24 Chai',
      isActive: true,
      createdAt: '2026-08-21T10:00:00Z',
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (productApi.getAllList as any).mockResolvedValue([
      { id: 10, code: 'WATER', name: 'Nước khoáng Lavie' },
    ]);
    (uomConversionApi.getAll as any).mockResolvedValue({
      items: mockConversions,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // #region TC01: RENDER DANH SÁCH QUY ĐỔI
  /**
   * TC01: Render tiêu đề trang "Tỷ Lệ Quy Đổi" và các bản ghi quy đổi kèm công thức toán học.
   */
  it('TC01 - Render tiêu đề trang và danh sách quy đổi từ API', async () => {
    render(
      <MemoryRouter>
        <UoMConversionList />
      </MemoryRouter>
    );

    expect(screen.getByText('Tỷ Lệ Quy Đổi')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('1 Tấn')).toBeInTheDocument();
      expect(screen.getByText('1 Thùng')).toBeInTheDocument();
      expect(screen.getByText('Tiêu chuẩn')).toBeInTheDocument();
      expect(screen.getByText('Đặc thù SP')).toBeInTheDocument();
      expect(screen.getByText(/Nước khoáng Lavie/i)).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC02: TÌM KIẾM QUY TẮC
  /**
   * TC02: Nhập từ khóa tìm kiếm theo tên đơn vị hoặc tên sản phẩm.
   */
  it('TC02 - Nhập từ khóa tìm kiếm quy tắc quy đổi', async () => {
    render(
      <MemoryRouter>
        <UoMConversionList />
      </MemoryRouter>
    );

    const searchInput = screen.getByPlaceholderText('Tìm theo đơn vị hoặc tên sản phẩm...');
    fireEvent.change(searchInput, { target: { value: 'Lavie' } });

    await waitFor(
      () => {
        expect(uomConversionApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({
            search: 'Lavie',
          })
        );
      },
      { timeout: 1000 }
    );
  });
  // #endregion

  // #region TC03: ĐỔI TRẠNG THÁI HOẠT ĐỘNG
  /**
   * TC03: Click badge đổi trạng thái quy tắc quy đổi.
   */
  it('TC03 - Đổi trạng thái hoạt động khi click vào Badge trạng thái', async () => {
    (uomConversionApi.toggleActive as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <UoMConversionList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('1 Tấn')).toBeInTheDocument();
    });

    const activeBadges = screen.getAllByText('Hoạt động');
    fireEvent.click(activeBadges[0]);

    await waitFor(() => {
      expect(uomConversionApi.toggleActive).toHaveBeenCalledWith(1);
    });
  });
  // #endregion

  // #region TC04: MỞ MODAL VÀ XÓA QUY TẮC QUY ĐỔI
  /**
   * TC04: Click icon xóa mở modal xác nhận và gọi API delete quy tắc quy đổi.
   */
  it('TC04 - Mở modal xác nhận và xóa quy tắc quy đổi thành công', async () => {
    (uomConversionApi.delete as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <UoMConversionList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('1 Tấn')).toBeInTheDocument();
    });

    const deleteButtons = screen.getAllByTitle('Xóa');
    fireEvent.click(deleteButtons[0]);

    await waitFor(() => {
      expect(screen.getByText('Xác nhận xóa dữ liệu')).toBeInTheDocument();
    });

    const confirmBtn = screen.getByText('Xóa ngay');
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(uomConversionApi.delete).toHaveBeenCalledWith(1);
    });
  });
  // #endregion
});
