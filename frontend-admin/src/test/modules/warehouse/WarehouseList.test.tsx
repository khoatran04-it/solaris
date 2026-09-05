import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import WarehouseList from '../../../pages/warehouse/WarehouseList';
import { warehouseApi } from '../../../api/warehouseApi';

// Mock APIs
vi.mock('../../../api/warehouseApi', () => ({
  warehouseApi: {
    getAll: vi.fn(),
    getAllList: vi.fn(),
    toggleActive: vi.fn(),
    delete: vi.fn(),
  },
}));

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

/**
 * ============================================================================
 * MODULE 08: WAREHOUSE & PHYSICAL ADDRESS MANAGEMENT
 * COMPONENT TEST: WarehouseList (Danh sách Kho Hàng & Địa chỉ)
 * ============================================================================
 */
describe('Module 08 - WarehouseList Component', () => {
  const mockWarehouses = [
    {
      id: 1,
      code: 'WH-HN-01',
      name: 'Tổng Kho Hà Nội',
      warehouseType: 'Kho Tổng',
      managerId: 10,
      managerName: 'Nguyễn Văn Trưởng Kho',
      addressId: 1,
      province: 'Hà Nội',
      district: 'Long Biên',
      ward: 'Gia Thụy',
      streetAddress: 'Số 123 Nguyễn Sơn',
      fullAddress: 'Số 123 Nguyễn Sơn, Gia Thụy, Long Biên, Hà Nội',
      latitude: 21.0456,
      longitude: 105.8821,
      isActive: true,
      createdAt: '2026-08-20T10:00:00Z',
    },
    {
      id: 2,
      code: 'WH-HCM-01',
      name: 'Kho Nam Sài Gòn',
      warehouseType: 'Kho Bán Lẻ',
      managerId: null,
      managerName: null,
      addressId: 2,
      province: 'TP Hồ Chí Minh',
      district: 'Quận 7',
      ward: 'Tân Phong',
      streetAddress: 'Nguyễn Văn Linh',
      fullAddress: 'Nguyễn Văn Linh, Tân Phong, Quận 7, TP Hồ Chí Minh',
      latitude: 10.7769,
      longitude: 106.7009,
      isActive: false,
      createdAt: '2026-08-21T10:00:00Z',
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (warehouseApi.getAllList as any).mockResolvedValue(mockWarehouses);
    (warehouseApi.getAll as any).mockResolvedValue({
      items: mockWarehouses,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // #region TC01: RENDER DANH SÁCH KHO HÀNG
  it('TC01 - Render danh sách kho hàng với đầy đủ thông tin mã, tên, địa chỉ chi tiết và trưởng kho', async () => {
    render(
      <MemoryRouter>
        <WarehouseList />
      </MemoryRouter>
    );

    expect(screen.getByText('Danh Sách Kho Hàng')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('Tổng Kho Hà Nội')).toBeInTheDocument();
      expect(screen.getByText('WH-HN-01')).toBeInTheDocument();
      expect(
        screen.getByText('Số 123 Nguyễn Sơn, Gia Thụy, Long Biên, Hà Nội')
      ).toBeInTheDocument();
      expect(screen.getByText('Nguyễn Văn Trưởng Kho')).toBeInTheDocument();

      expect(screen.getByText('Kho Nam Sài Gòn')).toBeInTheDocument();
      expect(screen.getByText('WH-HCM-01')).toBeInTheDocument();
      expect(screen.getByText('Chưa bổ nhiệm')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC02: TÌM KIẾM THEO TỪ KHÓA
  it('TC02 - Tìm kiếm kho hàng theo từ khóa (Debounced search)', async () => {
    render(
      <MemoryRouter>
        <WarehouseList />
      </MemoryRouter>
    );

    const searchInput = screen.getByPlaceholderText('Tìm kiếm theo mã, tên kho...');
    fireEvent.change(searchInput, { target: { value: 'Sài Gòn' } });

    await waitFor(
      () => {
        expect(warehouseApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({
            search: 'Sài Gòn',
          })
        );
      },
      { timeout: 1000 }
    );
  });
  // #endregion

  // #region TC03: TOGGLE ACTIVE
  it('TC03 - Thay đổi trạng thái hoạt động (Hoạt động / Tạm khóa) của kho hàng', async () => {
    (warehouseApi.toggleActive as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <WarehouseList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Tổng Kho Hà Nội')).toBeInTheDocument();
    });

    const activeButtons = screen.getAllByTitle('Nhấn để đổi trạng thái');
    fireEvent.click(activeButtons[0]);

    await waitFor(() => {
      expect(warehouseApi.toggleActive).toHaveBeenCalledWith(1);
      expect(warehouseApi.getAll).toHaveBeenCalledTimes(2);
    });
  });
  // #endregion

  // #region TC04: XÓA KHO HÀNG
  it('TC04 - Mở modal xác nhận xóa và xóa thành công kho hàng', async () => {
    (warehouseApi.delete as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <WarehouseList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Tổng Kho Hà Nội')).toBeInTheDocument();
    });

    const deleteButtons = screen.getAllByTitle('Xóa');
    fireEvent.click(deleteButtons[0]);

    // Modal hiển thị
    await waitFor(() => {
      expect(screen.getByText(/Bạn có chắc chắn muốn xóa/i)).toBeInTheDocument();
    });

    const confirmBtn = screen.getByRole('button', { name: /Xóa ngay/i });
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(warehouseApi.delete).toHaveBeenCalledWith(1);
      expect(warehouseApi.getAll).toHaveBeenCalledTimes(2);
    });
  });
  // #endregion

  // #region TC05: TABLE EMPTY
  it('TC05 - Hiển thị giao diện rỗng khi không có kho hàng nào', async () => {
    (warehouseApi.getAll as any).mockResolvedValue({
      items: [],
      totalRecords: 0,
      totalPages: 0,
      currentPage: 1,
      pageSize: 10,
    });

    render(
      <MemoryRouter>
        <WarehouseList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(
        screen.getByText(/Thử thay đổi từ khóa tìm kiếm hoặc bộ lọc khu vực/i)
      ).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC06: ĐIỀU HƯỚNG TẠO MỚI & SỬA
  it('TC06 - Điều hướng chính xác khi nhấn Thêm mới và Chỉnh sửa', async () => {
    render(
      <MemoryRouter>
        <WarehouseList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Tổng Kho Hà Nội')).toBeInTheDocument();
    });

    // Nút Thêm mới
    const addBtn = screen.getByRole('button', { name: /THÊM/i });
    fireEvent.click(addBtn);
    expect(mockNavigate).toHaveBeenCalledWith('/warehouses/create');

    // Nút Sửa
    const editButtons = screen.getAllByTitle('Chỉnh sửa');
    fireEvent.click(editButtons[0]);
    expect(mockNavigate).toHaveBeenCalledWith('/warehouses/edit/1');
  });
  // #endregion
});
