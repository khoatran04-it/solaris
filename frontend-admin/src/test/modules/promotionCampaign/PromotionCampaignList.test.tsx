import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import PromotionCampaignList from '../../../pages/promotionCampaign/PromotionCampaignList';
import { promotionCampaignApi } from '../../../api/promotionCampaignApi';

// Mock APIs
vi.mock('../../../api/promotionCampaignApi', () => ({
  promotionCampaignApi: {
    getAll: vi.fn(),
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
 * MODULE 07: PROMOTION CAMPAIGNS
 * COMPONENT TEST: PromotionCampaignList (Danh sách Chiến Dịch Khuyến Mãi)
 * ============================================================================
 */
describe('Module 07 - PromotionCampaignList Component', () => {
  const mockCampaigns = [
    {
      id: 1,
      name: 'Flash Sale Cuối Tuần',
      description: 'Giảm giá sâu các mặt hàng trái cây',
      isPercentage: true,
      discountValue: 20,
      startDate: new Date(Date.now() - 86400000).toISOString(), // Đang diễn ra
      endDate: new Date(Date.now() + 86400000 * 2).toISOString(),
      isActive: true,
      createdAt: '2026-08-20T10:00:00Z',
      updatedAt: '2026-08-20T10:00:00Z',
      appliedVariants: [],
    },
    {
      id: 2,
      name: 'Xả Kho Mùa Thu',
      description: 'Giảm giá tiền mặt 50.000đ',
      isPercentage: false,
      discountValue: 50000,
      startDate: '2026-01-01T00:00:00Z', // Đã kết thúc
      endDate: '2026-01-10T23:59:59Z',
      isActive: true,
      createdAt: '2026-08-21T10:00:00Z',
      updatedAt: '2026-08-21T10:00:00Z',
      appliedVariants: [],
    },
    {
      id: 3,
      name: 'Siêu Sale Tết',
      description: 'Tạm khóa chiến dịch',
      isPercentage: true,
      discountValue: 15,
      startDate: new Date(Date.now() + 86400000 * 10).toISOString(), // Sắp diễn ra nhưng bị khóa
      endDate: new Date(Date.now() + 86400000 * 20).toISOString(),
      isActive: false,
      createdAt: '2026-08-22T10:00:00Z',
      updatedAt: '2026-08-22T10:00:00Z',
      appliedVariants: [],
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (promotionCampaignApi.getAll as any).mockResolvedValue({
      items: mockCampaigns,
      totalRecords: 3,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // #region TC01: RENDER DANH SÁCH CHIẾN DỊCH
  it('TC01 - Render danh sách chiến dịch khuyến mãi với đầy đủ thông tin giảm giá và trạng thái', async () => {
    render(
      <MemoryRouter>
        <PromotionCampaignList />
      </MemoryRouter>
    );

    expect(screen.getByText('Chiến Dịch Khuyến Mãi')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('Flash Sale Cuối Tuần')).toBeInTheDocument();
      expect(screen.getByText('Xả Kho Mùa Thu')).toBeInTheDocument();
      expect(screen.getByText('Siêu Sale Tết')).toBeInTheDocument();
      expect(screen.getByText('Giảm 20%')).toBeInTheDocument();
      expect(screen.getByText('-50.000 ₫')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC02: HIỂN THỊ BADGE THỜI GIAN
  it('TC02 - Hiển thị chính xác badge thời hạn chiến dịch (Đang diễn ra, Đã kết thúc, Đã khóa)', async () => {
    render(
      <MemoryRouter>
        <PromotionCampaignList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Đang diễn ra')).toBeInTheDocument();
      expect(screen.getByText('Đã kết thúc')).toBeInTheDocument();
      expect(screen.getByText('Đã khóa')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC03: TÌM KIẾM THEO TÊN CHIẾN DỊCH
  it('TC03 - Tìm kiếm chiến dịch theo từ khóa (Debounced search)', async () => {
    render(
      <MemoryRouter>
        <PromotionCampaignList />
      </MemoryRouter>
    );

    const searchInput = screen.getByPlaceholderText('Tìm kiếm theo tên chiến dịch...');
    fireEvent.change(searchInput, { target: { value: 'Flash Sale' } });

    await waitFor(
      () => {
        expect(promotionCampaignApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({
            search: 'Flash Sale',
          })
        );
      },
      { timeout: 1000 }
    );
  });
  // #endregion

  // #region TC04: TOGGLE ACTIVE
  it('TC04 - Thay đổi trạng thái hoạt động (Kích hoạt / Tạm khóa) của chiến dịch', async () => {
    (promotionCampaignApi.toggleActive as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <PromotionCampaignList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Flash Sale Cuối Tuần')).toBeInTheDocument();
    });

    const activeButtons = screen.getAllByTitle('Nhấn để đổi trạng thái');
    fireEvent.click(activeButtons[0]);

    await waitFor(() => {
      expect(promotionCampaignApi.toggleActive).toHaveBeenCalledWith(1);
      expect(promotionCampaignApi.getAll).toHaveBeenCalledTimes(2);
    });
  });
  // #endregion

  // #region TC05: XÓA CHIẾN DỊCH
  it('TC05 - Mở modal xác nhận xóa và xóa thành công chiến dịch', async () => {
    (promotionCampaignApi.delete as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <PromotionCampaignList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Flash Sale Cuối Tuần')).toBeInTheDocument();
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
      expect(promotionCampaignApi.delete).toHaveBeenCalledWith(1);
      expect(promotionCampaignApi.getAll).toHaveBeenCalledTimes(2);
    });
  });
  // #endregion

  // #region TC06: TABLE EMPTY
  it('TC06 - Hiển thị giao diện rỗng khi không có chiến dịch nào', async () => {
    (promotionCampaignApi.getAll as any).mockResolvedValue({
      items: [],
      totalRecords: 0,
      totalPages: 0,
      currentPage: 1,
      pageSize: 10,
    });

    render(
      <MemoryRouter>
        <PromotionCampaignList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Chưa có chiến dịch khuyến mãi nào.')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC07: ĐIỀU HƯỚNG TẠO MỚI & SỬA
  it('TC07 - Điều hướng chính xác khi nhấn Thêm mới và Chỉnh sửa', async () => {
    render(
      <MemoryRouter>
        <PromotionCampaignList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Flash Sale Cuối Tuần')).toBeInTheDocument();
    });

    // Nút Thêm mới
    const addBtn = screen.getByRole('button', { name: /THÊM/i });
    fireEvent.click(addBtn);
    expect(mockNavigate).toHaveBeenCalledWith('/promotions/create');

    // Nút Sửa
    const editButtons = screen.getAllByTitle('Sửa thông tin & Gắn sản phẩm');
    fireEvent.click(editButtons[0]);
    expect(mockNavigate).toHaveBeenCalledWith('/promotions/edit/1');
  });
  // #endregion
});
